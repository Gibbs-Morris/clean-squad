using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Definitions;
using CleanSquad.Workflow.Orchestration;
using CleanSquad.Workflow.Prompting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CleanSquad.Workflow.Decisions;

/// <summary>
///     Resolves branch decisions using rules or an optional decision-agent node.
/// </summary>
public sealed partial class WorkflowDecisionResolver : IWorkflowDecisionResolver
{
    private readonly IWorkflowAgentRunner workflowAgentRunner;
    private readonly ILogger<WorkflowDecisionResolver> logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="WorkflowDecisionResolver" /> class.
    /// </summary>
    /// <param name="workflowAgentRunner">The workflow agent runner.</param>
    /// <param name="logger">The optional logger.</param>
    public WorkflowDecisionResolver(
        IWorkflowAgentRunner workflowAgentRunner,
        ILogger<WorkflowDecisionResolver>? logger = null)
    {
        this.workflowAgentRunner = workflowAgentRunner ?? throw new ArgumentNullException(nameof(workflowAgentRunner));
        this.logger = logger ?? NullLogger<WorkflowDecisionResolver>.Instance;
    }

    /// <inheritdoc />
    public async Task<WorkflowDecision> ResolveAsync(WorkflowDecisionContext context, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Node.DecisionMode == WorkflowDecisionMode.Agent)
        {
            this.LogResolvingAgentDecision(
                context.Node.Id,
                context.AttachmentFilePaths.Count);
            string prompt = await WorkflowPromptComposer.ComposeAsync(
                context.Definition,
                context.Node,
                context.AttachmentFilePaths,
                cancellationToken);
            string decisionMarkdown = await this.workflowAgentRunner.RunAsync(
                ResolveAgentName(context.Node),
                prompt,
                context.AttachmentFilePaths,
                context.Node.Models,
                context.Node.ReasoningEffort,
                WorkflowResponseTimeout.TryParse(context.Node.ResponseTimeout, out TimeSpan responseTimeout) ? responseTimeout : null,
                cancellationToken);

            WorkflowDecision agentDecision = this.ParseDecision(context.Node, decisionMarkdown);
            this.LogDecisionResolved(
                context.Node.Id,
                agentDecision.ChoiceId,
                agentDecision.NextNodeId);
            return agentDecision;
        }

        WorkflowDecision rulesDecision = this.ParseRulesDecision(context);
        this.LogDecisionResolved(
            context.Node.Id,
            rulesDecision.ChoiceId,
            rulesDecision.NextNodeId);
        return rulesDecision;
    }

    private WorkflowDecision ParseRulesDecision(WorkflowDecisionContext context)
    {
        if (string.Equals(context.Node.RuleSet, "legacy-review", StringComparison.OrdinalIgnoreCase))
        {
            return ParseLegacyReview(context);
        }

        if (string.Equals(context.Node.RuleSet, "clean-agile-review", StringComparison.OrdinalIgnoreCase))
        {
            return ParseApprovalReview(context);
        }

        return this.ParseDecision(context.Node, context.SourceMarkdown);
    }

    private WorkflowDecision ParseApprovalReview(WorkflowDecisionContext context)
    {
        if (!TryParseApproved(context.SourceMarkdown, out bool approved))
        {
            return ParseDecision(context.Node, context.SourceMarkdown);
        }

        if (approved)
        {
            WorkflowDecisionOptionDefinition approveChoice = FindChoice(context.Node, "approve");
            return new WorkflowDecision(
                WorkflowDecisionAction.Approve,
                "The review approved the current phase.",
                "review-rules",
                context.SourceMarkdown.Trim(),
                approveChoice.Id,
                approveChoice.NextNodeId);
        }

        int reviewCount = CountCompletedSteps(
            context.State,
            string.IsNullOrWhiteSpace(context.Node.DecisionSourceNodeId) ? context.Node.Id : context.Node.DecisionSourceNodeId);

        if (reviewCount >= context.Definition.Policy.MaxReviewCycles || HasReachedRebuildLimit(context))
        {
            WorkflowDecisionOptionDefinition stopChoice = FindChoice(context.Node, "stop");
            return new WorkflowDecision(
                WorkflowDecisionAction.Stop,
                "The configured review or rebuild policy limit was reached.",
                "review-rules",
                context.SourceMarkdown.Trim(),
                stopChoice.Id,
                stopChoice.NextNodeId);
        }

        WorkflowDecisionOptionDefinition? reworkChoice = FindChoiceOrDefault(context.Node, "rework");
        if (reworkChoice is not null)
        {
            return new WorkflowDecision(
                WorkflowDecisionAction.Branch,
                "The review denied approval and requested rework for the current phase.",
                "review-rules",
                context.SourceMarkdown.Trim(),
                reworkChoice.Id,
                reworkChoice.NextNodeId);
        }

        WorkflowDecisionOptionDefinition? rebuildChoice = FindChoiceOrDefault(context.Node, "rebuild");
        if (rebuildChoice is not null)
        {
            return new WorkflowDecision(
                WorkflowDecisionAction.Rebuild,
                "The review denied approval and requested another implementation iteration.",
                "review-rules",
                context.SourceMarkdown.Trim(),
                rebuildChoice.Id,
                rebuildChoice.NextNodeId);
        }

        WorkflowDecisionOptionDefinition fallbackStopChoice = FindChoice(context.Node, "stop");
        return new WorkflowDecision(
            WorkflowDecisionAction.Stop,
            "The review denied approval and no rework path was configured.",
            "review-rules",
            context.SourceMarkdown.Trim(),
            fallbackStopChoice.Id,
            fallbackStopChoice.NextNodeId);
    }

    private static WorkflowDecision ParseLegacyReview(WorkflowDecisionContext context)
    {
        bool approved = false;
        foreach (string line in SplitLines(context.SourceMarkdown))
        {
            if (!line.StartsWith("Approved:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int separatorIndex = line.IndexOf(':', StringComparison.Ordinal);
            string value = line[(separatorIndex + 1)..].Trim();
            approved = value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("approved", StringComparison.OrdinalIgnoreCase);
            break;
        }

        if (approved)
        {
            WorkflowDecisionOptionDefinition choice = FindChoice(context.Node, "approve");
            return new WorkflowDecision(WorkflowDecisionAction.Approve, "The reviewer approved the current build.", "review-rules", context.SourceMarkdown.Trim(), choice.Id, choice.NextNodeId);
        }

        int rebuildCount = CountCompletedSteps(context.State, "rebuilder");
        int reviewCount = CountCompletedSteps(context.State, "reviewer");
        if (rebuildCount >= context.Definition.Policy.MaxRebuilds || reviewCount >= context.Definition.Policy.MaxReviewCycles)
        {
            WorkflowDecisionOptionDefinition stopChoice = FindChoice(context.Node, "stop");
            return new WorkflowDecision(WorkflowDecisionAction.Stop, "The configured review or rebuild policy limit was reached.", "review-rules", context.SourceMarkdown.Trim(), stopChoice.Id, stopChoice.NextNodeId);
        }

        WorkflowDecisionOptionDefinition rebuildChoice = FindChoice(context.Node, "rebuild");
        return new WorkflowDecision(WorkflowDecisionAction.Rebuild, "The reviewer requested another build iteration.", "review-rules", context.SourceMarkdown.Trim(), rebuildChoice.Id, rebuildChoice.NextNodeId);
    }

    private WorkflowDecision ParseDecision(WorkflowNodeDefinition node, string decisionMarkdown)
    {
        foreach (string line in SplitLines(decisionMarkdown))
        {
            bool isChoiceLine = line.StartsWith("Choice:", StringComparison.OrdinalIgnoreCase);
            bool isActionLine = line.StartsWith("Action:", StringComparison.OrdinalIgnoreCase);
            if (!isChoiceLine && !isActionLine)
            {
                continue;
            }

            int separatorIndex = line.IndexOf(':', StringComparison.Ordinal);
            string value = line[(separatorIndex + 1)..].Trim();
            WorkflowDecisionOptionDefinition choice = FindChoice(node, value);
            WorkflowDecisionAction action = choice.Id.ToUpperInvariant() switch
            {
                "APPROVE" => WorkflowDecisionAction.Approve,
                "REBUILD" => WorkflowDecisionAction.Rebuild,
                "STOP" => WorkflowDecisionAction.Stop,
                _ => WorkflowDecisionAction.Branch,
            };

            string decisionSource = isChoiceLine ? "decision-choice" : "decision-agent";
            return new WorkflowDecision(action, $"Decision selected '{choice.Id}'.", decisionSource, decisionMarkdown.Trim(), choice.Id, choice.NextNodeId);
        }

        WorkflowDecisionOptionDefinition fallbackChoice = node.Choices[0];
        WorkflowDecisionAction fallbackAction = string.Equals(fallbackChoice.Id, "stop", StringComparison.OrdinalIgnoreCase)
            ? WorkflowDecisionAction.Stop
            : WorkflowDecisionAction.Branch;
        this.LogDecisionFallback(node.Id, fallbackChoice.Id);
        return new WorkflowDecision(fallbackAction, "The decision node did not emit a supported Choice or Action line.", "decision-agent", decisionMarkdown.Trim(), fallbackChoice.Id, fallbackChoice.NextNodeId);
    }

    private static string[] SplitLines(string content)
    {
        return content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n')
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    private static bool TryParseApproved(string content, out bool approved)
    {
        foreach (string line in SplitLines(content))
        {
            if (!line.StartsWith("Approved:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            int separatorIndex = line.IndexOf(':', StringComparison.Ordinal);
            string value = line[(separatorIndex + 1)..].Trim();
            approved = value.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || value.Equals("true", StringComparison.OrdinalIgnoreCase)
                || value.Equals("approved", StringComparison.OrdinalIgnoreCase);
            return true;
        }

        approved = false;
        return false;
    }

    private static WorkflowDecisionOptionDefinition FindChoice(WorkflowNodeDefinition node, string value)
    {
        return node.Choices.FirstOrDefault(choice => string.Equals(choice.Id, value, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException($"The decision node '{node.Id}' does not define a '{value}' choice.");
    }

    private static WorkflowDecisionOptionDefinition? FindChoiceOrDefault(WorkflowNodeDefinition node, string value)
    {
        return node.Choices.FirstOrDefault(existingChoice => string.Equals(existingChoice.Id, value, StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveAgentName(WorkflowNodeDefinition node)
    {
        if (!string.IsNullOrWhiteSpace(node.Agent))
        {
            return node.Agent;
        }

        return string.IsNullOrWhiteSpace(node.Role) ? node.Id : node.Role;
    }

    private static int CountCompletedSteps(WorkflowRunState state, string nodeId)
    {
        return state.Steps.Count(step =>
            step.Status == WorkflowStepStatus.Completed
            && string.Equals(step.NodeId, nodeId, StringComparison.OrdinalIgnoreCase));
    }

    private static bool HasReachedRebuildLimit(WorkflowDecisionContext context)
    {
        WorkflowDecisionOptionDefinition? rebuildChoice = FindChoiceOrDefault(context.Node, "rebuild");
        if (rebuildChoice is null || string.IsNullOrWhiteSpace(rebuildChoice.NextNodeId))
        {
            return false;
        }

        int rebuildCount = CountCompletedSteps(context.State, rebuildChoice.NextNodeId);
        return rebuildCount >= context.Definition.Policy.MaxRebuilds;
    }

    [LoggerMessage(EventId = 300, Level = LogLevel.Debug, Message = "Resolving decision for node {NodeId} using agent mode with {AttachmentCount} attachments.")]
    private partial void LogResolvingAgentDecision(string nodeId, int attachmentCount);

    [LoggerMessage(EventId = 301, Level = LogLevel.Information, Message = "Decision node {NodeId} resolved choice {ChoiceId} -> {NextNodeId}.")]
    private partial void LogDecisionResolved(string nodeId, string? choiceId, string? nextNodeId);

    [LoggerMessage(EventId = 302, Level = LogLevel.Warning, Message = "Decision node {NodeId} did not emit a supported Choice or Action line. Falling back to {ChoiceId}.")]
    private partial void LogDecisionFallback(string nodeId, string choiceId);
}

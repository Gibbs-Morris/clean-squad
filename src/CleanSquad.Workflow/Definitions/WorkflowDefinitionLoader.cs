using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using CleanSquad.Workflow;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Loads and validates workflow definitions from JSON files.
/// </summary>
public static class WorkflowDefinitionLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    ///     Loads and validates a workflow definition from disk.
    /// </summary>
    /// <param name="workflowDefinitionPath">The workflow definition JSON path.</param>
    /// <returns>The loaded and normalized workflow definition.</returns>
    public static WorkflowDefinition LoadFromFile(string workflowDefinitionPath)
    {
        WorkflowDefinitionValidationResult validationResult = ValidateFile(workflowDefinitionPath);
        if (!validationResult.IsValid || validationResult.Definition is null)
        {
            throw new InvalidOperationException(FormatValidationMessage(validationResult.WorkflowDefinitionPath, validationResult.Errors));
        }

        return validationResult.Definition;
    }

    /// <summary>
    ///     Validates a workflow definition from disk without executing it.
    /// </summary>
    /// <param name="workflowDefinitionPath">The workflow definition JSON path.</param>
    /// <returns>The validation result.</returns>
    public static WorkflowDefinitionValidationResult ValidateFile(string workflowDefinitionPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowDefinitionPath);

        string normalizedPath = Path.GetFullPath(workflowDefinitionPath);
        List<string> errors = [];
        List<string> warnings = [];
        if (!File.Exists(normalizedPath))
        {
            errors.Add($"The workflow definition file could not be found: {normalizedPath}");
            return CreateValidationResult(normalizedPath, null, errors, warnings);
        }

        WorkflowDefinition? definition;
        try
        {
            string json = File.ReadAllText(normalizedPath);
            definition = JsonSerializer.Deserialize<WorkflowDefinition>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            errors.Add($"The workflow definition JSON could not be parsed: {exception.Message}");
            return CreateValidationResult(normalizedPath, null, errors, warnings);
        }
        catch (IOException exception)
        {
            errors.Add($"The workflow definition file could not be read: {exception.Message}");
            return CreateValidationResult(normalizedPath, null, errors, warnings);
        }

        if (definition is null)
        {
            errors.Add("The workflow definition JSON could not be deserialized.");
            return CreateValidationResult(normalizedPath, null, errors, warnings);
        }

        NormalizePackageMetadata(definition);
        NormalizeAssetPaths(definition, Path.GetDirectoryName(normalizedPath) ?? Directory.GetCurrentDirectory(), errors);
        SynthesizeGraphIfNeeded(definition);
        ValidateDefinition(definition, errors, warnings);
        return CreateValidationResult(normalizedPath, definition, errors, warnings);
    }

    /// <summary>
    ///     Saves a normalized workflow definition to disk.
    /// </summary>
    /// <param name="definition">The workflow definition to save.</param>
    /// <param name="workflowDefinitionPath">The target JSON path.</param>
    public static void SaveToFile(WorkflowDefinition definition, string workflowDefinitionPath)
    {
        ArgumentNullException.ThrowIfNull(definition);
        ArgumentException.ThrowIfNullOrWhiteSpace(workflowDefinitionPath);

        string normalizedPath = Path.GetFullPath(workflowDefinitionPath);
        string? directoryPath = Path.GetDirectoryName(normalizedPath);
        if (!string.IsNullOrWhiteSpace(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        string json = JsonSerializer.Serialize(definition, SerializerOptions);
        File.WriteAllText(normalizedPath, json);
    }

    private static void ValidateDefinition(WorkflowDefinition definition, List<string> errors, List<string> warnings)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.Policy.MaxReviewCycles < 1)
        {
            errors.Add("The workflow definition must set policy.maxReviewCycles to at least 1.");
        }

        if (definition.Policy.MaxRebuilds < 0)
        {
            errors.Add("The workflow definition must set policy.maxRebuilds to 0 or greater.");
        }

        if (definition.Policy.MaxParallelism < 1)
        {
            errors.Add("The workflow definition must set policy.maxParallelism to at least 1.");
        }

        if (definition.Policy.MaxNodeVisits < 1)
        {
            errors.Add("The workflow definition must set policy.maxNodeVisits to at least 1.");
        }

        if (definition.Policy.DecisionMode == WorkflowDecisionMode.Agent && definition.Decision is null)
        {
            errors.Add("The workflow definition must include a decision stage when policy.decisionMode is Agent.");
        }

        ValidatePackage(definition.Package, errors);

        ValidateStageAssets(definition.SharedAssets, "sharedAssets", errors);
        ValidateOptionalIdentifier(definition.Planner?.Agent, "planner.agent", errors);
        ValidateModels(definition.Planner?.Models, "planner.models", errors);
        ValidateOutputNames(definition.Planner?.Outputs, "planner.outputs", errors);
        ValidateStageAssets(definition.Planner?.Assets, "planner.assets", errors);
        ValidateOptionalIdentifier(definition.Builder?.Agent, "builder.agent", errors);
        ValidateModels(definition.Builder?.Models, "builder.models", errors);
        ValidateOutputNames(definition.Builder?.Outputs, "builder.outputs", errors);
        ValidateStageAssets(definition.Builder?.Assets, "builder.assets", errors);
        ValidateOptionalIdentifier(definition.Reviewer?.Agent, "reviewer.agent", errors);
        ValidateModels(definition.Reviewer?.Models, "reviewer.models", errors);
        ValidateOutputNames(definition.Reviewer?.Outputs, "reviewer.outputs", errors);
        ValidateStageAssets(definition.Reviewer?.Assets, "reviewer.assets", errors);
        ValidateOptionalIdentifier(definition.Decision?.Agent, "decision.agent", errors);
        ValidateModels(definition.Decision?.Models, "decision.models", errors);
        ValidateOutputNames(definition.Decision?.Outputs, "decision.outputs", errors);
        ValidateStageAssets(definition.Decision?.Assets, "decision.assets", errors);
        ValidateOptionalIdentifier(definition.Rebuilder?.Agent, "rebuilder.agent", errors);
        ValidateModels(definition.Rebuilder?.Models, "rebuilder.models", errors);
        ValidateOutputNames(definition.Rebuilder?.Outputs, "rebuilder.outputs", errors);
        ValidateStageAssets(definition.Rebuilder?.Assets, "rebuilder.assets", errors);
        ValidateStageAssetsForNodes(definition.Nodes, errors);
        ValidateModelsForNodes(definition.Nodes, errors);
        ValidateOutputsForNodes(definition.Nodes, errors);
        ValidateAgentsForNodes(definition.Nodes, errors);
        ValidateEntryPoints(definition, errors);
        ValidateNodes(definition, errors);
        ValidateInputs(definition.Nodes, errors);
        ValidateGraph(definition, errors, warnings);
    }

    private static WorkflowDefinitionValidationResult CreateValidationResult(
        string workflowDefinitionPath,
        WorkflowDefinition? definition,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
    {
        List<WorkflowDefinitionValidationIssue> issues = [];
        issues.AddRange(errors.Select(error => new WorkflowDefinitionValidationIssue(WorkflowDefinitionValidationSeverity.Error, error)));
        issues.AddRange(warnings.Select(warning => new WorkflowDefinitionValidationIssue(WorkflowDefinitionValidationSeverity.Warning, warning)));
        return new WorkflowDefinitionValidationResult(workflowDefinitionPath, definition, issues);
    }

    private static string FormatValidationMessage(string workflowDefinitionPath, IReadOnlyList<string> errors)
    {
        return $"Workflow definition '{workflowDefinitionPath}' is invalid:{Environment.NewLine}- {string.Join(Environment.NewLine + "- ", errors)}";
    }

    private static void ValidateEntryPoints(WorkflowDefinition definition, List<string> errors)
    {
        if (definition.Nodes.Count == 0)
        {
            errors.Add("The workflow definition must define at least one graph node.");
            return;
        }

        HashSet<string> nodeIds = definition.Nodes.Select(node => node.Id).ToHashSet(StringComparer.OrdinalIgnoreCase);
        HashSet<string> entryPointIds = new(StringComparer.OrdinalIgnoreCase);
        if (definition.EntryPoints.Count == 0)
        {
            errors.Add("The workflow definition must define at least one entry point.");
        }

        foreach (WorkflowEntryPointDefinition entryPoint in definition.EntryPoints)
        {
            if (string.IsNullOrWhiteSpace(entryPoint.Id))
            {
                errors.Add("Each workflow entry point must define an id.");
            }
            else if (!entryPointIds.Add(entryPoint.Id))
            {
                errors.Add($"The workflow entry point id '{entryPoint.Id}' is duplicated.");
            }

            if (string.IsNullOrWhiteSpace(entryPoint.NodeId))
            {
                errors.Add($"The workflow entry point '{entryPoint.Id}' must define a nodeId.");
                continue;
            }

            if (!nodeIds.Contains(entryPoint.NodeId))
            {
                errors.Add($"The workflow entry point '{entryPoint.Id}' targets unknown node '{entryPoint.NodeId}'.");
            }
        }

        string defaultEntryPointId = definition.DefaultEntryPoint
            ?? (definition.EntryPoints.Count > 0 ? definition.EntryPoints[0].Id : string.Empty);
        if (string.IsNullOrWhiteSpace(defaultEntryPointId))
        {
            errors.Add("The workflow definition must define defaultEntryPoint or at least one entry point.");
        }
        else if (!definition.EntryPoints.Any(entryPoint => defaultEntryPointId.Equals(entryPoint.Id, StringComparison.OrdinalIgnoreCase)))
        {
            errors.Add($"The workflow definition defaultEntryPoint '{defaultEntryPointId}' does not match a declared entry point.");
        }
    }

    private static void ValidatePackage(WorkflowPackageDefinition package, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(package);

        if (!string.IsNullOrWhiteSpace(package.Id) && package.Id.Any(char.IsWhiteSpace))
        {
            errors.Add("The workflow package id must not contain whitespace.");
        }

        ValidateOptionalEmail(package.SupportEmail, "package.supportEmail", errors);
        ValidateOptionalAbsoluteUri(package.SupportUrl, "package.supportUrl", errors);
        ValidateOptionalAbsoluteUri(package.RepositoryUrl, "package.repositoryUrl", errors);
        ValidateOptionalAbsoluteUri(package.DocumentationUrl, "package.documentationUrl", errors);

        foreach (KeyValuePair<string, string> metadataEntry in package.Metadata)
        {
            if (string.IsNullOrWhiteSpace(metadataEntry.Key))
            {
                errors.Add("The workflow package metadata keys must not be blank.");
            }

            if (string.IsNullOrWhiteSpace(metadataEntry.Value))
            {
                errors.Add($"The workflow package metadata value for key '{metadataEntry.Key}' must not be blank.");
            }
        }
    }

    private static void ValidateNodes(WorkflowDefinition definition, List<string> errors)
    {
        Dictionary<string, WorkflowNodeDefinition> nodesById = new(StringComparer.OrdinalIgnoreCase);
        foreach (WorkflowNodeDefinition node in definition.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id))
            {
                errors.Add("Each workflow node must define an id.");
                continue;
            }

            if (!nodesById.TryAdd(node.Id, node))
            {
                errors.Add($"The workflow node id '{node.Id}' is duplicated.");
            }
        }

        foreach (WorkflowNodeDefinition node in definition.Nodes)
        {
            ValidateNodeTargets(node, nodesById, errors);
        }
    }

    private static void ValidateInputs(IReadOnlyList<WorkflowNodeDefinition> nodes, List<string> errors)
    {
        HashSet<string> nodeIds = nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.Id))
            .Select(node => node.Id)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (WorkflowNodeDefinition node in nodes)
        {
            for (int index = 0; index < node.Inputs.Count; index++)
            {
                string input = node.Inputs[index];
                if (string.IsNullOrWhiteSpace(input))
                {
                    errors.Add($"The workflow node '{node.Id}' contains a blank input reference.");
                    continue;
                }

                if (string.Equals(input, "request", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(input, "workflow", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!input.StartsWith("node:", StringComparison.OrdinalIgnoreCase))
                {
                    errors.Add($"The workflow node '{node.Id}' uses unsupported input reference '{input}'.");
                    continue;
                }

                string sourceNodeId = input[5..].Trim();
                if (string.IsNullOrWhiteSpace(sourceNodeId))
                {
                    errors.Add($"The workflow node '{node.Id}' contains an empty node input reference '{input}'.");
                    continue;
                }

                if (!nodeIds.Contains(sourceNodeId))
                {
                    errors.Add($"The workflow node '{node.Id}' references unknown input node '{sourceNodeId}'.");
                }
            }

            if (node.Kind == WorkflowNodeKind.Decision
                && !string.IsNullOrWhiteSpace(node.DecisionSourceNodeId)
                && !nodeIds.Contains(node.DecisionSourceNodeId))
            {
                errors.Add($"The decision node '{node.Id}' references unknown decisionSourceNodeId '{node.DecisionSourceNodeId}'.");
            }
        }
    }

    private static void ValidateNodeTargets(
        WorkflowNodeDefinition node,
        Dictionary<string, WorkflowNodeDefinition> nodesById,
        List<string> errors)
    {
        switch (node.Kind)
        {
            case WorkflowNodeKind.Stage:
                ValidateTarget(node.Next, $"node '{node.Id}'.next", nodesById, errors);
                break;

            case WorkflowNodeKind.Decision:
                if (node.Choices.Count == 0)
                {
                    errors.Add($"The decision node '{node.Id}' must define at least one choice.");
                }

                HashSet<string> choiceIds = new(StringComparer.OrdinalIgnoreCase);

                foreach (WorkflowDecisionOptionDefinition choice in node.Choices)
                {
                    if (string.IsNullOrWhiteSpace(choice.Id))
                    {
                        errors.Add($"The decision node '{node.Id}' has a choice without an id.");
                    }
                    else if (!choiceIds.Add(choice.Id))
                    {
                        errors.Add($"The decision node '{node.Id}' contains duplicate choice id '{choice.Id}'.");
                    }

                    ValidateTarget(choice.NextNodeId, $"decision node '{node.Id}' choice '{choice.Id}'", nodesById, errors);
                }

                break;

            case WorkflowNodeKind.Fork:
                if (node.Branches.Count == 0)
                {
                    errors.Add($"The fork node '{node.Id}' must define at least one branch.");
                }

                if (string.IsNullOrWhiteSpace(node.JoinNodeId))
                {
                    errors.Add($"The fork node '{node.Id}' must define joinNodeId.");
                }
                else if (nodesById.TryGetValue(node.JoinNodeId, out WorkflowNodeDefinition? joinNode))
                {
                    if (joinNode.Kind != WorkflowNodeKind.Join)
                    {
                        errors.Add($"The fork node '{node.Id}' joinNodeId '{node.JoinNodeId}' must target a join node.");
                    }
                }
                else
                {
                    errors.Add($"The fork node '{node.Id}' joinNodeId '{node.JoinNodeId}' is unknown.");
                }

                HashSet<string> branchIds = new(StringComparer.OrdinalIgnoreCase);
                foreach (WorkflowForkBranchDefinition branch in node.Branches)
                {
                    if (string.IsNullOrWhiteSpace(branch.Id))
                    {
                        errors.Add($"The fork node '{node.Id}' contains a branch without an id.");
                    }
                    else if (!branchIds.Add(branch.Id))
                    {
                        errors.Add($"The fork node '{node.Id}' contains duplicate branch id '{branch.Id}'.");
                    }

                    ValidateTarget(branch.NextNodeId, $"fork node '{node.Id}' branch '{branch.Id}'", nodesById, errors);
                }

                break;

            case WorkflowNodeKind.Join:
                if (string.IsNullOrWhiteSpace(node.ForkId))
                {
                    errors.Add($"The join node '{node.Id}' must define forkId.");
                }
                else if (nodesById.TryGetValue(node.ForkId, out WorkflowNodeDefinition? forkNode))
                {
                    if (forkNode.Kind != WorkflowNodeKind.Fork)
                    {
                        errors.Add($"The join node '{node.Id}' forkId '{node.ForkId}' must target a fork node.");
                    }
                    else if (!string.Equals(forkNode.JoinNodeId, node.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        errors.Add($"The join node '{node.Id}' must be the joinNodeId referenced by fork '{node.ForkId}'.");
                    }
                }
                else
                {
                    errors.Add($"The join node '{node.Id}' forkId '{node.ForkId}' is unknown.");
                }

                ValidateTarget(node.Next, $"join node '{node.Id}'.next", nodesById, errors);
                break;

            case WorkflowNodeKind.Exit:
                break;

            default:
                errors.Add($"The workflow node '{node.Id}' uses unsupported kind '{node.Kind}'.");
                break;
        }
    }

    private static void ValidateTarget(
        string? nodeId,
        string propertyName,
        Dictionary<string, WorkflowNodeDefinition> nodesById,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(nodeId))
        {
            errors.Add($"The workflow {propertyName} must target a node id.");
            return;
        }

        if (!nodesById.ContainsKey(nodeId))
        {
            errors.Add($"The workflow {propertyName} targets unknown node '{nodeId}'.");
        }
    }

    private static void ValidateStageAssets(IReadOnlyList<WorkflowAssetReference>? assets, string propertyName, List<string> errors)
    {
        if (assets is null)
        {
            return;
        }

        foreach (WorkflowAssetReference asset in assets)
        {
            if (string.IsNullOrWhiteSpace(asset.Kind))
            {
                errors.Add($"The workflow definition asset '{propertyName}' must set a kind.");
            }

            if (string.IsNullOrWhiteSpace(asset.Path))
            {
                errors.Add($"The workflow definition asset '{propertyName}' must set a path.");
            }
        }
    }

    private static void ValidateStageAssetsForNodes(IReadOnlyList<WorkflowNodeDefinition> nodes, List<string> errors)
    {
        foreach (WorkflowNodeDefinition node in nodes)
        {
            ValidateStageAssets(node.Assets, $"nodes[{node.Id}].assets", errors);
        }
    }

    private static void ValidateModelsForNodes(IReadOnlyList<WorkflowNodeDefinition> nodes, List<string> errors)
    {
        foreach (WorkflowNodeDefinition node in nodes)
        {
            ValidateModels(node.Models, $"nodes[{node.Id}].models", errors);
        }
    }

    private static void ValidateOutputsForNodes(IReadOnlyList<WorkflowNodeDefinition> nodes, List<string> errors)
    {
        foreach (WorkflowNodeDefinition node in nodes)
        {
            ValidateOutputNames(node.Outputs, $"nodes[{node.Id}].outputs", errors);
        }
    }

    private static void ValidateAgentsForNodes(IReadOnlyList<WorkflowNodeDefinition> nodes, List<string> errors)
    {
        foreach (WorkflowNodeDefinition node in nodes)
        {
            ValidateOptionalIdentifier(node.Agent, $"nodes[{node.Id}].agent", errors);
        }
    }

    private static void ValidateModels(IReadOnlyList<string>? models, string propertyName, List<string> errors)
    {
        if (models is null)
        {
            return;
        }

        for (int index = 0; index < models.Count; index++)
        {
            if (string.IsNullOrWhiteSpace(models[index]))
            {
                errors.Add($"The workflow definition property '{propertyName}' must not contain blank model identifiers.");
            }
        }
    }

    private static void ValidateOutputNames(IReadOnlyList<string>? outputNames, string propertyName, List<string> errors)
    {
        if (outputNames is null)
        {
            return;
        }

        HashSet<string> normalizedOutputNames = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < outputNames.Count; index++)
        {
            string outputName = outputNames[index];
            if (string.IsNullOrWhiteSpace(outputName))
            {
                errors.Add($"The workflow definition property '{propertyName}' must not contain blank output names.");
                continue;
            }

            string trimmedOutputName = outputName.Trim();
            if (!normalizedOutputNames.Add(trimmedOutputName))
            {
                errors.Add($"The workflow definition property '{propertyName}' contains duplicate output name '{trimmedOutputName}'.");
            }
        }
    }

    private static void ValidateOptionalIdentifier(string? value, string propertyName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (value.Any(char.IsWhiteSpace))
        {
            errors.Add($"The workflow definition property '{propertyName}' must not contain whitespace.");
        }
    }

    private static void ValidateGraph(WorkflowDefinition definition, List<string> errors, List<string> warnings)
    {
        Dictionary<string, WorkflowNodeDefinition> nodesById = definition.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.Id))
            .GroupBy(node => node.Id, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .ToDictionary(node => node.Id, StringComparer.OrdinalIgnoreCase);
        if (nodesById.Count == 0)
        {
            return;
        }

        Dictionary<string, IReadOnlyList<string>> edgesByNodeId = BuildGraphEdges(nodesById);
        string[] exitNodeIds = definition.Nodes
            .Where(node => node.Kind == WorkflowNodeKind.Exit && !string.IsNullOrWhiteSpace(node.Id))
            .Select(node => node.Id)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (exitNodeIds.Length == 0)
        {
            errors.Add("The workflow definition must define at least one exit node.");
            return;
        }

        string[] entryNodeIds = definition.EntryPoints
            .Where(entryPoint => !string.IsNullOrWhiteSpace(entryPoint.NodeId) && nodesById.ContainsKey(entryPoint.NodeId))
            .Select(entryPoint => entryPoint.NodeId)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        HashSet<string> reachableNodeIds = TraverseGraph(entryNodeIds, edgesByNodeId);
        warnings.AddRange(definition.Nodes
            .Where(node => !string.IsNullOrWhiteSpace(node.Id) && !reachableNodeIds.Contains(node.Id))
            .Select(node => $"The workflow node '{node.Id}' is not reachable from any entry point."));

        HashSet<string> exitReachableNodeIds = TraverseReverseGraph(exitNodeIds, edgesByNodeId);
        errors.AddRange(definition.EntryPoints
            .Where(entryPoint => !string.IsNullOrWhiteSpace(entryPoint.NodeId)
                && nodesById.ContainsKey(entryPoint.NodeId)
                && !exitReachableNodeIds.Contains(entryPoint.NodeId))
            .Select(entryPoint => $"The workflow entry point '{entryPoint.Id}' cannot reach any exit node."));

        errors.AddRange(reachableNodeIds
            .Where(nodeId => !exitReachableNodeIds.Contains(nodeId))
            .Select(nodeId => $"The workflow node '{nodeId}' cannot reach any exit node."));

        HashSet<string> problematicNodeIds = reachableNodeIds
            .Where(nodeId => !exitReachableNodeIds.Contains(nodeId))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string>? circularPath = FindCircularPath(problematicNodeIds, edgesByNodeId);
        if (circularPath is not null)
        {
            errors.Add($"The workflow graph contains a circular reference without an exit path: {string.Join(" -> ", circularPath)}.");
        }
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildGraphEdges(IReadOnlyDictionary<string, WorkflowNodeDefinition> nodesById)
    {
        Dictionary<string, IReadOnlyList<string>> edgesByNodeId = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string nodeId, WorkflowNodeDefinition node) in nodesById)
        {
            List<string> nextNodeIds = [];
            switch (node.Kind)
            {
                case WorkflowNodeKind.Stage:
                case WorkflowNodeKind.Join:
                    if (!string.IsNullOrWhiteSpace(node.Next))
                    {
                        nextNodeIds.Add(node.Next);
                    }

                    break;

                case WorkflowNodeKind.Decision:
                    nextNodeIds.AddRange(node.Choices
                        .Where(choice => !string.IsNullOrWhiteSpace(choice.NextNodeId))
                        .Select(choice => choice.NextNodeId));
                    break;

                case WorkflowNodeKind.Fork:
                    nextNodeIds.AddRange(node.Branches
                        .Where(branch => !string.IsNullOrWhiteSpace(branch.NextNodeId))
                        .Select(branch => branch.NextNodeId));
                    break;
            }

            edgesByNodeId[nodeId] = nextNodeIds
                .Where(nodesById.ContainsKey)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        return edgesByNodeId;
    }

    private static HashSet<string> TraverseGraph(IEnumerable<string> startNodeIds, Dictionary<string, IReadOnlyList<string>> edgesByNodeId)
    {
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
        Stack<string> pending = new(startNodeIds.Where(nodeId => !string.IsNullOrWhiteSpace(nodeId)).Reverse());
        while (pending.Count > 0)
        {
            string nodeId = pending.Pop();
            if (!visited.Add(nodeId) || !edgesByNodeId.TryGetValue(nodeId, out IReadOnlyList<string>? nextNodeIds))
            {
                continue;
            }

            for (int index = nextNodeIds.Count - 1; index >= 0; index--)
            {
                pending.Push(nextNodeIds[index]);
            }
        }

        return visited;
    }

    private static HashSet<string> TraverseReverseGraph(IEnumerable<string> targetNodeIds, Dictionary<string, IReadOnlyList<string>> edgesByNodeId)
    {
        Dictionary<string, List<string>> reverseEdgesByNodeId = new(StringComparer.OrdinalIgnoreCase);
        foreach ((string nodeId, IReadOnlyList<string> nextNodeIds) in edgesByNodeId)
        {
            if (!reverseEdgesByNodeId.ContainsKey(nodeId))
            {
                reverseEdgesByNodeId[nodeId] = [];
            }

            foreach (string nextNodeId in nextNodeIds)
            {
                if (!reverseEdgesByNodeId.TryGetValue(nextNodeId, out List<string>? incomingNodeIds))
                {
                    incomingNodeIds = [];
                    reverseEdgesByNodeId[nextNodeId] = incomingNodeIds;
                }

                incomingNodeIds.Add(nodeId);
            }
        }

        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
        Stack<string> pending = new(targetNodeIds.Where(nodeId => !string.IsNullOrWhiteSpace(nodeId)).Reverse());
        while (pending.Count > 0)
        {
            string nodeId = pending.Pop();
            if (!visited.Add(nodeId) || !reverseEdgesByNodeId.TryGetValue(nodeId, out List<string>? previousNodeIds))
            {
                continue;
            }

            for (int index = previousNodeIds.Count - 1; index >= 0; index--)
            {
                pending.Push(previousNodeIds[index]);
            }
        }

        return visited;
    }

    private static List<string>? FindCircularPath(
        IReadOnlySet<string> candidateNodeIds,
        Dictionary<string, IReadOnlyList<string>> edgesByNodeId)
    {
        HashSet<string> visited = new(StringComparer.OrdinalIgnoreCase);
        HashSet<string> active = new(StringComparer.OrdinalIgnoreCase);
        Stack<string> path = new();

        foreach (string nodeId in candidateNodeIds.OrderBy(nodeId => nodeId, StringComparer.OrdinalIgnoreCase))
        {
            List<string>? cycle = FindCircularPath(nodeId, candidateNodeIds, edgesByNodeId, visited, active, path);
            if (cycle is not null)
            {
                return cycle;
            }
        }

        return null;
    }

    private static List<string>? FindCircularPath(
        string nodeId,
        IReadOnlySet<string> candidateNodeIds,
        Dictionary<string, IReadOnlyList<string>> edgesByNodeId,
        ISet<string> visited,
        ISet<string> active,
        Stack<string> path)
    {
        if (!candidateNodeIds.Contains(nodeId))
        {
            return null;
        }

        if (active.Contains(nodeId))
        {
            string[] pathNodes = path.Reverse().ToArray();
            int startIndex = Array.FindIndex(pathNodes, candidate => string.Equals(candidate, nodeId, StringComparison.OrdinalIgnoreCase));
            if (startIndex < 0)
            {
                return [nodeId, nodeId];
            }

            List<string> cycle = pathNodes[startIndex..].ToList();
            cycle.Add(nodeId);
            return cycle;
        }

        if (!visited.Add(nodeId))
        {
            return null;
        }

        active.Add(nodeId);
        path.Push(nodeId);
        if (edgesByNodeId.TryGetValue(nodeId, out IReadOnlyList<string>? nextNodeIds))
        {
            foreach (string nextNodeId in nextNodeIds)
            {
                List<string>? cycle = FindCircularPath(nextNodeId, candidateNodeIds, edgesByNodeId, visited, active, path);
                if (cycle is not null)
                {
                    return cycle;
                }
            }
        }

        path.Pop();
        active.Remove(nodeId);
        return null;
    }

    private static void NormalizeAssetPaths(WorkflowDefinition definition, string baseDirectoryPath, List<string> errors)
    {
        if (definition.Planner is not null)
        {
            definition.Planner.Agent = NormalizeOptionalText(definition.Planner.Agent);
            definition.Planner.Models = NormalizeModels(definition.Planner.Models);
            definition.Planner.Inputs = NormalizeReferences(definition.Planner.Inputs);
            definition.Planner.Outputs = NormalizeNames(definition.Planner.Outputs);
            definition.Planner.CustomMessage = NormalizeOptionalText(definition.Planner.CustomMessage);
        }

        if (definition.Builder is not null)
        {
            definition.Builder.Agent = NormalizeOptionalText(definition.Builder.Agent);
            definition.Builder.Models = NormalizeModels(definition.Builder.Models);
            definition.Builder.Inputs = NormalizeReferences(definition.Builder.Inputs);
            definition.Builder.Outputs = NormalizeNames(definition.Builder.Outputs);
            definition.Builder.CustomMessage = NormalizeOptionalText(definition.Builder.CustomMessage);
        }

        if (definition.Reviewer is not null)
        {
            definition.Reviewer.Agent = NormalizeOptionalText(definition.Reviewer.Agent);
            definition.Reviewer.Models = NormalizeModels(definition.Reviewer.Models);
            definition.Reviewer.Inputs = NormalizeReferences(definition.Reviewer.Inputs);
            definition.Reviewer.Outputs = NormalizeNames(definition.Reviewer.Outputs);
            definition.Reviewer.CustomMessage = NormalizeOptionalText(definition.Reviewer.CustomMessage);
        }

        if (definition.Decision is not null)
        {
            definition.Decision.Agent = NormalizeOptionalText(definition.Decision.Agent);
            definition.Decision.Models = NormalizeModels(definition.Decision.Models);
            definition.Decision.Inputs = NormalizeReferences(definition.Decision.Inputs);
            definition.Decision.Outputs = NormalizeNames(definition.Decision.Outputs);
            definition.Decision.CustomMessage = NormalizeOptionalText(definition.Decision.CustomMessage);
        }

        if (definition.Rebuilder is not null)
        {
            definition.Rebuilder.Agent = NormalizeOptionalText(definition.Rebuilder.Agent);
            definition.Rebuilder.Models = NormalizeModels(definition.Rebuilder.Models);
            definition.Rebuilder.Inputs = NormalizeReferences(definition.Rebuilder.Inputs);
            definition.Rebuilder.Outputs = NormalizeNames(definition.Rebuilder.Outputs);
            definition.Rebuilder.CustomMessage = NormalizeOptionalText(definition.Rebuilder.CustomMessage);
        }

        definition.SharedAssets = NormalizeAssets(definition.SharedAssets, baseDirectoryPath, "sharedAssets", errors);

        if (definition.Planner is not null)
        {
            definition.Planner.Assets = NormalizeAssets(definition.Planner.Assets, baseDirectoryPath, "planner.assets", errors);
        }

        if (definition.Builder is not null)
        {
            definition.Builder.Assets = NormalizeAssets(definition.Builder.Assets, baseDirectoryPath, "builder.assets", errors);
        }

        if (definition.Reviewer is not null)
        {
            definition.Reviewer.Assets = NormalizeAssets(definition.Reviewer.Assets, baseDirectoryPath, "reviewer.assets", errors);
        }

        if (definition.Decision is not null)
        {
            definition.Decision.Assets = NormalizeAssets(definition.Decision.Assets, baseDirectoryPath, "decision.assets", errors);
        }

        if (definition.Rebuilder is not null)
        {
            definition.Rebuilder.Assets = NormalizeAssets(definition.Rebuilder.Assets, baseDirectoryPath, "rebuilder.assets", errors);
        }

        if (definition.Nodes.Count > 0)
        {
            definition.Nodes = definition.Nodes.Select((node, index) =>
            {
                node.Agent = NormalizeOptionalText(node.Agent);
                node.Models = NormalizeModels(node.Models);
                node.Inputs = NormalizeReferences(node.Inputs);
                node.Outputs = NormalizeNames(node.Outputs);
                node.CustomMessage = NormalizeOptionalText(node.CustomMessage);
                node.Assets = NormalizeAssets(node.Assets, baseDirectoryPath, $"nodes[{node.Id ?? index.ToString(System.Globalization.CultureInfo.InvariantCulture)}].assets", errors);
                return node;
            }).ToList();
        }
    }

    private static List<string> NormalizeModels(IReadOnlyList<string>? models)
    {
        if (models is null)
        {
            return [];
        }

        return models
            .Where(model => !string.IsNullOrWhiteSpace(model))
            .Select(model => model.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizeReferences(IReadOnlyList<string>? values)
    {
        if (values is null)
        {
            return [];
        }

        return values
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> NormalizeNames(IReadOnlyList<string>? values)
    {
        return NormalizeReferences(values);
    }

    private static void NormalizePackageMetadata(WorkflowDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        WorkflowPackageDefinition package = definition.Package ?? new WorkflowPackageDefinition();
        string normalizedWorkflowName = NormalizeOptionalText(definition.Name) ?? "Default Workflow";
        string normalizedSchemaVersion = NormalizeOptionalText(definition.Version) ?? "1.0";
        Dictionary<string, string> normalizedMetadata = [];
        foreach (KeyValuePair<string, string> metadataEntry in package.Metadata ?? new Dictionary<string, string>())
        {
            string normalizedKey = NormalizeOptionalText(metadataEntry.Key) ?? string.Empty;
            string normalizedValue = NormalizeOptionalText(metadataEntry.Value) ?? string.Empty;
            normalizedMetadata[normalizedKey] = normalizedValue;
        }

        definition.Name = normalizedWorkflowName;
        definition.Version = normalizedSchemaVersion;
        definition.Package = new WorkflowPackageDefinition
        {
            Id = NormalizeOptionalText(package.Id),
            DisplayName = NormalizeOptionalText(package.DisplayName) ?? normalizedWorkflowName,
            Description = NormalizeOptionalText(package.Description),
            Version = NormalizeOptionalText(package.Version) ?? normalizedSchemaVersion,
            Publisher = NormalizeOptionalText(package.Publisher),
            SupportEmail = NormalizeOptionalText(package.SupportEmail),
            SupportUrl = NormalizeOptionalUri(package.SupportUrl),
            RepositoryUrl = NormalizeOptionalUri(package.RepositoryUrl),
            DocumentationUrl = NormalizeOptionalUri(package.DocumentationUrl),
            Metadata = normalizedMetadata,
        };
    }

    private static void ValidateOptionalAbsoluteUri(Uri? value, string propertyName, List<string> errors)
    {
        if (value is null)
        {
            return;
        }

        if (!value.IsAbsoluteUri || (value.Scheme != Uri.UriSchemeHttp && value.Scheme != Uri.UriSchemeHttps))
        {
            errors.Add($"The workflow definition {propertyName} must be an absolute http or https URL.");
        }
    }

    private static void ValidateOptionalEmail(string? value, string propertyName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        try
        {
            _ = new MailAddress(value);
        }
        catch (FormatException)
        {
            errors.Add($"The workflow definition {propertyName} must be a valid email address.");
        }
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static Uri? NormalizeOptionalUri(Uri? value)
    {
        if (value is null)
        {
            return null;
        }

        string? normalizedValue = NormalizeOptionalText(value.OriginalString);
        return normalizedValue is null ? null : new Uri(normalizedValue, UriKind.RelativeOrAbsolute);
    }

    private static List<WorkflowAssetReference> NormalizeAssets(
        IReadOnlyList<WorkflowAssetReference>? assets,
        string baseDirectoryPath,
        string propertyName,
        List<string> errors)
    {
        if (assets is null)
        {
            return [];
        }

        List<WorkflowAssetReference> normalizedAssets = [];
        for (int index = 0; index < assets.Count; index++)
        {
            WorkflowAssetReference asset = assets[index];
            string normalizedKind = NormalizeOptionalText(asset.Kind) ?? string.Empty;
            string normalizedRelativePath = NormalizeOptionalText(asset.Path) ?? string.Empty;
            string? normalizedHash = NormalizeOptionalText(asset.Hash);
            string normalizedPath = string.IsNullOrWhiteSpace(normalizedRelativePath)
                ? string.Empty
                : Path.GetFullPath(normalizedRelativePath, baseDirectoryPath);

            if (string.IsNullOrWhiteSpace(normalizedPath))
            {
                normalizedAssets.Add(new WorkflowAssetReference(normalizedKind, normalizedPath, normalizedHash));
                continue;
            }

            if (!File.Exists(normalizedPath))
            {
                errors.Add($"The workflow asset '{propertyName}[{index}]' could not be found: {normalizedRelativePath}");
            }
            else if (!string.IsNullOrWhiteSpace(normalizedHash))
            {
                ValidateAssetHash(normalizedPath, normalizedHash, $"{propertyName}[{index}]", errors);
            }

            normalizedAssets.Add(new WorkflowAssetReference(normalizedKind, normalizedPath, normalizedHash));
        }

        return normalizedAssets;
    }

    private static void ValidateAssetHash(string assetPath, string expectedHash, string propertyName, List<string> errors)
    {
        string normalizedExpectedHash = NormalizeHash(expectedHash);
        if (string.IsNullOrWhiteSpace(normalizedExpectedHash))
        {
            errors.Add($"The workflow asset '{propertyName}' defines an unsupported hash '{expectedHash}'. Use a SHA-256 hex digest or 'sha256:<hex>'.");
            return;
        }

        using SHA256 sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(assetPath);
        string actualHash = Convert.ToHexString(sha256.ComputeHash(stream));
        if (!string.Equals(normalizedExpectedHash, actualHash, StringComparison.Ordinal))
        {
            errors.Add($"The workflow asset '{propertyName}' hash does not match for file '{assetPath}'. Expected '{normalizedExpectedHash}' but found '{actualHash}'.");
        }
    }

    private static string NormalizeHash(string value)
    {
        string normalized = value.Trim();
        if (normalized.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[7..].Trim();
        }

        if (normalized.Length != 64 || normalized.Any(character => !Uri.IsHexDigit(character)))
        {
            return string.Empty;
        }

        return normalized.ToUpperInvariant();
    }

    private static void SynthesizeGraphIfNeeded(WorkflowDefinition definition)
    {
        if (definition.Nodes.Count > 0)
        {
            if (string.IsNullOrWhiteSpace(definition.DefaultEntryPoint) && definition.EntryPoints.Count > 0)
            {
                definition.DefaultEntryPoint = definition.EntryPoints[0].Id;
            }

            return;
        }

        if (definition.Planner is null || definition.Builder is null || definition.Reviewer is null || definition.Rebuilder is null)
        {
            return;
        }

        WorkflowNodeDefinition planner = CreateLegacyStageNode(
            "planner",
            definition.Planner,
            WorkflowStage.Planner.ToString(),
            ["request"],
            "builder");
        WorkflowNodeDefinition builder = CreateLegacyStageNode(
            "builder",
            definition.Builder,
            WorkflowStage.Builder.ToString(),
            ["request", "node:planner"],
            "reviewer");
        WorkflowNodeDefinition reviewer = CreateLegacyStageNode(
            "reviewer",
            definition.Reviewer,
            WorkflowStage.Reviewer.ToString(),
            ["request", "node:planner", "node:builder", "node:rebuilder"],
            "review-decision");
        WorkflowNodeDefinition rebuilder = CreateLegacyStageNode(
            "rebuilder",
            definition.Rebuilder,
            WorkflowStage.Rebuilder.ToString(),
            ["request", "node:planner", "node:builder", "node:reviewer", "node:rebuilder"],
            "reviewer");
        WorkflowNodeDefinition decision = new()
        {
            Id = "review-decision",
            Kind = WorkflowNodeKind.Decision,
            DisplayName = definition.Decision?.DisplayName ?? "Review Decision",
            Role = definition.Decision?.Role ?? WorkflowStage.Decision.ToString(),
            Agent = definition.Decision?.Agent,
            Models = definition.Decision?.Models ?? [],
            Assets = definition.Decision?.Assets ?? [],
            Inputs = definition.Decision?.Inputs?.Count > 0
                ? definition.Decision.Inputs
                : ["request", "node:planner", "node:builder", "node:reviewer", "node:rebuilder"],
            Outputs = definition.Decision?.Outputs ?? [],
            CustomMessage = definition.Decision?.CustomMessage,
            DecisionMode = definition.Policy.DecisionMode,
            RuleSet = "legacy-review",
            DecisionSourceNodeId = "reviewer",
            Choices =
            [
                new WorkflowDecisionOptionDefinition { Id = "approve", DisplayName = "Approve", NextNodeId = "approved" },
                new WorkflowDecisionOptionDefinition { Id = "rebuild", DisplayName = "Rebuild", NextNodeId = "rebuilder" },
                new WorkflowDecisionOptionDefinition { Id = "stop", DisplayName = "Stop", NextNodeId = "stopped" },
            ],
        };
        WorkflowNodeDefinition approved = new()
        {
            Id = "approved",
            Kind = WorkflowNodeKind.Exit,
            DisplayName = "Approved Exit",
            ExitStatus = WorkflowRunStatus.Approved,
        };
        WorkflowNodeDefinition stopped = new()
        {
            Id = "stopped",
            Kind = WorkflowNodeKind.Exit,
            DisplayName = "Stopped Exit",
            ExitStatus = WorkflowRunStatus.Stopped,
        };

        definition.Nodes = [planner, builder, reviewer, decision, rebuilder, approved, stopped];
        definition.EntryPoints =
        [
            new WorkflowEntryPointDefinition { Id = "default", NodeId = "planner" },
            new WorkflowEntryPointDefinition { Id = "planner", NodeId = "planner" },
            new WorkflowEntryPointDefinition { Id = "builder", NodeId = "builder" },
            new WorkflowEntryPointDefinition { Id = "review", NodeId = "reviewer" },
            new WorkflowEntryPointDefinition { Id = "decision", NodeId = "review-decision" },
            new WorkflowEntryPointDefinition { Id = "rebuilder", NodeId = "rebuilder" },
        ];
        definition.DefaultEntryPoint ??= "default";
    }

    private static WorkflowNodeDefinition CreateLegacyStageNode(
        string id,
        WorkflowStageDefinition stage,
        string fallbackRole,
        IReadOnlyList<string> defaultInputs,
        string nextNodeId)
    {
        return new WorkflowNodeDefinition
        {
            Id = id,
            Kind = WorkflowNodeKind.Stage,
            DisplayName = stage.DisplayName,
            Role = string.IsNullOrWhiteSpace(stage.Role) ? fallbackRole : stage.Role,
            Agent = stage.Agent,
            Models = stage.Models,
            Assets = stage.Assets,
            Inputs = stage.Inputs.Count > 0 ? stage.Inputs : defaultInputs,
            Outputs = stage.Outputs,
            CustomMessage = stage.CustomMessage,
            Next = nextNodeId,
        };
    }
}

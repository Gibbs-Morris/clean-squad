using System.Collections.Generic;
using CleanSquad.Workflow.Definitions;
using CleanSquad.Workflow.Orchestration;
using CleanSquad.Workflow.Storage;

namespace CleanSquad.Workflow.Decisions;

/// <summary>
///     Provides the information needed to resolve the next workflow action.
/// </summary>
/// <param name="Definition">The loaded workflow definition.</param>
/// <param name="Node">The decision node being evaluated.</param>
/// <param name="Artifacts">The workflow artifacts for the run.</param>
/// <param name="State">The persisted workflow state.</param>
/// <param name="AttachmentFilePaths">The attachment files available to the decision node.</param>
/// <param name="SourceMarkdown">The source markdown used by the decision rules.</param>
public sealed record WorkflowDecisionContext(
    WorkflowDefinition Definition,
    WorkflowNodeDefinition Node,
    WorkflowArtifacts Artifacts,
    WorkflowRunState State,
    IReadOnlyList<string> AttachmentFilePaths,
    string SourceMarkdown);

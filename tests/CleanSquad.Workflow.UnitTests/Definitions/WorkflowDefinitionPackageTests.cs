using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CleanSquad.Workflow.Definitions;

namespace CleanSquad.Workflow.UnitTests.Definitions;

/// <summary>
///     Validates every workflow-definition package checked into the repository.
/// </summary>
public sealed class WorkflowDefinitionPackageTests
{
    /// <summary>
    ///     Verifies every discovered workflow definition has a valid graph, configuration, and asset set.
    /// </summary>
    [Fact]
    public void ValidateAllCheckedInWorkflowDefinitionPackages()
    {
        string repositoryRootPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                ".."));
        string workflowDefinitionsPath = Path.Combine(repositoryRootPath, "workflow-definitions");
        string[] definitionPaths = Directory
            .EnumerateFiles(workflowDefinitionsPath, "workflow.json", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.NotEmpty(definitionPaths);

        List<string> failures = [];
        foreach (string definitionPath in definitionPaths)
        {
            WorkflowDefinitionValidationResult result = WorkflowDefinitionLoader.ValidateFile(definitionPath);
            if (result.IsValid && result.Warnings.Count == 0)
            {
                continue;
            }

            string relativePath = Path.GetRelativePath(repositoryRootPath, definitionPath);
            string[] issues =
            [
                .. result.Errors.Select(error => $"error: {error}"),
                .. result.Warnings.Select(warning => $"warning: {warning}"),
            ];
            failures.Add($"{relativePath}{Environment.NewLine}{string.Join(Environment.NewLine, issues)}");
        }

        Assert.True(
            failures.Count == 0,
            string.Join($"{Environment.NewLine}{Environment.NewLine}", failures));
    }
}

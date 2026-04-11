using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using GitHub.Copilot.SDK;

namespace CleanSquad.Core.Workflows;

/// <summary>
///     Executes workflow stages by calling the GitHub Copilot SDK.
/// </summary>
public sealed class CopilotWorkflowAgentRunner : IWorkflowAgentRunner
{
    private readonly string workspaceRootPath;

    /// <summary>
    ///     Initializes a new instance of the <see cref="CopilotWorkflowAgentRunner" /> class.
    /// </summary>
    /// <param name="workspaceRootPath">The workspace root path used as the Copilot CLI working directory.</param>
    public CopilotWorkflowAgentRunner(string workspaceRootPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workspaceRootPath);
        this.workspaceRootPath = Path.GetFullPath(workspaceRootPath);
    }

    /// <inheritdoc />
    public async Task<string> RunAsync(
        WorkflowStage stage,
        string prompt,
        IReadOnlyList<string> attachmentFilePaths,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);
        ArgumentNullException.ThrowIfNull(attachmentFilePaths);
        cancellationToken.ThrowIfCancellationRequested();
        string promptWithContext = await BuildPromptWithContextAsync(prompt, attachmentFilePaths, cancellationToken);

        await using var client = new CopilotClient(new CopilotClientOptions
        {
            Cwd = this.workspaceRootPath,
        });

        await using var session = await client.CreateSessionAsync(
            new SessionConfig
            {
                OnPermissionRequest = (_, _) => Task.FromResult(new PermissionRequestResult
                {
                    Kind = PermissionRequestResultKind.DeniedByRules,
                }),
            },
            cancellationToken);

        AssistantMessageEvent? reply = await session.SendAndWaitAsync(
            new MessageOptions
            {
                Prompt = promptWithContext,
            },
            null,
            cancellationToken);

        string? content = reply?.Data.Content;
        if (string.IsNullOrWhiteSpace(content))
        {
            throw new InvalidOperationException($"The {stage} agent returned an empty response.");
        }

        return content.Trim();
    }

    private static async Task<string> BuildPromptWithContextAsync(
        string prompt,
        IReadOnlyList<string> attachmentFilePaths,
        CancellationToken cancellationToken)
    {
        StringBuilder builder = new();
        builder.AppendLine(prompt.Trim());
        builder.AppendLine();
        builder.AppendLine("## Allowed Markdown Context");
        builder.AppendLine();

        foreach (string filePath in attachmentFilePaths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string fileName = Path.GetFileName(filePath);
            string markdownContent = await File.ReadAllTextAsync(filePath, cancellationToken);

            builder.Append("### ");
            builder.AppendLine(fileName);
            builder.AppendLine();
            builder.AppendLine(markdownContent.Trim());
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}

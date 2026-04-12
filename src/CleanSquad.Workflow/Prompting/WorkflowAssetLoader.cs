using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Workflow.Definitions;

namespace CleanSquad.Workflow.Prompting;

/// <summary>
///     Loads workflow asset files from disk.
/// </summary>
public static class WorkflowAssetLoader
{
    /// <summary>
    ///     Loads the configured workflow assets as markdown blocks.
    /// </summary>
    /// <param name="assets">The assets to load.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The combined asset markdown.</returns>
    public static async Task<string> LoadMarkdownBlocksAsync(IReadOnlyList<WorkflowAssetReference> assets, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);

        StringBuilder builder = new();
        foreach (WorkflowAssetReference asset in assets)
        {
            cancellationToken.ThrowIfCancellationRequested();
            string content = await File.ReadAllTextAsync(asset.Path, cancellationToken);
            builder.Append("### ");
            builder.Append(asset.Kind);
            builder.Append(": ");
            builder.AppendLine(Path.GetFileName(asset.Path));
            builder.AppendLine();
            builder.AppendLine(content.Trim());
            builder.AppendLine();
        }

        return builder.ToString().TrimEnd();
    }
}

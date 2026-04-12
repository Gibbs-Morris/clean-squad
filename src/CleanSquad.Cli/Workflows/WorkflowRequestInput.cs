using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace CleanSquad.Cli.Workflows;

/// <summary>
///     Represents a workflow request document resolved for a CLI workflow run.
/// </summary>
/// <remarks>
///     Inline request text is materialized to a temporary markdown file so the existing workflow runtime can remain path-based.
/// </remarks>
internal sealed class WorkflowRequestInput : IDisposable
{
    private static readonly UTF8Encoding Utf8WithoutBom = new(false);

    private readonly string? temporaryDirectoryPath;

    private WorkflowRequestInput(string requestDocumentPath, string? temporaryDirectoryPath)
    {
        this.RequestDocumentPath = requestDocumentPath;
        this.temporaryDirectoryPath = temporaryDirectoryPath;
    }

    /// <summary>
    ///     Gets the markdown request document path that should be passed to the workflow runtime.
    /// </summary>
    internal string RequestDocumentPath { get; }

    /// <summary>
    ///     Creates a resolved request that points at an existing markdown file.
    /// </summary>
    /// <param name="requestDocumentPath">The existing markdown file path.</param>
    /// <returns>The resolved request input.</returns>
    internal static WorkflowRequestInput UseExistingFile(string requestDocumentPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestDocumentPath);

        return new WorkflowRequestInput(Path.GetFullPath(requestDocumentPath), null);
    }

    /// <summary>
    ///     Creates a resolved request by writing inline markdown to a temporary file.
    /// </summary>
    /// <param name="requestMarkdown">The inline markdown request content.</param>
    /// <returns>The resolved request input.</returns>
    internal static WorkflowRequestInput CreateTemporaryFromMarkdown(string requestMarkdown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestMarkdown);

        string temporaryDirectoryPath = Path.Combine(Path.GetTempPath(), "clean-squad", "inline-requests", Guid.NewGuid().ToString("N", System.Globalization.CultureInfo.InvariantCulture));
        Directory.CreateDirectory(temporaryDirectoryPath);

        string requestDocumentPath = Path.Combine(temporaryDirectoryPath, "inline-request.md");
        File.WriteAllText(requestDocumentPath, NormalizeMarkdown(requestMarkdown), Utf8WithoutBom);
        return new WorkflowRequestInput(requestDocumentPath, temporaryDirectoryPath);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (string.IsNullOrWhiteSpace(this.temporaryDirectoryPath) || !Directory.Exists(this.temporaryDirectoryPath))
        {
            return;
        }

        try
        {
            Directory.Delete(this.temporaryDirectoryPath, true);
        }
        catch (IOException)
        {
            Debug.WriteLine($"Failed to delete temporary workflow request directory '{this.temporaryDirectoryPath}'.");
        }
        catch (UnauthorizedAccessException)
        {
            Debug.WriteLine($"Failed to delete temporary workflow request directory '{this.temporaryDirectoryPath}'.");
        }
    }

    private static string NormalizeMarkdown(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        string normalized = content.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');

        return normalized.TrimEnd() + "\n";
    }
}

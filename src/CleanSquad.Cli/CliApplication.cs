using System;
using System.CommandLine;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CleanSquad.Cli.Workflows;
using CleanSquad.Core;
using CleanSquad.Workflow;
using CleanSquad.Workflow.Definitions;
using CleanSquad.Workflow.Infrastructure;
using Microsoft.Extensions.Logging;

namespace CleanSquad.Cli;

/// <summary>
///     Coordinates the CleanSquad command-line output.
/// </summary>
internal static partial class CliApplication
{
    private const string WorkflowRunUsage = "Usage: workflow run --definition <path-to-workflow.json> (<path-to-request.md> | --request-text <markdown>) [--entry-point <entry-or-node-id>]";
    private const string WorkflowResumeUsage = "Usage: workflow resume <run-folder-or-state.json> [--entry-point <entry-or-node-id>]";
    private const string WorkflowValidateUsage = "Usage: workflow validate --definition <path-to-workflow.json>";
    private const string WorkflowDiagramUsage = "Usage: workflow diagram --definition <path-to-workflow.json> [--output <path-to-diagram.md>]";

    /// <summary>
    ///     Builds the output text for the provided command-line arguments.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <returns>The message that should be printed by the CLI.</returns>
    internal static string BuildOutput(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        CliBrandingOptions brandingOptions = CliBrandingOptionsLoader.Load(Directory.GetCurrentDirectory());
        string? squadName = args.Length > 0 ? args[0] : null;
        return BuildSummaryOutput(squadName, brandingOptions);
    }

    /// <summary>
    ///     Invokes the CLI using the configured command-line parser.
    /// </summary>
    /// <param name="args">Optional command-line arguments.</param>
    /// <param name="workflowOrchestrator">Optional workflow orchestrator for testing.</param>
    /// <param name="currentDirectory">Optional current directory override for testing.</param>
    /// <param name="outputWriter">Optional output writer override for testing.</param>
    /// <param name="loggerFactory">Optional logger factory override for testing or host integration.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The process exit code.</returns>
    internal static async Task<int> InvokeAsync(
        string[] args,
        IWorkflowOrchestrator? workflowOrchestrator = null,
        string? currentDirectory = null,
        TextWriter? outputWriter = null,
        ILoggerFactory? loggerFactory = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args);

        TextWriter effectiveOutputWriter = outputWriter ?? Console.Out;
        bool enableConsoleLogging = workflowOrchestrator is null && outputWriter is null;
        string effectiveCurrentDirectory = currentDirectory ?? Directory.GetCurrentDirectory();
        CliBrandingOptions brandingOptions = CliBrandingOptionsLoader.Load(effectiveCurrentDirectory);

        if (loggerFactory is not null)
        {
            return await InvokeWithLoggerFactoryAsync(
                args,
                workflowOrchestrator,
                effectiveCurrentDirectory,
                brandingOptions,
                effectiveOutputWriter,
                loggerFactory,
                cancellationToken);
        }

        using ILoggerFactory ownedLoggerFactory = CreateLoggerFactory(enableConsoleLogging);
        return await InvokeWithLoggerFactoryAsync(
            args,
            workflowOrchestrator,
            effectiveCurrentDirectory,
            brandingOptions,
            effectiveOutputWriter,
            ownedLoggerFactory,
            cancellationToken);
    }

    private static async Task<int> InvokeWithLoggerFactoryAsync(
        string[] args,
        IWorkflowOrchestrator? workflowOrchestrator,
        string currentDirectory,
        CliBrandingOptions brandingOptions,
        TextWriter outputWriter,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        RootCommand rootCommand = CreateRootCommand(
            outputWriter,
            workflowOrchestrator,
            currentDirectory,
            brandingOptions,
            loggerFactory,
            cancellationToken);

#pragma warning disable CA2016 // System.CommandLine.InvokeAsync does not expose a CancellationToken overload.
        return await rootCommand.Parse(args).InvokeAsync().WaitAsync(cancellationToken);
#pragma warning restore CA2016
    }

    private static RootCommand CreateRootCommand(
        TextWriter outputWriter,
        IWorkflowOrchestrator? workflowOrchestrator,
        string? currentDirectory,
        CliBrandingOptions brandingOptions,
        ILoggerFactory loggerFactory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(outputWriter);
        ArgumentNullException.ThrowIfNull(brandingOptions);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        ILogger logger = loggerFactory.CreateLogger(typeof(CliApplication).FullName ?? nameof(CliApplication));

        Argument<string?> squadNameArgument = new("squad-name")
        {
            Description = $"Optional squad name for the {brandingOptions.ApplicationName} starter summary output.",
            Arity = ArgumentArity.ZeroOrOne,
        };
        RootCommand rootCommand = new(brandingOptions.ApplicationDescription ?? $"{brandingOptions.ApplicationName} command-line interface.");
        rootCommand.Arguments.Add(squadNameArgument);
        rootCommand.SetAction(parseResult =>
        {
            string? squadName = parseResult.GetValue(squadNameArgument);
            outputWriter.WriteLine(BuildSummaryOutput(squadName, brandingOptions));
            return 0;
        });

        Argument<string?> requestDocumentArgument = new("request-document")
        {
            Description = "Optional path to the markdown request document.",
            Arity = ArgumentArity.ZeroOrOne,
        };
        Option<string> workflowDefinitionOption = new("--definition", "-d")
        {
            Description = "Path to the workflow definition JSON file.",
            Required = true,
        };
        Option<string?> workspaceRootOption = new("--workspace-root", "-w")
        {
            Description = "Optional workspace root used for workflow artifacts and the agent runtime working directory.",
        };
        Option<string?> outputPathOption = new("--output", "-o")
        {
            Description = "Optional markdown output path for generated artifacts.",
        };
        Option<string?> entryPointOption = new("--entry-point", "-e")
        {
            Description = "Optional workflow entry point id or node id to start from.",
        };
        Option<string?> requestTextOption = new("--request-text", "-t")
        {
            Description = "Optional inline markdown request text. Specify this or the request-document argument.",
        };
        Command runWorkflowCommand = new("run", "Run a JSON-defined workflow for a markdown request file or inline request text.");
        runWorkflowCommand.Arguments.Add(requestDocumentArgument);
        runWorkflowCommand.Options.Add(workflowDefinitionOption);
        runWorkflowCommand.Options.Add(workspaceRootOption);
        runWorkflowCommand.Options.Add(entryPointOption);
        runWorkflowCommand.Options.Add(requestTextOption);
        runWorkflowCommand.SetAction(async parseResult =>
        {
            string? requestDocumentPath = parseResult.GetValue(requestDocumentArgument);
            string? requestText = parseResult.GetValue(requestTextOption);
            string workflowDefinitionPath = parseResult.GetValue(workflowDefinitionOption) ?? string.Empty;
            string? workspaceRootPath = parseResult.GetValue(workspaceRootOption);
            string? entryPointId = parseResult.GetValue(entryPointOption);
            CliCommandResult result = await ExecuteWorkflowRunCommandAsync(
                workflowDefinitionPath,
                requestDocumentPath,
                requestText,
                workspaceRootPath,
                entryPointId,
                workflowOrchestrator,
                currentDirectory,
                loggerFactory,
                logger,
                cancellationToken);
            await outputWriter.WriteLineAsync(result.Message);
            return result.ExitCode;
        });
        Command validateWorkflowCommand = new("validate", "Validate a workflow definition and its referenced assets.");
        validateWorkflowCommand.Options.Add(workflowDefinitionOption);
        validateWorkflowCommand.SetAction(async parseResult =>
        {
            string workflowDefinitionPath = parseResult.GetValue(workflowDefinitionOption) ?? string.Empty;
            CliCommandResult result = ExecuteWorkflowValidateCommand(
                workflowDefinitionPath,
                currentDirectory,
                logger);
            await outputWriter.WriteLineAsync(result.Message);
            return result.ExitCode;
        });
        Command diagramWorkflowCommand = new("diagram", "Generate a markdown file containing a Mermaid diagram for a workflow definition.");
        diagramWorkflowCommand.Options.Add(workflowDefinitionOption);
        diagramWorkflowCommand.Options.Add(outputPathOption);
        diagramWorkflowCommand.SetAction(async parseResult =>
        {
            string workflowDefinitionPath = parseResult.GetValue(workflowDefinitionOption) ?? string.Empty;
            string? outputPath = parseResult.GetValue(outputPathOption);
            CliCommandResult result = ExecuteWorkflowDiagramCommand(
                workflowDefinitionPath,
                outputPath,
                currentDirectory,
                logger);
            await outputWriter.WriteLineAsync(result.Message);
            return result.ExitCode;
        });
        Argument<string> resumeTargetArgument = new("run-path")
        {
            Description = "Path to the workflow run folder or state.json file to resume.",
        };
        Command resumeWorkflowCommand = new("resume", "Resume a persisted workflow run.");
        resumeWorkflowCommand.Arguments.Add(resumeTargetArgument);
        resumeWorkflowCommand.Options.Add(workspaceRootOption);
        resumeWorkflowCommand.Options.Add(entryPointOption);
        resumeWorkflowCommand.SetAction(async parseResult =>
        {
            string resumeTargetPath = parseResult.GetValue(resumeTargetArgument) ?? string.Empty;
            string? workspaceRootPath = parseResult.GetValue(workspaceRootOption);
            string? entryPointId = parseResult.GetValue(entryPointOption);
            CliCommandResult result = await ExecuteWorkflowResumeCommandAsync(
                resumeTargetPath,
                workspaceRootPath,
                entryPointId,
                workflowOrchestrator,
                currentDirectory,
                loggerFactory,
                logger,
                cancellationToken);
            await outputWriter.WriteLineAsync(result.Message);
            return result.ExitCode;
        });
        Command workflowCommand = new("workflow", brandingOptions.WorkflowCommandDescription ?? "Run the workflow engine.");
        workflowCommand.Subcommands.Add(runWorkflowCommand);
        workflowCommand.Subcommands.Add(validateWorkflowCommand);
        workflowCommand.Subcommands.Add(diagramWorkflowCommand);
        workflowCommand.Subcommands.Add(resumeWorkflowCommand);
        rootCommand.Subcommands.Add(workflowCommand);

        return rootCommand;
    }

    private static async Task<CliCommandResult> ExecuteWorkflowRunCommandAsync(
        string workflowDefinitionArgument,
        string? requestDocumentArgument,
        string? requestTextArgument,
        string? workspaceRootToken,
        string? entryPointId,
        IWorkflowOrchestrator? workflowOrchestrator,
        string? currentDirectory,
        ILoggerFactory loggerFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(workflowDefinitionArgument))
        {
            LogWarningMessage(logger, "Workflow run rejected because the definition path argument was missing.");
            return new CliCommandResult(1, WorkflowRunUsage);
        }

        bool hasRequestDocumentPath = !string.IsNullOrWhiteSpace(requestDocumentArgument);
        bool hasRequestText = !string.IsNullOrWhiteSpace(requestTextArgument);

        if (!hasRequestDocumentPath && !hasRequestText)
        {
            LogWarningMessage(logger, "Workflow run rejected because neither a request document path nor inline request text was supplied.");
            return new CliCommandResult(1, WorkflowRunUsage);
        }

        if (hasRequestDocumentPath && hasRequestText)
        {
            LogWarningMessage(logger, "Workflow run rejected because both a request document path and inline request text were supplied.");
            return new CliCommandResult(1, "Specify either a markdown (.md) request document path or --request-text, but not both.");
        }

        string workingDirectory = currentDirectory ?? Directory.GetCurrentDirectory();
        string workflowDefinitionPath = Path.GetFullPath(workflowDefinitionArgument, workingDirectory);
        if (!File.Exists(workflowDefinitionPath))
        {
            LogWarningMessage(logger, $"Workflow run rejected because the definition file {workflowDefinitionPath} was not found.");
            return new CliCommandResult(1, $"Workflow definition file not found: {workflowDefinitionPath}");
        }

        if (!string.Equals(Path.GetExtension(workflowDefinitionPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            LogWarningMessage(logger, $"Workflow run rejected because the definition file {workflowDefinitionPath} was not JSON.");
            return new CliCommandResult(1, "The workflow command requires a JSON workflow definition path.");
        }

        if (hasRequestDocumentPath)
        {
            string providedRequestDocumentPath = Path.GetFullPath(requestDocumentArgument!, workingDirectory);
            if (!File.Exists(providedRequestDocumentPath))
            {
                LogWarningMessage(logger, $"Workflow run rejected because the request file {providedRequestDocumentPath} was not found.");
                return new CliCommandResult(1, $"Request markdown file not found: {providedRequestDocumentPath}");
            }

            if (!string.Equals(Path.GetExtension(providedRequestDocumentPath), ".md", StringComparison.OrdinalIgnoreCase))
            {
                LogWarningMessage(logger, $"Workflow run rejected because the request file {providedRequestDocumentPath} was not markdown.");
                return new CliCommandResult(1, "The workflow command requires a markdown (.md) request document path.");
            }
        }

        string workspaceRootPath = string.IsNullOrWhiteSpace(workspaceRootToken)
            ? WorkspaceRootLocator.FindWorkspaceRoot(workingDirectory)
            : Path.GetFullPath(workspaceRootToken, workingDirectory);
        IWorkflowOrchestrator orchestrator = workflowOrchestrator ?? CreateWorkflowOrchestrator(workspaceRootPath, loggerFactory);
        using WorkflowRequestInput requestInput = hasRequestText
            ? WorkflowRequestInput.CreateTemporaryFromMarkdown(requestTextArgument!)
            : WorkflowRequestInput.UseExistingFile(Path.GetFullPath(requestDocumentArgument!, workingDirectory));

        string requestDocumentPath = requestInput.RequestDocumentPath;
        try
        {
            LogInformationMessage(
                logger,
                $"Starting workflow run using definition {workflowDefinitionPath}, request {requestDocumentPath}, and workspace root {workspaceRootPath}.");
            WorkflowRunResult result = await orchestrator.ExecuteAsync(
                new WorkflowExecutionRequest(
                    workspaceRootPath,
                    workflowDefinitionPath,
                    requestDocumentPath,
                    entryPointId),
                cancellationToken);

            LogInformationMessage(logger, $"Workflow run completed with status {result.Status}.");

            return new CliCommandResult(
                0,
                $"Workflow {result.StatusLabel}. Run folder: {result.RunDirectoryPath}. Final markdown: {result.FinalArtifactPath}. State: {result.StateArtifactPath}");
        }
        catch (IOException ioException)
        {
            LogErrorMessage(logger, ioException, "Workflow run failed because of an I/O error.");
            return new CliCommandResult(1, $"Workflow failed: {ioException.Message}");
        }
        catch (InvalidOperationException invalidOperationException)
        {
            LogErrorMessage(logger, invalidOperationException, "Workflow run failed because the runtime entered an invalid state.");
            return new CliCommandResult(1, $"Workflow failed: {invalidOperationException.Message}");
        }
    }

    private static async Task<CliCommandResult> ExecuteWorkflowResumeCommandAsync(
        string resumeTargetArgument,
        string? workspaceRootToken,
        string? entryPointId,
        IWorkflowOrchestrator? workflowOrchestrator,
        string? currentDirectory,
        ILoggerFactory loggerFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(resumeTargetArgument))
        {
            LogWarningMessage(logger, "Workflow resume rejected because the run path argument was missing.");
            return new CliCommandResult(1, WorkflowResumeUsage);
        }

        string workingDirectory = currentDirectory ?? Directory.GetCurrentDirectory();
        string resumeTargetPath = Path.GetFullPath(resumeTargetArgument, workingDirectory);
        if (!Directory.Exists(resumeTargetPath) && !File.Exists(resumeTargetPath))
        {
            LogWarningMessage(logger, $"Workflow resume rejected because the run path {resumeTargetPath} was not found.");
            return new CliCommandResult(1, $"Workflow run path not found: {resumeTargetPath}");
        }

        string workspaceRootPath = string.IsNullOrWhiteSpace(workspaceRootToken)
            ? WorkspaceRootLocator.FindWorkspaceRoot(workingDirectory)
            : Path.GetFullPath(workspaceRootToken, workingDirectory);
        IWorkflowOrchestrator orchestrator = workflowOrchestrator ?? CreateWorkflowOrchestrator(workspaceRootPath, loggerFactory);
        try
        {
            LogInformationMessage(
                logger,
                $"Resuming workflow run from {resumeTargetPath} using workspace root {workspaceRootPath}.");
            WorkflowRunResult result = await orchestrator.ExecuteAsync(
                new WorkflowExecutionRequest(
                    workspaceRootPath,
                    null,
                    null,
                    entryPointId,
                    resumeTargetPath),
                cancellationToken);

            LogInformationMessage(logger, $"Workflow resume completed with status {result.Status}.");

            return new CliCommandResult(
                0,
                $"Workflow {result.StatusLabel}. Run folder: {result.RunDirectoryPath}. Final markdown: {result.FinalArtifactPath}. State: {result.StateArtifactPath}");
        }
        catch (IOException ioException)
        {
            LogErrorMessage(logger, ioException, "Workflow resume failed because of an I/O error.");
            return new CliCommandResult(1, $"Workflow failed: {ioException.Message}");
        }
        catch (InvalidOperationException invalidOperationException)
        {
            LogErrorMessage(logger, invalidOperationException, "Workflow resume failed because the runtime entered an invalid state.");
            return new CliCommandResult(1, $"Workflow failed: {invalidOperationException.Message}");
        }
    }

    private static CliCommandResult ExecuteWorkflowValidateCommand(
        string workflowDefinitionArgument,
        string? currentDirectory,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(workflowDefinitionArgument))
        {
            LogWarningMessage(logger, "Workflow validation rejected because the definition path argument was missing.");
            return new CliCommandResult(1, WorkflowValidateUsage);
        }

        string workingDirectory = currentDirectory ?? Directory.GetCurrentDirectory();
        string workflowDefinitionPath = Path.GetFullPath(workflowDefinitionArgument, workingDirectory);
        if (!File.Exists(workflowDefinitionPath))
        {
            LogWarningMessage(logger, $"Workflow validation rejected because the definition file {workflowDefinitionPath} was not found.");
            return new CliCommandResult(1, $"Workflow definition file not found: {workflowDefinitionPath}");
        }

        if (!string.Equals(Path.GetExtension(workflowDefinitionPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            LogWarningMessage(logger, $"Workflow validation rejected because the definition file {workflowDefinitionPath} was not JSON.");
            return new CliCommandResult(1, "The workflow validate command requires a JSON workflow definition path.");
        }

        WorkflowDefinitionValidationResult validationResult = WorkflowDefinitionLoader.ValidateFile(workflowDefinitionPath);
        LogInformationMessage(
            logger,
            $"Workflow validation completed for {workflowDefinitionPath} with {validationResult.Errors.Count} errors and {validationResult.Warnings.Count} warnings.");
        return new CliCommandResult(validationResult.IsValid ? 0 : 1, FormatValidationOutput(validationResult));
    }

    private static CliCommandResult ExecuteWorkflowDiagramCommand(
        string workflowDefinitionArgument,
        string? outputPathArgument,
        string? currentDirectory,
        ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(workflowDefinitionArgument))
        {
            LogWarningMessage(logger, "Workflow diagram generation rejected because the definition path argument was missing.");
            return new CliCommandResult(1, WorkflowDiagramUsage);
        }

        string workingDirectory = currentDirectory ?? Directory.GetCurrentDirectory();
        string workflowDefinitionPath = Path.GetFullPath(workflowDefinitionArgument, workingDirectory);
        if (!File.Exists(workflowDefinitionPath))
        {
            LogWarningMessage(logger, $"Workflow diagram generation rejected because the definition file {workflowDefinitionPath} was not found.");
            return new CliCommandResult(1, $"Workflow definition file not found: {workflowDefinitionPath}");
        }

        if (!string.Equals(Path.GetExtension(workflowDefinitionPath), ".json", StringComparison.OrdinalIgnoreCase))
        {
            LogWarningMessage(logger, $"Workflow diagram generation rejected because the definition file {workflowDefinitionPath} was not JSON.");
            return new CliCommandResult(1, "The workflow diagram command requires a JSON workflow definition path.");
        }

        string outputPath = ResolveWorkflowDiagramOutputPath(workflowDefinitionPath, outputPathArgument, workingDirectory);
        string extension = Path.GetExtension(outputPath);
        if (!string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(extension, ".markdown", StringComparison.OrdinalIgnoreCase))
        {
            LogWarningMessage(logger, $"Workflow diagram generation rejected because the output file {outputPath} was not markdown.");
            return new CliCommandResult(1, "The workflow diagram command requires a markdown output path ending in .md or .markdown.");
        }

        try
        {
            WorkflowDefinition definition = WorkflowDefinitionLoader.LoadFromFile(workflowDefinitionPath);
            string markdown = WorkflowDiagramRenderer.RenderMarkdown(definition, workflowDefinitionPath);
            string? outputDirectoryPath = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrWhiteSpace(outputDirectoryPath))
            {
                Directory.CreateDirectory(outputDirectoryPath);
            }

            File.WriteAllText(outputPath, markdown);
            LogInformationMessage(
                logger,
                $"Workflow diagram markdown generated from {workflowDefinitionPath} to {outputPath}.");
            return new CliCommandResult(0, $"Workflow diagram markdown written to: {outputPath}");
        }
        catch (IOException ioException)
        {
            LogErrorMessage(logger, ioException, "Workflow diagram generation failed because of an I/O error.");
            return new CliCommandResult(1, $"Workflow diagram generation failed: {ioException.Message}");
        }
        catch (InvalidOperationException invalidOperationException)
        {
            LogErrorMessage(logger, invalidOperationException, "Workflow diagram generation failed because the workflow definition was invalid.");
            return new CliCommandResult(1, $"Workflow diagram generation failed: {invalidOperationException.Message}");
        }
    }

    private static string BuildSummaryOutput(string? squadName, CliBrandingOptions brandingOptions)
    {
        ArgumentNullException.ThrowIfNull(brandingOptions);

        return CleanupChecklistService.CreateSummary(squadName, brandingOptions.Checklist);
    }

    private static string FormatValidationOutput(WorkflowDefinitionValidationResult validationResult)
    {
        ArgumentNullException.ThrowIfNull(validationResult);

        StringBuilder builder = new();
        builder.AppendLine(validationResult.IsValid ? "Workflow definition is valid." : "Workflow definition is invalid.");
        builder.Append("Definition: ").AppendLine(validationResult.WorkflowDefinitionPath);

        if (validationResult.Definition is not null)
        {
            builder.Append("Name: ").AppendLine(validationResult.Definition.Name);
            builder.Append("Nodes: ").AppendLine(validationResult.NodeCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append("Entry points: ").AppendLine(validationResult.EntryPointCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append("Assets: ").AppendLine(validationResult.AssetCount.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        if (validationResult.Errors.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Errors:");
            foreach (string error in validationResult.Errors)
            {
                builder.Append("- ").AppendLine(error);
            }
        }

        if (validationResult.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Warnings:");
            foreach (string warning in validationResult.Warnings)
            {
                builder.Append("- ").AppendLine(warning);
            }
        }

        return builder.ToString().TrimEnd();
    }

    private static string ResolveWorkflowDiagramOutputPath(string workflowDefinitionPath, string? outputPathArgument, string workingDirectory)
    {
        if (!string.IsNullOrWhiteSpace(outputPathArgument))
        {
            return Path.GetFullPath(outputPathArgument, workingDirectory);
        }

        string definitionDirectoryPath = Path.GetDirectoryName(workflowDefinitionPath) ?? workingDirectory;
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(workflowDefinitionPath);
        return Path.Combine(definitionDirectoryPath, $"{fileNameWithoutExtension}.diagram.md");
    }

    private static ILoggerFactory CreateLoggerFactory(bool enableConsoleLogging)
    {
        return LoggerFactory.Create(builder =>
        {
            if (enableConsoleLogging)
            {
                builder.AddSimpleConsole(options =>
                {
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
                builder.SetMinimumLevel(LogLevel.Information);
                return;
            }

            builder.SetMinimumLevel(LogLevel.None);
        });
    }

    private static IWorkflowOrchestrator CreateWorkflowOrchestrator(string workspaceRootPath, ILoggerFactory loggerFactory)
    {
        return WorkflowOrchestratorFactory.Create(workspaceRootPath, TimeProvider.System, loggerFactory);
    }

    [LoggerMessage(EventId = 1000, Level = LogLevel.Warning, Message = "{Message}")]
    private static partial void LogWarningMessage(ILogger logger, string message);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "{Message}")]
    private static partial void LogInformationMessage(ILogger logger, string message);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Error, Message = "{Message}")]
    private static partial void LogErrorMessage(ILogger logger, Exception exception, string message);

    private sealed record CliCommandResult(int ExitCode, string Message);
}

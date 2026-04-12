using System;
using System.Collections.Generic;

namespace CleanSquad.Workflow.Definitions;

/// <summary>
///     Describes the distributable package metadata for a workflow definition.
/// </summary>
public sealed class WorkflowPackageDefinition
{
    /// <summary>
    ///     Gets or sets the stable package identifier.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    ///     Gets or sets the branded display name shown to users.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    ///     Gets or sets the optional package description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    ///     Gets or sets the package version that can evolve independently of the schema version.
    /// </summary>
    public string? Version { get; set; }

    /// <summary>
    ///     Gets or sets the organization or publisher that owns the workflow package.
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    ///     Gets or sets the support email address for the workflow package.
    /// </summary>
    public string? SupportEmail { get; set; }

    /// <summary>
    ///     Gets or sets the support URL for the workflow package.
    /// </summary>
    public Uri? SupportUrl { get; set; }

    /// <summary>
    ///     Gets or sets the repository URL for the workflow package.
    /// </summary>
    public Uri? RepositoryUrl { get; set; }

    /// <summary>
    ///     Gets or sets the documentation URL for the workflow package.
    /// </summary>
    public Uri? DocumentationUrl { get; set; }

    /// <summary>
    ///     Gets or sets arbitrary organization-defined metadata for future SDLC extensions.
    /// </summary>
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

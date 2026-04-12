using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using CleanSquad.Workflow.Definitions;

namespace CleanSquad.Workflow.UnitTests.Definitions;

/// <summary>
///     Unit tests for <see cref="WorkflowDefinitionLoader" />.
/// </summary>
public sealed class WorkflowDefinitionLoaderTests
{
    /// <summary>
    ///     Verifies the loader normalizes relative asset paths.
    /// </summary>
    [Fact]
    public void LoadNormalizesAssetPathsToAbsolutePaths()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "builder.md"), "builder", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "reviewer.md"), "reviewer", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "rebuilder.md"), "rebuilder", System.Text.Encoding.UTF8);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Loader Test\",",
                "  \"planner\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }] },",
                "  \"builder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/builder.md\" }] },",
                "  \"reviewer\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/reviewer.md\" }] },",
                "  \"rebuilder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/rebuilder.md\" }] },",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            WorkflowDefinition definition = WorkflowDefinitionLoader.LoadFromFile(definitionPath);

            Assert.True(Path.IsPathRooted(definition.Planner!.Assets[0].Path));
            Assert.True(File.Exists(definition.Planner.Assets[0].Path));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the loader rejects invalid policy combinations.
    /// </summary>
    [Fact]
    public void LoadRejectsAgentDecisionModeWithoutDecisionStage()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "builder.md"), "builder", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "reviewer.md"), "reviewer", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "rebuilder.md"), "rebuilder", System.Text.Encoding.UTF8);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Loader Test\",",
                "  \"planner\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }] },",
                "  \"builder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/builder.md\" }] },",
                "  \"reviewer\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/reviewer.md\" }] },",
                "  \"rebuilder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/rebuilder.md\" }] },",
                "  \"policy\": { \"decisionMode\": \"Agent\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => WorkflowDefinitionLoader.LoadFromFile(definitionPath));
            Assert.Contains("decision stage", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the loader normalizes workflow package metadata for future package-style distribution.
    /// </summary>
    [Fact]
    public void LoadNormalizesWorkflowPackageMetadata()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "builder.md"), "builder", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "reviewer.md"), "reviewer", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "rebuilder.md"), "rebuilder", System.Text.Encoding.UTF8);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"version\": \"2026.04\",",
                "  \"name\": \"Acme Quality Gate\",",
                "  \"package\": {",
                "    \"id\": \"acme.quality-gate\",",
                "    \"publisher\": \"Acme Platform Engineering\",",
                "    \"supportEmail\": \"quality@example.com\",",
                "    \"supportUrl\": \"https://support.example.com/quality-gate\",",
                "    \"repositoryUrl\": \"https://github.com/acme/quality-gate\",",
                "    \"documentationUrl\": \"https://docs.example.com/quality-gate\",",
                "    \"metadata\": { \"tooling.policyPack\": \"enterprise-default\" }",
                "  },",
                "  \"planner\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }] },",
                "  \"builder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/builder.md\" }] },",
                "  \"reviewer\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/reviewer.md\" }] },",
                "  \"rebuilder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/rebuilder.md\" }] },",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            WorkflowDefinition definition = WorkflowDefinitionLoader.LoadFromFile(definitionPath);

            Assert.Equal("Acme Quality Gate", definition.Package.DisplayName);
            Assert.Equal("2026.04", definition.Package.Version);
            Assert.Equal("Acme Platform Engineering", definition.Package.Publisher);
            Assert.Equal("https://github.com/acme/quality-gate", definition.Package.RepositoryUrl?.AbsoluteUri);
            Assert.Equal("enterprise-default", definition.Package.Metadata["tooling.policyPack"]);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the loader normalizes configured stage and node model identifiers.
    /// </summary>
    [Fact]
    public void LoadNormalizesConfiguredModels()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "shared.md"), "shared", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "research.md"), "research", System.Text.Encoding.UTF8);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Model Loader Test\",",
                "  \"defaultEntryPoint\": \"default\",",
                "  \"entryPoints\": [{ \"id\": \"default\", \"nodeId\": \"research\" }],",
                "  \"sharedAssets\": [{ \"kind\": \"instruction\", \"path\": \"assets/shared.md\" }],",
                "  \"nodes\": [",
                "    {",
                "      \"id\": \"research\",",
                "      \"kind\": \"Stage\",",
                "      \"role\": \"Research\",",
                "      \"models\": [\" model-alpha \", \"model-alpha\", \"model-beta\"],",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/research.md\" }],",
                "      \"next\": \"approved\"",
                "    },",
                "    { \"id\": \"approved\", \"kind\": \"Exit\", \"exitStatus\": \"Approved\" }",
                "  ],",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            WorkflowDefinition definition = WorkflowDefinitionLoader.LoadFromFile(definitionPath);

            Assert.Equal(["model-alpha", "model-beta"], definition.Nodes[0].Models);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the loader rejects invalid workflow package contact metadata.
    /// </summary>
    [Fact]
    public void LoadRejectsInvalidWorkflowPackageSupportEmail()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "builder.md"), "builder", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "reviewer.md"), "reviewer", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "rebuilder.md"), "rebuilder", System.Text.Encoding.UTF8);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Loader Test\",",
                "  \"package\": { \"supportEmail\": \"not-an-email\" },",
                "  \"planner\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }] },",
                "  \"builder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/builder.md\" }] },",
                "  \"reviewer\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/reviewer.md\" }] },",
                "  \"rebuilder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/rebuilder.md\" }] },",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => WorkflowDefinitionLoader.LoadFromFile(definitionPath));

            Assert.Contains("package.supportEmail", exception.Message, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies legacy stage definitions preserve agent, outputs, and custom message when graph nodes are synthesized.
    /// </summary>
    [Fact]
    public void LoadSynthesizesLegacyStageMetadataIntoGraphNodes()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "builder.md"), "builder", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "reviewer.md"), "reviewer", System.Text.Encoding.UTF8);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "rebuilder.md"), "rebuilder", System.Text.Encoding.UTF8);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Legacy Stage Metadata Test\",",
                "  \"planner\": {",
                "    \"agent\": \"planner-agent\",",
                "    \"models\": [\"model-plan\"],",
                "    \"outputs\": [\"planMarkdown\", \"riskSummary\"],",
                "    \"customMessage\": \"Create a concise plan.\",",
                "    \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }]",
                "  },",
                "  \"builder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/builder.md\" }] },",
                "  \"reviewer\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/reviewer.md\" }] },",
                "  \"rebuilder\": { \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/rebuilder.md\" }] },",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            WorkflowDefinition definition = WorkflowDefinitionLoader.LoadFromFile(definitionPath);
            WorkflowNodeDefinition planner = Assert.Single(definition.Nodes, node => string.Equals(node.Id, "planner", StringComparison.OrdinalIgnoreCase));

            Assert.Equal("planner-agent", planner.Agent);
            Assert.Equal(["planMarkdown", "riskSummary"], planner.Outputs);
            Assert.Equal("Create a concise plan.", planner.CustomMessage);
            Assert.Equal(["model-plan"], planner.Models);
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies validation rejects an asset whose configured SHA-256 hash does not match the file content.
    /// </summary>
    [Fact]
    public void ValidateFileRejectsAssetHashMismatch()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            string assetPath = Path.Combine(assetsDirectoryPath, "planner.md");
            File.WriteAllText(assetPath, "planner", System.Text.Encoding.UTF8);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Hash Test\",",
                "  \"defaultEntryPoint\": \"default\",",
                "  \"entryPoints\": [{ \"id\": \"default\", \"nodeId\": \"planner\" }],",
                "  \"nodes\": [",
                "    {",
                "      \"id\": \"planner\",",
                "      \"kind\": \"Stage\",",
                "      \"role\": \"Planner\",",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\", \"hash\": \"sha256:0000000000000000000000000000000000000000000000000000000000000000\" }],",
                "      \"next\": \"approved\"",
                "    },",
                "    { \"id\": \"approved\", \"kind\": \"Exit\", \"exitStatus\": \"Approved\" }",
                "  ],",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            WorkflowDefinitionValidationResult result = WorkflowDefinitionLoader.ValidateFile(definitionPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("hash does not match", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies validation rejects a workflow cycle that cannot reach an exit node.
    /// </summary>
    [Fact]
    public void ValidateFileRejectsCircularReferenceWithoutExitPath()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            string plannerAssetPath = Path.Combine(assetsDirectoryPath, "planner.md");
            string reviewerAssetPath = Path.Combine(assetsDirectoryPath, "reviewer.md");
            File.WriteAllText(plannerAssetPath, "planner", System.Text.Encoding.UTF8);
            File.WriteAllText(reviewerAssetPath, "reviewer", System.Text.Encoding.UTF8);
            string plannerHash = ComputeSha256(plannerAssetPath);
            string reviewerHash = ComputeSha256(reviewerAssetPath);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Cycle Test\",",
                "  \"defaultEntryPoint\": \"default\",",
                "  \"entryPoints\": [{ \"id\": \"default\", \"nodeId\": \"planner\" }],",
                "  \"nodes\": [",
                "    {",
                "      \"id\": \"planner\",",
                "      \"kind\": \"Stage\",",
                "      \"role\": \"Planner\",",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\", \"hash\": \"sha256:" + plannerHash + "\" }],",
                "      \"next\": \"reviewer\"",
                "    },",
                "    {",
                "      \"id\": \"reviewer\",",
                "      \"kind\": \"Stage\",",
                "      \"role\": \"Reviewer\",",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/reviewer.md\", \"hash\": \"sha256:" + reviewerHash + "\" }],",
                "      \"next\": \"planner\"",
                "    },",
                "    { \"id\": \"approved\", \"kind\": \"Exit\", \"exitStatus\": \"Approved\" }",
                "  ],",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            WorkflowDefinitionValidationResult result = WorkflowDefinitionLoader.ValidateFile(definitionPath);

            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, error => error.Contains("circular reference", StringComparison.OrdinalIgnoreCase));
            Assert.Contains(result.Errors, error => error.Contains("cannot reach any exit node", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies validation reports warnings for nodes that are not reachable from any entry point.
    /// </summary>
    [Fact]
    public void ValidateFileWarnsAboutUnreachableNodes()
    {
        string tempDirectoryPath = CreateTempDirectory();

        try
        {
            string assetsDirectoryPath = Path.Combine(tempDirectoryPath, "assets");
            Directory.CreateDirectory(assetsDirectoryPath);
            File.WriteAllText(Path.Combine(assetsDirectoryPath, "planner.md"), "planner", System.Text.Encoding.UTF8);
            string definitionPath = Path.Combine(tempDirectoryPath, "workflow.json");
            string definitionJson = string.Join(
                Environment.NewLine,
                "{",
                "  \"name\": \"Reachability Test\",",
                "  \"defaultEntryPoint\": \"default\",",
                "  \"entryPoints\": [{ \"id\": \"default\", \"nodeId\": \"planner\" }],",
                "  \"nodes\": [",
                "    {",
                "      \"id\": \"planner\",",
                "      \"kind\": \"Stage\",",
                "      \"role\": \"Planner\",",
                "      \"assets\": [{ \"kind\": \"agent\", \"path\": \"assets/planner.md\" }],",
                "      \"next\": \"approved\"",
                "    },",
                "    { \"id\": \"approved\", \"kind\": \"Exit\", \"exitStatus\": \"Approved\" },",
                "    { \"id\": \"unused\", \"kind\": \"Exit\", \"exitStatus\": \"Stopped\" }",
                "  ],",
                "  \"policy\": { \"decisionMode\": \"Rules\", \"maxReviewCycles\": 2, \"maxRebuilds\": 1 }",
                "}");
            File.WriteAllText(definitionPath, definitionJson, System.Text.Encoding.UTF8);

            WorkflowDefinitionValidationResult result = WorkflowDefinitionLoader.ValidateFile(definitionPath);

            Assert.True(result.IsValid);
            Assert.Contains(result.Warnings, warning => warning.Contains("not reachable", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            Directory.Delete(tempDirectoryPath, true);
        }
    }

    /// <summary>
    ///     Verifies the default workflow-definition package remains structurally valid.
    /// </summary>
    [Fact]
    public void ValidateDefaultWorkflowDefinitionPackage()
    {
        string definitionPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "..",
                "workflow-definitions",
                "default",
                "workflow.json"));

        WorkflowDefinitionValidationResult result = WorkflowDefinitionLoader.ValidateFile(definitionPath);

        Assert.True(
            result.IsValid,
            string.Join(
                Environment.NewLine,
                [
                    .. result.Errors,
                    .. result.Warnings,
                ]));
    }

    private static string CreateTempDirectory()
    {
        string tempDirectoryPath = Path.Combine(Path.GetTempPath(), $"clean-squad-definition-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDirectoryPath);
        return tempDirectoryPath;
    }

    private static string ComputeSha256(string filePath)
    {
        using SHA256 sha256 = SHA256.Create();
        using FileStream stream = File.OpenRead(filePath);
        return Convert.ToHexString(sha256.ComputeHash(stream));
    }
}

using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;

using SharpClaw.Contracts.Modules;

namespace SharpClaw.Modules.Metrics;

/// <summary>First-party SharpClaw metrics module identity and lifecycle implementation.</summary>
public sealed class MetricsModule : ISharpClawCoreModule
{
    public string Id => "sharpclaw_metrics";
    public string DisplayName => "Metrics";
    public string ToolPrefix => "metric";

    public void ConfigureServices(IServiceCollection services)
    {
    }

    public IReadOnlyList<ModuleToolDefinition> GetToolDefinitions() => [];

    public Task<string> ExecuteToolAsync(
        string toolName,
        JsonElement parameters,
        AgentJobContext job,
        IServiceProvider scopedServices,
        CancellationToken ct) =>
        throw new InvalidOperationException(
            $"Metrics module has no tools. Unknown: '{toolName}'.");
}

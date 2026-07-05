using System.Text.Json;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

using SharpClaw.Contracts.Modules;
using SharpClaw.Contracts.Tasks;
using SharpClaw.Modules.Metrics;

namespace SharpClaw.Metrics.Tests;

public sealed class MetricsModuleTests
{
    [Test]
    public void ModuleIdentityMatchesPublicManifest()
    {
        var module = new MetricsModule();
        using var manifest = JsonDocument.Parse(File.ReadAllText(
            Path.Combine(TestContext.CurrentContext.TestDirectory, "module.json")));
        var root = manifest.RootElement;

        Assert.That(module.Id, Is.EqualTo("sharpclaw_metrics"));
        Assert.That(module.DisplayName, Is.EqualTo("Metrics"));
        Assert.That(module.ToolPrefix, Is.EqualTo("metric"));
        Assert.That(module.GetToolDefinitions(), Is.Empty);

        Assert.That(root.GetProperty("id").GetString(), Is.EqualTo(module.Id));
        Assert.That(root.GetProperty("version").GetString(), Is.EqualTo("0.1.1-beta"));
        Assert.That(root.GetProperty("toolPrefix").GetString(), Is.EqualTo(module.ToolPrefix));
        Assert.That(root.GetProperty("entryAssembly").GetString(), Is.EqualTo("SharpClaw.Modules.Metrics.dll"));
        Assert.That(root.GetProperty("moduleType").GetString(), Is.EqualTo(typeof(MetricsModule).FullName));
        Assert.That(root.GetProperty("hostMode").GetString(), Is.EqualTo("sidecar"));
        Assert.That(root.GetProperty("enabled").GetBoolean(), Is.True);
        Assert.That(root.GetProperty("defaultEnabled").GetBoolean(), Is.True);
    }

    [Test]
    public void ConfigureServicesRegistersMetricProvidersAndTriggerSource()
    {
        var services = new ServiceCollection();
        var module = new MetricsModule();

        module.ConfigureServices(services);

        Assert.That(
            services.Where(service => service.ServiceType == typeof(ITaskMetricProvider))
                .Select(service => service.ImplementationType),
            Is.EquivalentTo(new[]
            {
                typeof(PendingJobCountMetricProvider),
                typeof(PendingTaskCountMetricProvider),
                typeof(SchedulerPendingJobCountMetricProvider),
            }));

        Assert.That(
            services.Single(service => service.ServiceType == typeof(ITaskTriggerSource)).ImplementationType,
            Is.EqualTo(typeof(MetricTriggerSource)));
    }

    [Test]
    public async Task BuiltInProvidersForwardToHostQueueMetrics()
    {
        var host = new RecordingHostQueueMetrics();

        Assert.That(await new PendingJobCountMetricProvider(host).GetValueAsync(CancellationToken.None),
            Is.EqualTo(11));
        Assert.That(await new PendingTaskCountMetricProvider(host).GetValueAsync(CancellationToken.None),
            Is.EqualTo(22));
        Assert.That(await new SchedulerPendingJobCountMetricProvider(host).GetValueAsync(CancellationToken.None),
            Is.EqualTo(33));

        Assert.That(host.Calls, Is.EqualTo(new[]
        {
            nameof(IHostQueueMetrics.GetPendingJobCountAsync),
            nameof(IHostQueueMetrics.GetPendingTaskCountAsync),
            nameof(IHostQueueMetrics.GetSchedulerPendingJobCountAsync),
        }));
    }

    [Test]
    public void ParserExtensionExposesMetricThresholdAttributeHandler()
    {
        var extension = MetricsParserExtension.Instance;

        Assert.That(extension.OperationKeyMappings, Is.Empty);
        Assert.That(extension.EventTriggerMappings, Is.Empty);
        Assert.That(extension.SingleArgExpressionMethods, Is.Empty);
        Assert.That(extension.TriggerAttributeHandlers.Keys, Is.EqualTo(new[] { "OnMetricThreshold" }));
    }

    [Test]
    public void MetricTriggerSourcePreservesTriggerIdentityAndBindingValue()
    {
        var source = new MetricTriggerSource([], NullLogger<MetricTriggerSource>.Instance);
        var definition = new TaskTriggerDefinition
        {
            TriggerKey = MetricTriggerKeys.MetricThreshold,
            Parameters = new Dictionary<string, string?>
            {
                [MetricTriggerKeys.Source] = "Queue.PendingJobCount",
                [MetricTriggerKeys.Threshold] = "5",
            },
        };

        Assert.That(source.TriggerKey, Is.EqualTo("MetricThreshold"));
        Assert.That(source.GetBindingValue(definition), Is.EqualTo("Queue.PendingJobCount"));
    }

    [Test]
    public void ExecuteToolRejectsEveryToolName()
    {
        var module = new MetricsModule();
        var context = new AgentJobContext(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), null, "metric_unknown");

        var ex = Assert.ThrowsAsync<InvalidOperationException>(() =>
            module.ExecuteToolAsync("unknown", default, context, new ServiceCollection().BuildServiceProvider(),
                CancellationToken.None));

        Assert.That(ex!.Message, Does.Contain("Metrics module has no job-pipeline tools"));
    }

    private sealed class RecordingHostQueueMetrics : IHostQueueMetrics
    {
        public List<string> Calls { get; } = [];

        public Task<double> GetPendingJobCountAsync(CancellationToken ct)
        {
            Calls.Add(nameof(GetPendingJobCountAsync));
            return Task.FromResult(11d);
        }

        public Task<double> GetPendingTaskCountAsync(CancellationToken ct)
        {
            Calls.Add(nameof(GetPendingTaskCountAsync));
            return Task.FromResult(22d);
        }

        public Task<double> GetSchedulerPendingJobCountAsync(CancellationToken ct)
        {
            Calls.Add(nameof(GetSchedulerPendingJobCountAsync));
            return Task.FromResult(33d);
        }
    }
}

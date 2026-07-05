# SharpClaw Metrics

SharpClaw Metrics is the first-party SharpClaw module that owns the
`sharpclaw_metrics` runtime module identity and the `metric` tool prefix. It
registers the built-in queue and scheduler metric providers and the
`MetricThreshold` task trigger source. A task script that declares
`[OnMetricThreshold("Queue.PendingJobCount", Threshold = 5, Direction =
ThresholdDirection.Above)]` is parsed into a module-owned trigger definition
whose binding value is `Queue.PendingJobCount`.

The package is intended to be loaded by a SharpClaw host as a module payload.
The NuGet package carries the runtime files under `sharpclaw\`, including
`module.json`, `SharpClaw.Modules.Metrics.dll`, the dependency file, and the
dependency assemblies needed by the sidecar module. The manifest keeps the
public module type as `SharpClaw.Modules.Metrics.MetricsModule`, defaults the
module to enabled, and declares sidecar .NET runtime loading.

At runtime the module registers three `ITaskMetricProvider` implementations
against the host-provided `IHostQueueMetrics` contract. `Queue.PendingJobCount`
reads queued agent jobs, `Queue.PendingTaskCount` reads queued task instances,
and `Scheduler.PendingJobCount` reads scheduled jobs whose next run is due but
has not fired yet. `MetricTriggerSource` polls registered metric providers and
fires a task only when the configured threshold transitions from not-crossed to
crossed, which avoids repeated launches while the metric remains on the same
side of the threshold.

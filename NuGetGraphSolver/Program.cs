using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NuGetGraphSolver;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using OpenTelemetry.Metrics;

var builder = Host.CreateApplicationBuilder(args);

var resourceBuilder = ResourceBuilder.CreateDefault().AddService(
    serviceName: "NuGetGraphSolver",
    serviceVersion: typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0");

builder.Logging.ClearProviders();
builder.Logging.AddOpenTelemetry(otlp =>
{
    otlp.IncludeFormattedMessage = true;
    otlp.ParseStateValues = true;
    otlp.IncludeScopes = true;
    otlp.SetResourceBuilder(resourceBuilder);
    otlp.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(rb => rb.AddService("NuGetGraphSolver"))
    .WithTracing(tpb => tpb
        .AddOtlpExporter())
    .WithMetrics(mpb => mpb
        .AddRuntimeInstrumentation()
        .AddMeter("NuGetGraphSolver")
        .AddOtlpExporter());

ServicesRegistration.AddCoreServices(builder.Services);
var root = RootCommandFactory.Create(builder.Services);
var parseResult = root.Parse(args);
return parseResult.Invoke();

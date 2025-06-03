using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace TelemetryAspireDashboardQuickstart
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Endpoint to the Aspire Dashboard
            var endpoint = "http://localhost:4317";

            var resourceBuilder = ResourceBuilder
                .CreateDefault()
                .AddService("TelemetryAspire");

            // Enable model diagnostics with sensitive data.
            AppContext.SetSwitch("Microsoft.SemanticKernel.Experimental.GenAI.EnableOTelDiagnosticsSensitive", true);

            using var traceProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddSource("Microsoft.SemanticKernel*")
                .AddOtlpExporter(options => options.Endpoint = new Uri(endpoint))
                .Build();

            using var meterProvider = Sdk.CreateMeterProviderBuilder()
                .SetResourceBuilder(resourceBuilder)
                .AddMeter("Microsoft.SemanticKernel*")
                .AddOtlpExporter(options => options.Endpoint = new Uri(endpoint))
                .Build();

            using var loggerFactory = LoggerFactory.Create(builder =>
            {
                // Add OpenTelemetry as a logging provider
                builder.AddOpenTelemetry(options =>
                {
                    options.SetResourceBuilder(resourceBuilder);
                    options.AddOtlpExporter(options => options.Endpoint = new Uri(endpoint));
                    // Format log messages. This is default to false.
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                });
                builder.SetMinimumLevel(LogLevel.Information);
            });

            IKernelBuilder builder = Kernel.CreateBuilder();

            builder.Services.AddSingleton(loggerFactory);
            // Get the Azure API key from environment variables
            string? apiKey = string.Empty;
            apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");

            if (string.IsNullOrEmpty(apiKey))
            {
                Console.WriteLine("Please set the AZURE_API_KEY environment variable.");
                return;
            }

            var az_endpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
            if (string.IsNullOrEmpty(az_endpoint))
            {
                Console.WriteLine("Please set the AZURE_ENDPOINT environment variable.");
                return;
            }

            builder.AddAzureOpenAIChatCompletion(
            "GPT-4o",
            az_endpoint,
            apiKey,
            "GPT-4o");

            Kernel kernel = builder.Build();

            var answer = await kernel.InvokePromptAsync(
                "What is managed services in one sentence?"
            );

            Console.WriteLine(answer);
        }
    }
}
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Threading.Tasks;

namespace SK_AgentsNRD
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("=== NRD Issue Resolution System ===\n");

            // Setup logging
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                })
                .BuildServiceProvider();

            var logger = serviceProvider.GetRequiredService<ILogger<NRDAgentsOrchestrator>>();

            try
            {
                // Setup configuration to read Azure OpenAI settings
                var configuration = new ConfigurationBuilder()
                    .AddUserSecrets<Program>()
                    .Build();

                // Get Azure OpenAI settings
                var endpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
                if (string.IsNullOrEmpty(endpoint))
                {
                    endpoint = configuration["AZURE_ENDPOINT"];
                }
                
                var apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = configuration["AZURE_API_KEY"];
                }
                
                var deploymentName = Environment.GetEnvironmentVariable("DEPLOYMENT_NAME");
                if (string.IsNullOrEmpty(deploymentName))
                {
                    deploymentName = configuration["DEPLOYMENT_NAME"] ?? "gpt-4";
                }

                // Determine if we should use simulation mode
                bool simulationMode = false;
                
                // Check if credentials are missing
                if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("Azure OpenAI credentials not found. Running in simulation mode.\n");
                    simulationMode = true;
                }
                else
                {
                    // Ask user if they want to use simulation mode
                    Console.WriteLine("Select execution mode:");
                    Console.WriteLine("1. Real mode (uses Azure OpenAI API - costs apply)");
                    Console.WriteLine("2. Simulation mode (no API calls - faster, no costs)\n");
                    
                    Console.Write("Enter your choice (1 or 2): ");
                    string choice = Console.ReadLine()?.Trim() ?? "2";
                    
                    simulationMode = choice == "2";
                    Console.WriteLine();
                }

                // Create the orchestrator
                var orchestrator = new NRDAgentsOrchestrator(endpoint ?? "", apiKey ?? "", deploymentName, logger, simulationMode);

                // Menu loop
                bool exit = false;
                while (!exit)
                {
                    Console.WriteLine("\nNRD Issue Resolution Menu:");
                    Console.WriteLine("1. Investigate specific device");
                    Console.WriteLine("2. Run test cases");
                    Console.WriteLine("3. Exit");
                    Console.Write("\nSelect an option: ");

                    var option = Console.ReadLine()?.Trim();

                    switch (option)
                    {
                        case "1":
                            await InvestigateDevice(orchestrator);
                            break;

                        case "2":
                            await RunTestCases(orchestrator);
                            break;

                        case "3":
                            exit = true;
                            break;

                        default:
                            Console.WriteLine("Invalid option. Please try again.");
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                logger.LogError(ex, "An error occurred in the NRD Issue Resolution System");
            }

            Console.WriteLine("\nNRD Issue Resolution System terminated.");
        }

        static async Task InvestigateDevice(NRDAgentsOrchestrator orchestrator)
        {
            Console.Write("Enter device ID to investigate: ");
            string deviceId = Console.ReadLine()?.Trim() ?? "";
            
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                Console.WriteLine("Invalid device ID.");
                return;
            }
            
            Console.WriteLine($"\nInvestigating device: {deviceId}\n");
            bool resolved = await orchestrator.HandleNRDIssue(deviceId);
            
            if (resolved)
            {
                Console.WriteLine("\nIssue successfully resolved!");
            }
            else
            {
                Console.WriteLine("\nIssue investigation completed but may require further attention.");
            }
        }

        static async Task RunTestCases(NRDAgentsOrchestrator orchestrator)
        {
            Console.WriteLine("=== Running NRD Resolution Tests ===\n");

            // Test cases with different devices
            string[] testDevices = { "DEV001", "DEV002", "DEV003", "DEV004" };

            foreach (var deviceId in testDevices)
            {
                Console.WriteLine($"Testing device: {deviceId}");
                await orchestrator.HandleNRDIssue(deviceId);
                Console.WriteLine();
            }
        }
    }
}

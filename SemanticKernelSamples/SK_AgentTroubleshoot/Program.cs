using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SK_AgentTroubleshoot;

namespace PrinterTroubleshootingSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection()
           .AddLogging(builder =>
           {
               builder.AddConsole();
               builder.SetMinimumLevel(LogLevel.Information);
           });

            var serviceProvider = services.BuildServiceProvider();
            var logger = serviceProvider.GetRequiredService<ILogger<PrinterTroubleShooterAgents>>();

            var apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");
            var endpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");

            // Initialize the troubleshooter with your Azure OpenAI credentials
            var troubleshooter = new PrinterTroubleShooterAgents(
                endpoint,
                apiKey,
                "GPT-4o",
                logger
            );

            Console.WriteLine("Printer Troubleshooting Assistant");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("1. Run diagnostic tests");
            Console.WriteLine("2. Enter custom issue");
            Console.WriteLine("3. Reset chat");
            Console.WriteLine("4. Exit");

            while (true)
            {
                Console.Write("\nSelect an option (1-4): ");
                var choice = Console.ReadLine();

                switch (choice)
                {
                    case "1":
                        await troubleshooter.RunDiagnosticTests();
                        break;

                    case "2":
                        Console.Write("\nDescribe your printer issue: ");
                        var issue = Console.ReadLine();
                        if (!string.IsNullOrEmpty(issue))
                        {
                            await troubleshooter.HandlePrinterIssue(issue);
                        }
                        break;

                    case "3":
                        await troubleshooter.ResetChat();
                        break;

                    case "4":
                        return;

                    default:
                        Console.WriteLine("Invalid option. Please try again.");
                        break;
                }
            }
        }
    }
}
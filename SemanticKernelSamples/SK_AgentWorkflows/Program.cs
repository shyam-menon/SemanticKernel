using Microsoft.SemanticKernel;
using System.Text.Json;
using System.Reflection;
using SK_AgentWorkflows.Examples;

class Program
{
    static async Task Main(string[] args)
    {
        while (true)
        {
            Console.Clear();
            Console.WriteLine("Semantic Kernel Agent Workflow Examples");
            Console.WriteLine("=======================================");
            Console.WriteLine("1. Prompt Chaining");
            Console.WriteLine("2. Routing");
            Console.WriteLine("3. Tool Use");
            Console.WriteLine("4. Parallelization");
            Console.WriteLine("5. Orchestrator-Workers");
            Console.WriteLine("6. Evaluator-Optimizer");
            Console.WriteLine("7. Agents");
            Console.WriteLine("0. Exit");
            Console.WriteLine("\nSelect an example to run:");

            var choice = Console.ReadLine();

            try
            {
                switch (choice)
                {
                    case "1":
                        await PromptChaining.RunAsync();
                        break;
                    case "2":
                        await Routing.RunAsync();
                        break;
                    case "3":
                        await ToolUse.RunAsync();
                        break;
                    case "4":
                        await Parallelization.RunAsync();
                        break;
                    case "5":
                        await OrchestratorWorkers.RunAsync();
                        break;
                    case "6":
                        await EvaluatorOptimizer.RunAsync();
                        break;
                    case "7":
                        await Agents.RunAsync();
                        break;
                    case "0":
                        return;
                    default:
                        Console.WriteLine("Invalid choice. Press any key to try again.");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }
    }
}

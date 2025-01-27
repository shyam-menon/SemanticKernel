using Microsoft.SemanticKernel;
using System.Reflection;
using System.Text.Json;
using System.ComponentModel;
using SK_AgentWorkflows.Tools;

namespace SK_AgentWorkflows.Examples;

public class Agents
{
    private static Kernel Kernel = null!;
    private static Dictionary<string, KernelFunction> Workers = new();
    private static ProjectTools ProjectTools = new();

    public static async Task RunAsync()
    {
        // Get Azure OpenAI credentials from environment variables
        var deploymentName = "GPT-4o";
        var endpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");

        if (string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Please set the following environment variables:");
            Console.WriteLine("- AZURE_ENDPOINT");
            Console.WriteLine("- AZURE_API_KEY");
            return;
        }

        // Initialize the kernel with Azure OpenAI
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: deploymentName,
            endpoint: endpoint,
            apiKey: apiKey
        );
        
        Kernel = kernelBuilder.Build();

        // Import native functions
        var projectPlugin = KernelPluginFactory.CreateFromObject(ProjectTools, "ProjectTools");
        Kernel.Plugins.Add(projectPlugin);

        // Get the directory where the executable is located
        var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var promptTemplateDirectory = Path.Combine(executingPath!, "Prompts");

        // Initialize the agents
        Workers["planner"] = Kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptTemplateDirectory, "AgentPlanner.txt"))
        );
        Workers["executor"] = Kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptTemplateDirectory, "AgentExecutor.txt"))
        );
        Workers["monitor"] = Kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptTemplateDirectory, "AgentMonitor.txt"))
        );

        try
        {
            // Reset project state
            await Kernel.InvokeAsync(projectPlugin["ResetProject"]);

            Console.WriteLine("Autonomous Agents Demo");
            Console.WriteLine("=====================");

            // Sample project requirements
            var projectRequirements = @"Create a new mobile app development project with the following requirements:
- User authentication system
- Product catalog with search
- Shopping cart functionality
- Payment integration
Timeline: 2 months
Team size: 5 developers
Budget: $100,000";

            // Get available tools
            var tools = typeof(ProjectTools).GetMethods()
                .Where(m => m.GetCustomAttribute<KernelFunctionAttribute>() != null)
                .Select(m => new
                {
                    name = m.Name,
                    description = m.GetCustomAttribute<DescriptionAttribute>()?.Description ?? ""
                })
                .ToList();

            var toolsJson = JsonSerializer.Serialize(tools);

            Console.WriteLine($"\nProject Requirements:\n{projectRequirements}\n");

            // Step 1: Planning
            Console.WriteLine("1. Planning Phase");
            Console.WriteLine("-----------------");
            var planResult = await Kernel.InvokeAsync(Workers["planner"], new()
            {
                ["input"] = projectRequirements,
                ["state"] = "{}",
                ["tools"] = toolsJson
            });
            var plan = planResult.GetValue<string>()?.Trim() ?? "{}";

            Console.WriteLine("\nProject Plan:");
            Console.WriteLine(JsonSerializer.Serialize(
                JsonDocument.Parse(plan).RootElement,
                new JsonSerializerOptions { WriteIndented = true }
            ));

            // Step 2: Execution
            Console.WriteLine("\n2. Execution Phase");
            Console.WriteLine("------------------");
            var planObj = JsonDocument.Parse(plan).RootElement;
            foreach (var action in planObj.GetProperty("action_plan").EnumerateArray())
            {
                var step = action.GetProperty("step").GetInt32();
                var task = action.GetProperty("action").GetString();
                var tool = action.GetProperty("tool_needed").GetString();

                Console.WriteLine($"\nExecuting Step {step}: {task}");
                
                var executionResult = await Kernel.InvokeAsync(Workers["executor"], new()
                {
                    ["task"] = task,
                    ["tools"] = toolsJson,
                    ["context"] = plan
                });
                var execution = executionResult.GetValue<string>()?.Trim() ?? "{}";

                Console.WriteLine("\nExecution Result:");
                Console.WriteLine(JsonSerializer.Serialize(
                    JsonDocument.Parse(execution).RootElement,
                    new JsonSerializerOptions { WriteIndented = true }
                ));
            }

            // Step 3: Monitoring
            Console.WriteLine("\n3. Monitoring Phase");
            Console.WriteLine("-------------------");
            var projectStatus = await Kernel.InvokeAsync(projectPlugin["GetProjectStatus"]);
            var recentUpdates = await Kernel.InvokeAsync(projectPlugin["GetRecentUpdates"]);

            var monitorResult = await Kernel.InvokeAsync(Workers["monitor"], new()
            {
                ["state"] = projectStatus.GetValue<string>(),
                ["actions"] = recentUpdates.GetValue<string>(),
                ["criteria"] = JsonSerializer.Serialize(planObj.GetProperty("success_criteria"))
            });
            var monitoring = monitorResult.GetValue<string>()?.Trim() ?? "{}";

            Console.WriteLine("\nProject Status:");
            Console.WriteLine(JsonSerializer.Serialize(
                JsonDocument.Parse(monitoring).RootElement,
                new JsonSerializerOptions { WriteIndented = true }
            ));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner error: {ex.InnerException.Message}");
            }
        }
    }
}

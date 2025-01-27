using Microsoft.SemanticKernel;
using System.Reflection;

namespace SK_AgentWorkflows.Examples;

public class Routing
{
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
        
        var kernel = kernelBuilder.Build();

        // Get the directory where the executable is located
        var executingPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        var promptTemplateDirectory = Path.Combine(executingPath!, "Prompts");

        // Ensure prompt directory exists
        if (!Directory.Exists(promptTemplateDirectory))
        {
            Console.WriteLine($"Error: Prompt directory not found at {promptTemplateDirectory}");
            return;
        }

        // Create prompt functions
        var routeFunction = kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptTemplateDirectory, "RouteQuery.txt"))
        );

        var departmentResponseFunction = kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptTemplateDirectory, "GenerateDepartmentResponse.txt"))
        );

        while (true)
        {
            //Use below prompts to see how the routing works

            //I want to upgrade my subscription plan
            //I'm having trouble logging into my account
            //What's the status of my recent job application?
            //"I need to update my billing information


            Console.WriteLine("\nEnter your query (or 'exit' to quit):");
            var query = Console.ReadLine();

            if (string.IsNullOrEmpty(query) || query.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            try
            {
                // Step 1: Route the query to appropriate department
                var routingResult = await kernel.InvokeAsync(routeFunction, new() { ["input"] = query });
                var department = routingResult.GetValue<string>();
                Console.WriteLine($"\nRouting to department: {department}");

                // Step 2: Generate department-specific response
                var responseResult = await kernel.InvokeAsync(departmentResponseFunction, new()
                {
                    ["input"] = query,
                    ["department"] = department
                });
                
                Console.WriteLine($"\nResponse from {department} department:");
                Console.WriteLine(responseResult.GetValue<string>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}

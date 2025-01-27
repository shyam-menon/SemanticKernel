using Microsoft.SemanticKernel;
using System.Reflection;

namespace SK_AgentWorkflows.Examples;

public class PromptChaining
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
        
        // Create prompt functions using the new API
        var sentimentFunction = kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptTemplateDirectory, "AnalyzeSentiment.txt"))
        );
        
        var responseFunction = kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptTemplateDirectory, "GenerateResponse.txt"))
        );
        
        var formatFunction = kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptTemplateDirectory, "FormatOutput.txt"))
        );

        // Example text to process
        var inputText = "I just got promoted at work and I'm really excited about the new opportunities!";
        
        try
        {
            // Step 1: Analyze sentiment
            var sentimentResult = await kernel.InvokeAsync(sentimentFunction, new() { ["input"] = inputText });
            var sentiment = sentimentResult.GetValue<string>();
            Console.WriteLine($"Detected sentiment: {sentiment}");

            // Step 2: Generate response based on sentiment
            var responseResult = await kernel.InvokeAsync(responseFunction, new() 
            { 
                ["input"] = inputText,
                ["sentiment"] = sentiment 
            });
            Console.WriteLine($"\nGenerated response: {responseResult.GetValue<string>()}");

            // Step 3: Format the final output
            var formatResult = await kernel.InvokeAsync(formatFunction, new()
            {
                ["input"] = responseResult.GetValue<string>(),
                ["sentiment"] = sentiment
            });
            Console.WriteLine($"\nFormatted output: {formatResult.GetValue<string>()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}

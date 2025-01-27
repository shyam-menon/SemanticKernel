using Microsoft.SemanticKernel;
using System.Reflection;
using System.Text.Json;

namespace SK_AgentWorkflows.Examples;

public class Parallelization
{
    private static readonly List<string> SampleReviews = new()
    {
        "The product arrived two days late, but the quality exceeded my expectations. The customer service team was very helpful in tracking my package.",
        "I'm disappointed with the durability. After just two weeks of use, the handle broke. Not worth the premium price.",
        "Easy to use and intuitive interface. The new features are game-changing for my workflow. Best software purchase this year!",
        "Shipping was quick but the packaging was damaged. The product itself seems fine but I'm concerned about long-term reliability.",
        "The customer support team took 3 days to respond to my urgent issue. By then, I had already found a workaround. Very frustrating experience."
    };

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

        try
        {
            // Create the analysis function
            var analyzeFunction = kernel.CreateFunctionFromPrompt(
                File.ReadAllText(Path.Combine(promptTemplateDirectory, "AnalyzeReview.txt"))
            );

            Console.WriteLine("Starting parallel analysis of customer reviews...\n");

            // Process reviews in parallel
            var analysisTaskList = SampleReviews.Select(async (review, index) =>
            {
                Console.WriteLine($"Processing review {index + 1}...");
                var result = await kernel.InvokeAsync(analyzeFunction, new() { ["input"] = review });
                return result.GetValue<string>();
            }).ToList();

            // Wait for all analyses to complete
            var analysisResults = await Task.WhenAll(analysisTaskList);

            Console.WriteLine("\nAll reviews processed. Generating summary...\n");

            // Create the summary function
            var summarizeFunction = kernel.CreateFunctionFromPrompt(
                File.ReadAllText(Path.Combine(promptTemplateDirectory, "SummarizeAnalysis.txt"))
            );

            // Generate summary of all analyses
            var summaryResult = await kernel.InvokeAsync(
                summarizeFunction,
                new() { ["input"] = JsonSerializer.Serialize(analysisResults) }
            );

            Console.WriteLine("Analysis Summary:");
            Console.WriteLine("================");
            Console.WriteLine(summaryResult.GetValue<string>());

            // Display individual review analyses
            Console.WriteLine("\nDetailed Review Analyses:");
            Console.WriteLine("=========================");
            for (int i = 0; i < analysisResults.Length; i++)
            {
                Console.WriteLine($"\nReview {i + 1}:");
                Console.WriteLine($"Original: {SampleReviews[i]}");
                Console.WriteLine($"Analysis: {analysisResults[i]}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"An error occurred: {ex.Message}");
        }
    }
}

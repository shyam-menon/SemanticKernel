using Microsoft.SemanticKernel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SK_AgentWorkflows.Examples;

public class EvaluatorOptimizer
{
    private static Kernel Kernel = null!;
    private const int MaxIterations = 3;
    private const double TargetScore = 85.0;

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
            // Initialize the functions
            var generator = Kernel.CreateFunctionFromPrompt(
                File.ReadAllText(Path.Combine(promptTemplateDirectory, "CopyGenerator.txt"))
            );
            var evaluator = Kernel.CreateFunctionFromPrompt(
                File.ReadAllText(Path.Combine(promptTemplateDirectory, "CopyEvaluator.txt"))
            );
            var optimizer = Kernel.CreateFunctionFromPrompt(
                File.ReadAllText(Path.Combine(promptTemplateDirectory, "CopyOptimizer.txt"))
            );

            // Sample product requirements
            var productRequirements = @"Create marketing copy for a new AI-powered smart home security system with:
- Advanced facial recognition
- Real-time mobile alerts
- Integration with smart home devices
- 24/7 professional monitoring
Target audience: Tech-savvy homeowners aged 30-50
Tone: Professional but approachable
Must emphasize: Easy setup, peace of mind, and innovative technology";

            var criteria = @"{
                ""minimum_scores"": {
                    ""clarity"": 80,
                    ""persuasiveness"": 85,
                    ""engagement"": 85,
                    ""brand_alignment"": 80,
                    ""call_to_action"": 90
                },
                ""required_elements"": [
                    ""value proposition"",
                    ""key features"",
                    ""emotional appeal"",
                    ""clear call to action""
                ],
                ""tone_requirements"": ""Professional but approachable, emphasizing reliability and innovation""
            }";

            Console.WriteLine("Marketing Copy Optimization Demo");
            Console.WriteLine("==============================");
            Console.WriteLine($"\nProduct Requirements:\n{productRequirements}\n");

            string currentCopy = "";
            string previousFeedback = "{}";
            var iteration = 1;
            double currentScore = 0;

            while (iteration <= MaxIterations && currentScore < TargetScore)
            {
                Console.WriteLine($"\nIteration {iteration}:");
                Console.WriteLine("----------------");

                // Generate copy
                Console.WriteLine("\nGenerating marketing copy...");
                var copyResult = await Kernel.InvokeAsync(generator, new()
                {
                    ["input"] = productRequirements,
                    ["feedback"] = previousFeedback
                });
                currentCopy = copyResult.GetValue<string>()?.Trim() ?? "{}";
                
                // Evaluate copy
                Console.WriteLine("\nEvaluating copy...");
                var evaluationResult = await Kernel.InvokeAsync(evaluator, new()
                {
                    ["input"] = currentCopy,
                    ["criteria"] = criteria
                });
                var evaluation = evaluationResult.GetValue<string>()?.Trim() ?? "{}";

                // Parse evaluation to get current score
                var evaluationJson = JsonDocument.Parse(evaluation).RootElement;
                currentScore = evaluationJson.GetProperty("total_score").GetDouble();

                Console.WriteLine($"\nCurrent copy (Score: {currentScore:F1}):");
                Console.WriteLine(JsonSerializer.Serialize(
                    JsonDocument.Parse(currentCopy).RootElement,
                    new JsonSerializerOptions { WriteIndented = true }
                ));

                Console.WriteLine("\nEvaluation:");
                Console.WriteLine(JsonSerializer.Serialize(
                    evaluationJson,
                    new JsonSerializerOptions { WriteIndented = true }
                ));

                if (currentScore >= TargetScore)
                {
                    Console.WriteLine($"\nTarget score of {TargetScore} achieved!");
                    break;
                }

                if (iteration < MaxIterations)
                {
                    // Get optimization recommendations
                    Console.WriteLine("\nGenerating optimization recommendations...");
                    var optimizationResult = await Kernel.InvokeAsync(optimizer, new()
                    {
                        ["copy"] = currentCopy,
                        ["evaluation"] = evaluation
                    });
                    var optimization = optimizationResult.GetValue<string>()?.Trim() ?? "{}";

                    Console.WriteLine("\nOptimization Plan:");
                    Console.WriteLine(JsonSerializer.Serialize(
                        JsonDocument.Parse(optimization).RootElement,
                        new JsonSerializerOptions { WriteIndented = true }
                    ));

                    previousFeedback = optimization;
                }

                iteration++;
            }

            if (currentScore < TargetScore)
            {
                Console.WriteLine($"\nMaximum iterations ({MaxIterations}) reached without achieving target score.");
            }

            Console.WriteLine("\nFinal Marketing Copy:");
            Console.WriteLine(JsonSerializer.Serialize(
                JsonDocument.Parse(currentCopy).RootElement,
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

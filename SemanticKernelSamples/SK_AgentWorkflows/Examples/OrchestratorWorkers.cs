using Microsoft.SemanticKernel;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SK_AgentWorkflows.Examples;

public class OrchestratorWorkers
{
    private static readonly Dictionary<string, KernelFunction> Workers = new();
    private static Kernel Kernel = null!;

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

        // Initialize all workers
        await InitializeWorkers(promptTemplateDirectory);

        var sampleRequest = @"Create a blog post about the benefits of artificial intelligence in healthcare, 
            focusing on recent developments and real-world applications. The content should be 
            informative yet accessible to a general audience.";

        try
        {
            Console.WriteLine("Content Creation Workflow Demo");
            Console.WriteLine("=============================");
            Console.WriteLine($"\nProcessing request: {sampleRequest}\n");

            // Step 1: Orchestrator creates task list
            Console.WriteLine("1. Orchestrator analyzing request...");
            var orchestratorFunction = Kernel.CreateFunctionFromPrompt(
                File.ReadAllText(Path.Combine(promptTemplateDirectory, "ContentOrchestrator.txt"))
            );

            var taskListResult = await Kernel.InvokeAsync(orchestratorFunction, new() { ["input"] = sampleRequest });
            var taskListJson = taskListResult.GetValue<string>()?.Trim();
            
            if (string.IsNullOrEmpty(taskListJson))
            {
                throw new Exception("Orchestrator returned empty response");
            }

            Console.WriteLine("\nTask list received:");
            Console.WriteLine(taskListJson);

            var jsonOptions = new JsonSerializerOptions 
            { 
                PropertyNameCaseInsensitive = true,
                NumberHandling = JsonNumberHandling.AllowReadingFromString
            };
            
            var taskList = JsonSerializer.Deserialize<WorkflowTasks>(taskListJson, jsonOptions);
            
            if (taskList == null)
            {
                throw new Exception("Failed to parse task list");
            }
            
            if (taskList.Tasks == null || !taskList.Tasks.Any())
            {
                throw new Exception("No tasks found in the task list");
            }

            Console.WriteLine($"\nTask list created with {taskList.Tasks.Count} tasks.");

            // Step 2: Process tasks in order of dependencies
            var taskResults = new Dictionary<string, string>();
            
            Console.WriteLine("\n2. Processing tasks...");
            foreach (var task in taskList.Tasks)
            {
                var worker = task.Worker;
                var taskDescription = task.Task;
                var dependencies = task.Dependencies ?? new List<string>();

                Console.WriteLine($"\nAssigning task to {worker}: {taskDescription}");

                // Wait for dependencies to complete
                foreach (var dep in dependencies)
                {
                    while (!taskResults.ContainsKey(dep))
                    {
                        await Task.Delay(100);
                    }
                }

                // Execute task based on worker type
                var result = worker switch
                {
                    "researcher" => await ExecuteResearchTask(taskDescription),
                    "writer" => await ExecuteWritingTask(taskDescription, taskResults),
                    "editor" => await ExecuteEditingTask(taskResults),
                    "fact_checker" => await ExecuteFactCheckingTask(taskResults),
                    _ => throw new ArgumentException($"Unknown worker type: {worker}")
                };

                taskResults[task.Id] = result;
                Console.WriteLine($"{worker} completed their task.");
            }

            // Display final results
            Console.WriteLine("\nFinal Content Creation Results:");
            Console.WriteLine("===============================");
            foreach (var (taskId, result) in taskResults)
            {
                var task = taskList.Tasks.First(t => t.Id == taskId);
                Console.WriteLine($"\nTask: {task.Task} (Worker: {task.Worker})");
                Console.WriteLine("Result:");
                Console.WriteLine(JsonSerializer.Deserialize<JsonElement>(result).ToString());
            }
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

    private static async Task InitializeWorkers(string promptDirectory)
    {
        // Initialize each worker with their respective prompts
        Workers["researcher"] = Kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptDirectory, "ResearchWorker.txt"))
        );

        Workers["writer"] = Kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptDirectory, "WriterWorker.txt"))
        );

        Workers["editor"] = Kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptDirectory, "EditorWorker.txt"))
        );

        Workers["fact_checker"] = Kernel.CreateFunctionFromPrompt(
            File.ReadAllText(Path.Combine(promptDirectory, "FactCheckerWorker.txt"))
        );
    }

    private static async Task<string> ExecuteResearchTask(string task)
    {
        Console.WriteLine($"\nExecuting research task: {task}");
        
        var result = await Kernel.InvokeAsync(Workers["researcher"], new() { ["input"] = task });
        var response = result.GetValue<string>()?.Trim();
        
        if (string.IsNullOrEmpty(response))
        {
            throw new Exception("Researcher returned empty response");
        }

        Console.WriteLine("Raw response received. Validating JSON...");
        
        try
        {
            // Try to parse and format the JSON to ensure it's valid
            using var doc = JsonDocument.Parse(response);
            var formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            Console.WriteLine("JSON validation successful");
            return formatted;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON validation failed: {ex.Message}");
            Console.WriteLine($"Raw response was: {response}");
            throw new Exception($"Researcher returned invalid JSON response: {ex.Message}");
        }
    }

    private static async Task<string> ExecuteWritingTask(string task, Dictionary<string, string> previousResults)
    {
        // Find research data from previous results
        var research = previousResults.Values
            .FirstOrDefault(v => v.Contains("\"key_findings\"")) ?? "{}";
        
        Console.WriteLine($"\nExecuting writing task with research data");
        
        var result = await Kernel.InvokeAsync(Workers["writer"], new() 
        { 
            ["task"] = task,
            ["research"] = research
        });
        
        var response = result.GetValue<string>()?.Trim();
        
        if (string.IsNullOrEmpty(response))
        {
            throw new Exception("Writer returned empty response");
        }

        Console.WriteLine("Raw response received. Validating JSON...");
        
        try
        {
            using var doc = JsonDocument.Parse(response);
            var formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            Console.WriteLine("JSON validation successful");
            return formatted;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON validation failed: {ex.Message}");
            Console.WriteLine($"Raw response was: {response}");
            throw new Exception($"Writer returned invalid JSON response: {ex.Message}");
        }
    }

    private static async Task<string> ExecuteEditingTask(Dictionary<string, string> previousResults)
    {
        // Find written content from previous results
        var content = previousResults.Values
            .FirstOrDefault(v => v.Contains("\"content\"")) ?? "{}";
        
        Console.WriteLine("\nExecuting editing task");
        
        var result = await Kernel.InvokeAsync(Workers["editor"], new() { ["input"] = content });
        var response = result.GetValue<string>()?.Trim();
        
        if (string.IsNullOrEmpty(response))
        {
            throw new Exception("Editor returned empty response");
        }

        Console.WriteLine("Raw response received. Validating JSON...");
        
        try
        {
            using var doc = JsonDocument.Parse(response);
            var formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            Console.WriteLine("JSON validation successful");
            return formatted;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON validation failed: {ex.Message}");
            Console.WriteLine($"Raw response was: {response}");
            throw new Exception($"Editor returned invalid JSON response: {ex.Message}");
        }
    }

    private static async Task<string> ExecuteFactCheckingTask(Dictionary<string, string> previousResults)
    {
        // Get both research and content for fact checking
        var research = previousResults.Values
            .FirstOrDefault(v => v.Contains("\"key_findings\"")) ?? "{}";
        var content = previousResults.Values
            .FirstOrDefault(v => v.Contains("\"content\"")) ?? "{}";
        
        Console.WriteLine("\nExecuting fact checking task");
        
        var result = await Kernel.InvokeAsync(Workers["fact_checker"], new() 
        { 
            ["input"] = content,
            ["research"] = research
        });
        
        var response = result.GetValue<string>()?.Trim();
        
        if (string.IsNullOrEmpty(response))
        {
            throw new Exception("Fact checker returned empty response");
        }

        Console.WriteLine("Raw response received. Validating JSON...");
        
        try
        {
            using var doc = JsonDocument.Parse(response);
            var formatted = JsonSerializer.Serialize(doc.RootElement, new JsonSerializerOptions 
            { 
                WriteIndented = true 
            });
            
            Console.WriteLine("JSON validation successful");
            return formatted;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"JSON validation failed: {ex.Message}");
            Console.WriteLine($"Raw response was: {response}");
            throw new Exception($"Fact checker returned invalid JSON response: {ex.Message}");
        }
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(WorkflowTasks))]
internal partial class SourceGenerationContext : JsonSerializerContext {}

public class WorkflowTasks
{
    [JsonPropertyName("content_type")]
    public string ContentType { get; set; } = "";
    
    [JsonPropertyName("tasks")]
    public List<TaskItem> Tasks { get; set; } = new();
}

public class TaskItem
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";
    
    [JsonPropertyName("worker")]
    public string Worker { get; set; } = "";
    
    [JsonPropertyName("task")]
    public string Task { get; set; } = "";
    
    [JsonPropertyName("priority")]
    public int Priority { get; set; }
    
    [JsonPropertyName("dependencies")]
    public List<string> Dependencies { get; set; } = new();
}

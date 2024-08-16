using Microsoft.SemanticKernel;
using SK_FilebasedPlugins.Plugins;


// Initialize the Semantic Kernel
var builder = Kernel.CreateBuilder();

// Get the API key from environment variables
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Please set the OPENAI_API_KEY environment variable.");
    return;
}

// Add OpenAI Chat Completion Service
builder.AddOpenAIChatCompletion(
//modelId: "gpt-3.5-turbo", // Specify the model you want to use
modelId: "gpt-4o-mini",
apiKey: apiKey // Your OpenAI API key
);

// Import the native functions
builder.Plugins.AddFromType<ManagedServicesNativePlugin>();

var kernel = builder.Build();



// Load the file-based semantic function
string pluginsFolder = GetPluginsFolder();
var prompts = kernel.ImportPluginFromPromptDirectory(pluginsFolder, "ExplainService");


// Define the input based on user prompt
string input = "Explain what Managed Room Service is";

// Use the semantic function
var serviceExplanation = await kernel.InvokeAsync<string>(prompts["ExplainService"],
    new KernelArguments { ["prompt"] = input });
Console.WriteLine("Explanation: " + serviceExplanation);

//create a separate line


Console.WriteLine();
Console.WriteLine("----------------------");
Console.WriteLine();

// Use native functions to get the status of the service
string? status = await kernel.InvokeAsync<string>(
    "ManagedServicesNativePlugin", "GetServiceStatus", new()
    {
        {"serviceName", "Managed Room Service"}
    });

Console.WriteLine(status);

//Use native functions to calculate the cost of the service

double cost = await kernel.InvokeAsync<double>(
       "ManagedServicesNativePlugin", "CalculateServiceCost", new()
       {
        {"basePrice", 100},
        {"userCount", 5}
    });

Console.WriteLine("The cost of the service for 5 users is: " + cost);







static string GetPluginsFolder()
{
    string baseDirectory = AppContext.BaseDirectory;
    string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", ".."));
    string pluginsDirectory = Path.Combine(projectRoot, "Plugins");
    string pluginsDirectorySub = Path.Combine(pluginsDirectory, "ManagedServicesPlugin");
    return pluginsDirectorySub;
}
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
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

// Use the semantic function by invoking it explicitly
var serviceExplanation = await kernel.InvokeAsync<string>(prompts["ExplainService"],
    new KernelArguments { ["prompt"] = input });
// Console.WriteLine("Explanation: " + serviceExplanation);

//create a separate line


Console.WriteLine();
//Console.WriteLine("----------------------");
Console.WriteLine();

// Use native functions to get the status of the service
string? status = await kernel.InvokeAsync<string>(
    "ManagedServicesNativePlugin", "GetServiceStatus", new()
    {
        {"serviceName", "Managed Room Service"}
    });

// Console.WriteLine(status);

//Use native functions to calculate the cost of the service

double cost = await kernel.InvokeAsync<double>(
       "ManagedServicesNativePlugin", "CalculateServiceCost", new()
       {
        {"basePrice", 100},
        {"userCount", 5}
    });

// Console.WriteLine("The cost of the service for 5 users is: " + cost);

// Create chat history

var history = new ChatHistory("You can check the status of managed services and calculate the cost of managed services.");

// Get chat completion service

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Start the conversation

Console.Write("User > ");

string? userInput;

while ((userInput = Console.ReadLine()) != null)
{
    // Add user input
    history.AddUserMessage(userInput);

    //Analyse the prompt
    //What is the status of managed room service and how much does it cost for 20 users?

    /////////////////////////////////////////////

    // Enable auto function calling
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
        ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions
    };


    // Get the response from the AI
    var result =
    await chatCompletionService.GetChatMessageContentAsync(
        history,
        executionSettings: openAIPromptExecutionSettings,
        kernel: kernel);


    // Print the results
    Console.WriteLine("Assistant > " + result);


    // Add the message from the agent to the chat history
    history.AddMessage(result.Role, result.Content ?? string.Empty);

    // Get user input again
    Console.Write("User > ");
}





static string GetPluginsFolder()
{
    string baseDirectory = AppContext.BaseDirectory;
    string projectRoot = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", ".."));
    string pluginsDirectory = Path.Combine(projectRoot, "Plugins");
    string pluginsDirectorySub = Path.Combine(pluginsDirectory, "ManagedServicesPlugin");
    return pluginsDirectorySub;
}
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// Get the API key from environment variables
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Please set the OPENAI_API_KEY environment variable.");
    return;
}

Console.WriteLine("Launching Semantic Kernel Sandbox for Managed Services");

// Initialize the Semantic Kernel
var builder = Kernel.CreateBuilder();

// Add OpenAI Chat Completion Service
builder.AddOpenAIChatCompletion(
    //modelId: "gpt-3.5-turbo", // Specify the model you want to use
    modelId: "gpt-4o-mini",
    apiKey: apiKey // Your OpenAI API key
);
var kernel = builder.Build();

// Create chat history

var history = new ChatHistory("You are an expert in Managed Services. You will explain concepts when a user supplies a prompt.");

// Get chat completion service

var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

// Start the conversation

Console.Write("User > ");

string? userInput;

while ((userInput = Console.ReadLine()) != null)
{
    // Add user input
    history.AddUserMessage(userInput);


    // Enable auto function calling
    OpenAIPromptExecutionSettings openAIPromptExecutionSettings = new()
    {
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


using Azure.Core;
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

    //Analyse the prompt
    //await AnalysePrompt(kernel, userInput);

    /////////////////////////////////////////////

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

static async Task AnalysePrompt(Kernel kernel, string? userInput)
{
    // prompt to send to AI
    string prompt = $"What is the intent of this request? {userInput}";
    Console.WriteLine("\n");
    // Print the results
    Console.WriteLine(await kernel.InvokePromptAsync(prompt));


    //Ensuring predictability by ensuring the LLM only handles a discrete set of intents
    string prompt2 = $@"What is the intent of this request? {userInput}
   You can choose between GetInformation, MakeQuote, ReportAFault.";
    Console.WriteLine("\n");
    // Print the results
    Console.WriteLine(await kernel.InvokePromptAsync(prompt2));

    //Further refine the output by adding some structure to the prompt by using additional formatting
    string prompt3 = @$"Instructions: What is the intent of this request? 
            Choices: GetInformation, MakeQuote, ReportAFault.
            Request: {userInput}
            Intent:";
    // Print the results
    Console.WriteLine(await kernel.InvokePromptAsync(prompt3));

    //Earlier examples were based on zero-shot prompting. You can use few shot prompting to provide more context to the AI
    string prompt4 = @$"Instructions: What is the intent of this request?
          Choices: GetInformation, MakeQuote, ReportAFault.
          User Input: Can you tell me about Managed Services?
          Intent: GetInformation
          User Input: Can you tell me how to make a quote?
          Intent: MakeQuote
          User Input: {userInput}
          Intent: ";
    Console.WriteLine(await kernel.InvokePromptAsync(prompt4));


    //Handling the unexpected
    string prompt5 = @$"
        Instructions: What is the intent of this request?
        If you don't know the intent, don't guess; instead respond with ""Unknown"".
        Choices: GetInformation, MakeQuote, ReportAFault.

        User Input: Can you tell me about Managed Services?
        Intent: GetInformation

        User Input: Can you tell me how to make a quote?
        Intent: MakeQuote

        User Input: Can you tell me how to file a complaint?
        Intent: ReportAFault

        User Input: {userInput}
        Intent: ";

     Console.WriteLine(await kernel.InvokePromptAsync(prompt5));
}
             
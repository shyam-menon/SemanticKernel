using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents.OpenAI;
using Microsoft.SemanticKernel.ChatCompletion;

// Initialize the Semantic Kernel
var builder = Kernel.CreateBuilder();

//builder.Plugins.AddFromType<EmailPlugin>();

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

var kernel = builder.Build();


// Optional: Specify an endpoint if you have one, otherwise it can be null
Uri? endpoint = null; 

// Optional: Provide a custom HttpClient if needed, otherwise it can be null
HttpClient? httpClient = null;

// Instantiate the OpenAIClientProvider
#pragma warning disable SKEXP0110
var provider = OpenAIClientProvider.ForOpenAI(apiKey, endpoint, httpClient);

// Use the provider as needed
var client = provider.Client;

// Create an instance of OpenAIAssistantDefinition
var assistantDefinition = new OpenAIAssistantDefinition("gpt-4o-mini")
{
    Description = "This is a sample assistant",
    Name = "SampleAssistant",
    Instructions = "You are an expert in the field of Managed Services. Describe the benefits of Managed Services to a potential customer.",
    EnableCodeInterpreter = true,
    EnableFileSearch = false,
    EnableJsonResponse = false,
    Temperature = 0.7f,
    TopP = 0.9f,
    Metadata = new Dictionary<string, string>
            {
                { "creator", "Shyam Menon" },
                { "purpose", "Demo" }
            }
};


#pragma warning disable SKEXP0110
OpenAIAssistantAgent agent =
    await OpenAIAssistantAgent.CreateAsync(
        kernel: kernel,
        clientProvider: provider,
       definition: assistantDefinition,
        cancellationToken: CancellationToken.None);

string threadId = await agent.CreateThreadAsync();

await agent.AddChatMessageAsync(threadId, new ChatMessageContent(AuthorRole.User, "What is Managed Conference Room Service ? Given a room that accomodates 15 people, estimate the cost of service and show the results in a table"));

await foreach (ChatMessageContent message in agent.InvokeAsync(threadId))
{
    Console.WriteLine(message);
}



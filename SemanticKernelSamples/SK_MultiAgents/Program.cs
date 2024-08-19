using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

public partial class Program
{
    private const string ArchitectName = "Architect";
    private const string ArchitectInstructions =
        """
        You are a senior software architect with extensive experience in system design and best practices.
        Your role is to review and evaluate the product requirements and design proposals from the product owner.
        Focus on understanding the features well so that a technical architecture can be built.
        If the proposal meets architectural standards and aligns with best practices, approve it.
        If not, provide detailed feedback on how to improve the design without giving specific implementation examples.         
        Scalability, maintainability, and overall system architecture are also important. 
        If you are unsure about a proposal, ask for clarification.
        If you are clear then respond with the message "approved".
        """;

    private const string ProductOwnerName = "ProductOwner";
    private const string ProductOwnerInstructions =
        """
        You are a product owner with a strong understanding of user needs and business requirements.
        Your goal is to propose product features and high-level designs that meet user needs and business goals.
        Provide clear, concise descriptions of features and their potential implementation.
        Consider the architect's feedback when refining your proposals.
        Focus on the value proposition and user experience in your descriptions.
        Avoid technical jargon and implementation details unless specifically asked.
        """;

    public static async Task Main(string[] args)
    {
        await new Program().MainAsync(args);
    }

    public async Task MainAsync(string[] args)
    {
        // Set up the logger
        ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddConsole();
        });
        ILogger logger = loggerFactory.CreateLogger<Program>();


        // Initialize the Semantic Kernel
        var architectBuilder = Kernel.CreateBuilder();
        var productOwnerBuilder = Kernel.CreateBuilder();

        // Get the API key from environment variables
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            logger.LogError("Please set the OPENAI_API_KEY environment variable.");
            return;
        }

        architectBuilder.AddOpenAIChatCompletion(
        //modelId: "gpt-3.5-turbo", // Specify the model you want to use
        modelId: "gpt-4o-mini",
        apiKey: apiKey // Your OpenAI API key
        );

        productOwnerBuilder.AddOpenAIChatCompletion(
        modelId: "gpt-3.5-turbo", // Specify the model you want to use
        //modelId: "gpt-4o-mini",
        apiKey: apiKey // Your OpenAI API key
        );

       var architect_kernel = architectBuilder.Build();
       var productOwner_kernel = productOwnerBuilder.Build();

       //var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

        // Define the agents
        #pragma warning disable SKEXP0110
        ChatCompletionAgent agentArchitect =
            new()
            {
                Instructions = ArchitectInstructions,
                Name = ArchitectName,
                Kernel = architect_kernel
            };

        ChatCompletionAgent agentProductOwner =
           new()
           {
               Instructions = ProductOwnerInstructions,
               Name = ProductOwnerName,
               Kernel = productOwner_kernel
           };

        // Create a chat for agent interaction.
        AgentGroupChat chat =
            new(agentProductOwner, agentArchitect)
            {
                ExecutionSettings =
                    new()
                    {
                        // Here a TerminationStrategy subclass is used that will terminate when
                        // an assistant message contains the term "approve".
                        TerminationStrategy =
                            new ApprovalTerminationStrategy()
                            {
                                // Only the architect may approve.
                                Agents = [agentArchitect],
                                // Limit total number of turns
                                MaximumIterations = 10,
                            }
                    }
            };

        // Invoke chat and display messages.
        string input = "concept: develop a sales tool to quote managed services";
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));
        LogMessage(logger, AuthorRole.User.ToString(), input);


        await foreach (ChatMessageContent content in chat.InvokeAsync())
        {
            #pragma warning disable SKEXP0001
            LogMessage(logger, content.AuthorName, content.Content);
        }


        logger.LogInformation($"# IS COMPLETE: {chat.IsComplete}");
    }

    private static void LogMessage(ILogger logger, string role, string message)
    {
        switch (role)
        {
            case "user":
                Console.ForegroundColor = ConsoleColor.Cyan;
                break;
            case "Assistant":
                Console.ForegroundColor = ConsoleColor.Green;
                break;           
            default:
                Console.ResetColor();
                break;
        }

        logger.LogInformation($"# {role}: '{message}'");
        Console.ResetColor();
    }

    private sealed class ApprovalTerminationStrategy : TerminationStrategy
    {
        // Terminate when the final message contains the term "approve"
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
    }
}




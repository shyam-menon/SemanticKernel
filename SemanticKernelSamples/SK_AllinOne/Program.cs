// 1.  Get the API key from environment variables
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.Reflection;
using static LLama.Common.ChatHistory;


public partial class Program
{
    private const string ArchitectName = "Architect";
    private const string ArchitectInstructions =
        """
        You are a senior software architect with extensive experience in system design and best practices.
        Your role is to review and evaluate the product requirements and design proposals from the product owner.
        You need to look at the context of the project and the user story and acceptance criteria provided and provide feedback on the design proposal.
        You need to look into you memory to understand the context of the project and the user story and acceptance criteria provided.
        If the proposal meets architectural standards and aligns with best practices, approve it.
        If not, provide detailed feedback on how to improve the design without giving specific implementation examples.
        Focus on scalability, maintainability, and overall system architecture. If you are unsure about a proposal, ask for clarification.
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

    private const string contextWithUserStoryAndAcceptanceCriterion =
        """
        The Software Solutions Enablement in Managed Print Flex project is a project that aims to enable software solutions (both standalone and software associated to a device) to be offered through the Managed Print Flex (MP Flex) portal via adding software solutions into the MP Flex catalogue. The project will deliver a portal that allows partners to quote and onboard software solutions for MP Flex customers, as well as a bridge tool that can generate the DART deal with the priced software as individual items. The project will also enable the MDM team to manage the partner eligibility and the catalog content for software solutions.
        The project is expected to have a positive impact on the company's software sales, customer satisfaction, and partner enablement, as well as reduce operational complexity and costs. The primary objectives of the project are to:
        Provide a catalog that contains software solutions (with or without install) for MP Flex
        Enable partners to quote and onboard software solutions through the MP Flex portal.
        Enable the bridge tool to generate the DART deal with the software solutions as individual items.
        Enable the MDM team to manage the partner eligibility and the catalog content for software solutions.

                BR1: Catalog of Software Solutions
        As a partner, I want to view the catalog of software solutions for MP Flex so that I can select the software solutions that meet the customer's needs.
        Acceptance Criteria for BR1:
        •	Given the partner is logged in to the portal, when the partner wants to view the catalog of software solutions, then the portal should display the catalog of software solutions that are available for MP Flex, such as software as a service, support as a service, and perpetual license.
        •	Given the partner is viewing the catalog of software solutions, when the partner wants to view the details of a software solution, such as features, specifications, prices, availability, and install options, then the portal should display the details of the software solution.
        •	Given the partner is viewing the catalog of software solutions, when the partner wants to filter or sort the software solutions by criteria, such as name, category, price, or availability, then the portal should provide the options to do so.
        
        """;

    public static async Task Main(string[] args)
    {
        await new Program().MainAsync(args);
    }

    public async Task MainAsync(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Please set the OPENAI_API_KEY environment variable.");
            return;
        }


        //2. Create a Kernel Builder. Using Serverless Memory so that external setup is not needed
        //var memory = new KernelMemoryBuilder()
        //    .WithOpenAIDefaults(apiKey)
        //    .Build<MemoryServerless>();

        //3. Feed the memory with the document and a web page
        //await memory.ImportDocumentAsync("ManagedServices_CBA.pdf", documentId: "doc001");

        //4.Build the architect agent and load the memory
        var architectBuilder = Kernel.CreateBuilder();        
        //architectBuilder.Plugins.AddFromObject(new KernelMemoryPlugin(memory));

        //5. Build the product owner agent
        var productOwnerBuilder = Kernel.CreateBuilder();


        //4. Configure and build the agents
        architectBuilder.AddOpenAIChatCompletion(
        //modelId: "gpt-3.5-turbo", // Specify the model you want to use
        modelId: "gpt-4o-mini",
        apiKey: apiKey // Your OpenAI API key
        );

        productOwnerBuilder.AddOpenAIChatCompletion(
        modelId: "gpt-4o-mini", // Specify the model you want to use
        //modelId: "gpt-4o-mini",
        apiKey: apiKey // Your OpenAI API key
        );

        var architect_kernel = architectBuilder.Build();
        var productOwner_kernel = productOwnerBuilder.Build();

        // Define the agents
#pragma warning disable SKEXP0110
        ChatCompletionAgent agentArchitect =
            new()
            {
                Instructions = ArchitectInstructions,
                Name = ArchitectName,
                Kernel = architect_kernel,
                Arguments = new KernelArguments(new OpenAIPromptExecutionSettings() { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions })
            };

        // Initialize plugin and add to the agent's Kernel (same as direct Kernel usage).
        //KernelPlugin plugin = KernelPluginFactory.CreateFromType<KernelMemoryPlugin>();
        //agentArchitect.Kernel.Plugins.Add(plugin);

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

        
        var input = contextWithUserStoryAndAcceptanceCriterion;
        chat.AddChatMessage(new ChatMessageContent(AuthorRole.User, input));       


        await foreach (ChatMessageContent content in chat.InvokeAsync())
        {
            Console.WriteLine($"# {content.Role} - : {content.InnerContent}");
        }


        Console.WriteLine($"# IS COMPLETE: {chat.IsComplete}");


    }

    private sealed class ApprovalTerminationStrategy : TerminationStrategy
    {
        // Terminate when the final message contains the term "approve"
        protected override Task<bool> ShouldAgentTerminateAsync(Agent agent, IReadOnlyList<ChatMessageContent> history, CancellationToken cancellationToken)
            => Task.FromResult(history[history.Count - 1].Content?.Contains("approve", StringComparison.OrdinalIgnoreCase) ?? false);
    }  
}

        
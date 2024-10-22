using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.History;
using Microsoft.SemanticKernel.Agents.Chat;

namespace SK_AgentTroubleshoot
{
    public class PrinterTroubleShooterAgents
    {
        private readonly Kernel _kernel;
#pragma warning disable SKEXP0110
        private readonly AgentGroupChat _agentChat;
        private readonly ChatCompletionAgent _diagnosticAgent;
        private readonly ChatCompletionAgent _repairAgent;
        private readonly ChatCompletionAgent _verificationAgent;
        private readonly ILogger<PrinterTroubleShooterAgents> _logger;

        public PrinterTroubleShooterAgents(string endpoint, string apiKey, string deploymentName, ILogger<PrinterTroubleShooterAgents> logger)
        {
            _logger = logger;

            // Setup logging services
            var services = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                });

            var serviceProvider = services.BuildServiceProvider();
            var pluginLogger = serviceProvider.GetRequiredService<ILogger<PrinterPlugin>>();

            // Initialize kernel with Azure OpenAI
            var builder = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);

            // Add printer plugin with logger
            builder.Plugins.AddFromObject(new PrinterPlugin(pluginLogger), "PrinterPlugin");
            _kernel = builder.Build();

            // Create specialized agents
            _diagnosticAgent = CreateDiagnosticAgent();
            _repairAgent = CreateRepairAgent();
            _verificationAgent = CreateVerificationAgent();


            const string DiagnosticAgent = "DiagnosticAgent";
            const string RepairAgent = "RepairAgent";
            const string VerificationAgent = "VerificationAgent";

            var selectionFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
                $$$"""
                Examine the provided RESPONSE and choose the next participant.
                State only the name of the chosen participant without explanation.
                Never choose the participant named in the RESPONSE.

                Choose only from these participants:
                - {{{DiagnosticAgent}}}
                - {{{RepairAgent}}}
                - {{{VerificationAgent}}}

                Always follow these rules when choosing the next participant:
                - If RESPONSE is user input, it is {{{DiagnosticAgent}}}'s turn.
                - If RESPONSE contains "DIAGNOSTIC FINDINGS:", it is {{{RepairAgent}}}'s turn.
                - If RESPONSE contains "REPAIRS COMPLETED:", it is {{{VerificationAgent}}}'s turn.
                - If RESPONSE contains "ADDITIONAL ISSUES DETECTED:", it is {{{DiagnosticAgent}}}'s turn.

                RESPONSE:
                {{$lastmessage}}
                """,
                safeParameterNames: "lastmessage");

            const string TerminationToken = "yes";

            KernelFunction terminationFunction =
                AgentGroupChat.CreatePromptFunctionForStrategy(
                    $$$"""
                Examine the RESPONSE and determine whether the content has been deemed satisfactory.
                If content is satisfactory, respond with a single word without explanation: {{{TerminationToken}}}.
                If specific suggestions are being provided, it is not satisfactory.
                If no correction is suggested, it is satisfactory.

                RESPONSE:
                {{$lastmessage}}
                """,
                    safeParameterNames: "lastmessage");

            ChatHistoryTruncationReducer historyReducer = new(1);

            // Setup agent chat with collaboration strategy
            _agentChat = new AgentGroupChat(_diagnosticAgent, _repairAgent, _verificationAgent)
            {
                ExecutionSettings = new AgentGroupChatSettings
                {
                    //SelectionStrategy = CreateSelectionStrategy(),
                    SelectionStrategy = new KernelFunctionSelectionStrategy(selectionFunction, _kernel)
                    {
                        // Always start with the editor agent.
                        InitialAgent = _diagnosticAgent,
                        // Save tokens by only including the final response
                        HistoryReducer = historyReducer,
                        // The prompt variable name for the history argument.
                        HistoryVariableName = "lastmessage",
                        // Returns the entire result value as a string.
                        ResultParser = (result) => result.GetValue<string>() ?? _diagnosticAgent.Name
                    },
                    TerminationStrategy = CreateTerminationStrategy()
                    //TerminationStrategy = new KernelFunctionTerminationStrategy(terminationFunction, _kernel)
                    //{
                    //    // Only evaluate for editor's response
                    //    Agents = [_diagnosticAgent],
                    //    // Save tokens by only including the final response
                    //    HistoryReducer = historyReducer,
                    //    // The prompt variable name for the history argument.
                    //    HistoryVariableName = "lastmessage",
                    //    // Limit total number of turns
                    //    MaximumIterations = 12,
                    //    // Customer result parser to determine if the response is "yes"
                    //    ResultParser = (result) => result.GetValue<string>()?.Contains(TerminationToken, StringComparison.OrdinalIgnoreCase) ?? false
                    //}
                }
            };
            _logger = logger;
        }

        private ChatCompletionAgent CreateDiagnosticAgent()
        {
            return new ChatCompletionAgent
            {
                Name = "DiagnosticAgent",
                Instructions = """
            You are a printer diagnostic specialist. Your responsibility is to diagnose printer issues.
            Always run diagnostics in this order: GetPrinterStatus, CheckConnection, CheckPaper, CheckToner.
            
            After diagnostics, format your response EXACTLY like this:
            DIAGNOSTIC FINDINGS:
            1. [First issue found]
            2. [Second issue found]
            etc...
            
            Never perform repairs or make suggestions - only diagnose issues.
            """,
                Kernel = _kernel,
                Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings()
                {
#pragma warning disable SKEXP0001
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        private ChatCompletionAgent CreateRepairAgent()
        {
            return new ChatCompletionAgent
            {
                Name = "RepairAgent",
                Instructions = """
            You are a printer repair specialist. Execute these steps in order:
            1. Read the diagnostic findings
            2. For each issue found:
               - Execute the appropriate repair function
               - Document the result
            3. End your message with EXACTLY:
            
            REPAIRS COMPLETED:
            [List all repairs performed]
            
            Do not ask questions or make suggestions. Focus only on executing repairs.
            """,
                Kernel = _kernel,
                Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }

        private ChatCompletionAgent CreateVerificationAgent()
        {
            return new ChatCompletionAgent
            {
                Name = "VerificationAgent",
                Instructions = """
            You are a verification specialist. Follow this process:
            1. Use GetPrinterStatus to check current state
            2. Verify each repair that was performed
            3. End your message with EXACTLY ONE of these:
            
            IF any issues remain:
            ADDITIONAL ISSUES DETECTED:
            [List remaining issues]
            
            IF everything is fixed:
            ALL ISSUES RESOLVED
            
            Do not make suggestions or ask questions. Focus only on verification.
            """,
                Kernel = _kernel,
                Arguments =
            new KernelArguments(
                new AzureOpenAIPromptExecutionSettings()
                {
                    FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
                })
            };
        }
       

        private KernelFunctionTerminationStrategy CreateTerminationStrategy()
        {
            var terminationFunction = AgentGroupChat.CreatePromptFunctionForStrategy("""
        Your task is to determine if the troubleshooting session should end.
        The session should ONLY end when ALL of these have occurred:
        1. DiagnosticAgent has provided findings
        2. RepairAgent has completed repairs
        3. VerificationAgent has confirmed resolution
        
        Analyze this message:
        {{$lastmessage}}
        
        Respond with ONLY one of these:
        - 'CONTINUE' if any of the above steps are missing
        - 'COMPLETE' if you see "ALL ISSUES RESOLVED"
        """,
          safeParameterNames: "lastmessage");

            return new KernelFunctionTerminationStrategy(terminationFunction, _kernel)
            {
                MaximumIterations = 10,
                ResultParser = (result) =>
                {
                    var decision = result.GetValue<string>()?.Trim().ToUpperInvariant() == "COMPLETE";
                    _logger.LogInformation($"Termination decision: {(decision ? "Complete" : "Continue")}");
                    return decision;
                }
            };
        }

        public async Task HandlePrinterIssue(string userIssue)
        {
            try
            {
                await _agentChat.ResetAsync();
                _agentChat.IsComplete = false;


                _agentChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, userIssue));

                var turnCount = 0;
                var lastAgent = string.Empty;

                await foreach (var response in _agentChat.InvokeAsync())
                {
                    turnCount++;
                    Console.WriteLine($"\n--- Turn {turnCount} ---");
                    #pragma warning disable SKEXP0001
                    Console.WriteLine($"{response.AuthorName.ToUpperInvariant()}:{Environment.NewLine}{response.Content}");


                }

                Console.WriteLine("\n=== Troubleshooting Session Complete ===");
                Console.WriteLine($"Total turns: {turnCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error during troubleshooting: {ex.Message}");
                Console.WriteLine($"Error during troubleshooting: {ex.Message}");
            }
        }

        public async Task ResetChat()
        {
            await _agentChat.ResetAsync();
            Console.WriteLine("Chat session has been reset.");
        }

        // Add test methods
        public async Task RunDiagnosticTests()
        {
            Console.WriteLine("\n=== Running Diagnostic Tests ===");

            var testCases = new[]
            {
            "My printer won't print anything",
            "I'm getting a paper jam error",
            "The printer says toner is low",
            "Print queue seems stuck",
            "Printer is offline"
        };

            foreach (var testCase in testCases)
            {
                Console.WriteLine($"\nTesting scenario: {testCase}");
                await HandlePrinterIssue(testCase);
                await Task.Delay(2000); // Delay between tests
            }
        }
    }
}

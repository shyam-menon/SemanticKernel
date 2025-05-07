using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.MultiAgentOrchestration;
using System.Text;

namespace Microsoft.SemanticKernel.MultiAgentOrchestration
{
    /// <summary>
    /// Demonstrates the decentralized orchestration pattern with Semantic Kernel
    /// </summary>
    public class DecentralizedOrchestration
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger _logger;
        private readonly AgentCommunicationHub _communicationHub;
        private readonly Dictionary<string, PeerAgent> _agents = new();
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _deploymentName;
        
        public DecentralizedOrchestration(ILoggerFactory loggerFactory, string apiKey, string endpoint, string deploymentName = "gpt-4")
        {
            _loggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<DecentralizedOrchestration>();
            _communicationHub = new AgentCommunicationHub(loggerFactory.CreateLogger<AgentCommunicationHub>());
            _apiKey = apiKey;
            _endpoint = endpoint;
            _deploymentName = deploymentName;
            
            // Register a dummy user agent to handle user messages
            _communicationHub.RegisterAgent("user", new DummyUserAgent(_loggerFactory.CreateLogger<DummyUserAgent>()));
        }
        
        /// <summary>
        /// Run the decentralized orchestration sample
        /// </summary>
        public async Task RunAsync(string userRequest, bool useSimulationMode = false)
        {
            _logger.LogInformation("Starting Decentralized Orchestration Sample");
            
            // Create the peer agents
            await CreatePeerAgentsAsync();
            
            // Have agents introduce themselves to the network
            await IntroduceAgentsAsync();
            
            // Process the user request
            var conversationId = Guid.NewGuid().ToString();
            _logger.LogInformation($"Processing user request with conversation ID: {conversationId}");
            
            // Determine which agent should handle the initial request
            var initialAgentId = await DetermineInitialAgentAsync(userRequest, useSimulationMode);
            
            // Send the task to the initial agent
            var taskMessage = new AgentMessage
            {
                MessageType = "TaskRequest",
                ConversationId = conversationId,
                Content = new Dictionary<string, object>
                {
                    { "taskDescription", userRequest },
                    { "useSimulationMode", useSimulationMode }
                }
            };
            
            _logger.LogInformation($"Sending initial task to agent: {initialAgentId}");
            var response = await _communicationHub.SendMessageAsync("user", initialAgentId, taskMessage);
            
            // Process the response
            await ProcessResponseAsync(response, conversationId, userRequest);
        }
        
        /// <summary>
        /// Create the peer agents for the decentralized network
        /// </summary>
        private async Task CreatePeerAgentsAsync()
        {
            // Create research agent
            var researchKernel = CreateKernel();
            var researchAgent = new PeerAgent(
                "research",
                "research",
                researchKernel,
                _communicationHub,
                _loggerFactory.CreateLogger<PeerAgent>());
            _agents.Add("research", researchAgent);
            
            // Create analysis agent
            var analysisKernel = CreateKernel();
            var analysisAgent = new PeerAgent(
                "analysis",
                "analysis",
                analysisKernel,
                _communicationHub,
                _loggerFactory.CreateLogger<PeerAgent>());
            _agents.Add("analysis", analysisAgent);
            
            // Create writing agent
            var writingKernel = CreateKernel();
            var writingAgent = new PeerAgent(
                "writing",
                "writing",
                writingKernel,
                _communicationHub,
                _loggerFactory.CreateLogger<PeerAgent>());
            _agents.Add("writing", writingAgent);
            
            // Create review agent
            var reviewKernel = CreateKernel();
            var reviewAgent = new PeerAgent(
                "review",
                "review",
                reviewKernel,
                _communicationHub,
                _loggerFactory.CreateLogger<PeerAgent>());
            _agents.Add("review", reviewAgent);
        }
        
        /// <summary>
        /// Have agents introduce themselves to the network
        /// </summary>
        private async Task IntroduceAgentsAsync()
        {
            _logger.LogInformation("Agents introducing themselves to the network");
            
            var tasks = new List<Task>();
            foreach (var agent in _agents.Values)
            {
                tasks.Add(agent.IntroduceToNetworkAsync());
            }
            
            await Task.WhenAll(tasks);
            
            _logger.LogInformation("All agents have been introduced to the network");
        }
        
        /// <summary>
        /// Determine which agent should handle the initial request
        /// </summary>
        private async Task<string> DetermineInitialAgentAsync(string userRequest, bool useSimulationMode)
        {
            if (useSimulationMode)
            {
                // In simulation mode, use simple keyword matching
                if (userRequest.Contains("research", StringComparison.OrdinalIgnoreCase) || 
                    userRequest.Contains("information", StringComparison.OrdinalIgnoreCase) ||
                    userRequest.Contains("data", StringComparison.OrdinalIgnoreCase))
                {
                    return "research";
                }
                else if (userRequest.Contains("analysis", StringComparison.OrdinalIgnoreCase) || 
                         userRequest.Contains("analyze", StringComparison.OrdinalIgnoreCase) ||
                         userRequest.Contains("insights", StringComparison.OrdinalIgnoreCase))
                {
                    return "analysis";
                }
                else if (userRequest.Contains("write", StringComparison.OrdinalIgnoreCase) || 
                         userRequest.Contains("content", StringComparison.OrdinalIgnoreCase) ||
                         userRequest.Contains("document", StringComparison.OrdinalIgnoreCase))
                {
                    return "writing";
                }
                else if (userRequest.Contains("review", StringComparison.OrdinalIgnoreCase) || 
                         userRequest.Contains("improve", StringComparison.OrdinalIgnoreCase) ||
                         userRequest.Contains("feedback", StringComparison.OrdinalIgnoreCase))
                {
                    return "review";
                }
                
                // Default to research agent
                return "research";
            }
            
            // In real mode, use LLM to determine the most appropriate agent
            var kernel = CreateKernel();
            var routingPrompt = $@"
Determine which specialized agent should handle the following user request:

User Request: ""{userRequest}""

Available agents and their specialties:
- research: Researching information and gathering data
- analysis: Analyzing data and providing insights
- writing: Creating well-written content based on research and analysis
- review: Reviewing and improving content for accuracy and quality

Return only the agent ID (research, analysis, writing, or review) that should handle this request.
";
            
            try
            {
                var routingResponse = await kernel.InvokePromptAsync(routingPrompt);
                var agentId = routingResponse.ToString().Trim().ToLower();
                
                // Validate the response
                if (_agents.ContainsKey(agentId))
                {
                    return agentId;
                }
                
                // Default to research agent if response is invalid
                return "research";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error determining initial agent");
                
                // Default to research agent
                return "research";
            }
        }
        
        /// <summary>
        /// Process the response from an agent
        /// </summary>
        private async Task ProcessResponseAsync(AgentMessage response, string conversationId, string userRequest)
        {
            switch (response.MessageType)
            {
                case "TaskResult":
                    await HandleTaskResultAsync(response, conversationId, userRequest);
                    break;
                
                case "TaskDelegated":
                    _logger.LogInformation($"Task delegated to agent: {response.Content["delegatedTo"]}");
                    _logger.LogInformation($"Reason: {response.Content["reason"]}");
                    
                    // Get the result from the delegated agent
                    var delegatedAgentId = response.Content["delegatedTo"].ToString();
                    _logger.LogInformation($"Following up with delegated agent: {delegatedAgentId}");
                    
                    // Wait a bit to allow the delegated agent to process the task
                    await Task.Delay(500);
                    
                    // Query the delegated agent for the result
                    var queryMessage = new AgentMessage
                    {
                        MessageType = "QueryRequest",
                        ConversationId = conversationId,
                        Content = new Dictionary<string, object>
                        {
                            { "query", $"What is the result of the task: {userRequest}?" }
                        }
                    };
                    
                    try
                    {
                        var delegatedResponse = await _communicationHub.SendMessageAsync("user", delegatedAgentId, queryMessage);
                        if (delegatedResponse.MessageType == "QueryResponse")
                        {
                            var result = delegatedResponse.Content["response"].ToString();
                            
                            // Create a synthetic TaskResult message
                            var taskResultMessage = new AgentMessage
                            {
                                MessageType = "TaskResult",
                                ConversationId = conversationId,
                                Content = new Dictionary<string, object>
                                {
                                    { "result", result }
                                }
                            };
                            
                            await HandleTaskResultAsync(taskResultMessage, conversationId, userRequest);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error following up with delegated agent {delegatedAgentId}");
                    }
                    break;
                
                case "Error":
                    _logger.LogError($"Received error message: {response.Content["error"]}");
                    break;
                    
                default:
                    _logger.LogWarning($"Received unexpected message type: {response.MessageType}");
                    break;
            }
        }
        
        /// <summary>
        /// Handle a task result from an agent
        /// </summary>
        private async Task HandleTaskResultAsync(AgentMessage response, string conversationId, string userRequest)
        {
            var result = response.Content["result"].ToString();
            _logger.LogInformation($"Received task result: {result}");
            
            // In a real implementation, this would determine if additional processing is needed
            // For this sample, we'll generate a report with the result
            
            // Create the report
            var report = new StringBuilder();
            report.AppendLine($"# DECENTRALIZED REPORT: {userRequest}");
            report.AppendLine("## Generated by Multi-Agent System (Decentralized)");
            report.AppendLine($"## Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine();
            report.AppendLine("### Executive Summary");
            report.AppendLine("This report was created through a decentralized multi-agent system where agents coordinate directly with each other.");
            report.AppendLine();
            report.AppendLine("### Result");
            report.AppendLine(result);
            
            // Save the report to a file
            var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "reports");
            if (!Directory.Exists(reportsDirectory))
            {
                Directory.CreateDirectory(reportsDirectory);
            }
            
            var reportFilePath = Path.Combine(reportsDirectory, $"decentralized_report_{DateTime.Now:yyyyMMdd_HHmmss}.md");
            await File.WriteAllTextAsync(reportFilePath, report.ToString());
            
            // Display the final result
            Console.WriteLine("\nDecentralized agents have completed the task:");
            Console.WriteLine("==================================================");
            Console.WriteLine(result);
            Console.WriteLine("==================================================");
            Console.WriteLine($"\nThe complete report has been saved to: {reportFilePath}");
        }
        
        /// <summary>
        /// Create a Kernel instance for an agent
        /// </summary>
        private Kernel CreateKernel()
        {
            var builder = Kernel.CreateBuilder();
            
            // Configure Azure OpenAI chat completion service
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: _deploymentName,
                endpoint: _endpoint,
                apiKey: _apiKey);
            
            return builder.Build();
        }
    }
    
    /// <summary>
    /// A simple agent implementation to represent the user in the communication hub
    /// </summary>
    internal class DummyUserAgent : IAgent
    {
        private readonly ILogger _logger;
        
        public DummyUserAgent(ILogger logger)
        {
            _logger = logger;
        }
        
        public Task<AgentMessage> ProcessMessageAsync(string fromAgentId, AgentMessage message)
        {
            // This agent doesn't process any messages, it just exists to allow the user to send messages
            _logger.LogInformation($"User received message from {fromAgentId}: {message.MessageType}");
            
            // Return a simple acknowledgment
            return Task.FromResult(new AgentMessage
            {
                MessageType = "Acknowledgment",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "status", "received" }
                }
            });
        }
    }
}

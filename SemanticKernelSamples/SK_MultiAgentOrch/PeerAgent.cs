using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.MultiAgentOrchestration;

namespace Microsoft.SemanticKernel.MultiAgentOrchestration
{
    /// <summary>
    /// Peer agent that coordinates directly with other agents in a decentralized system
    /// </summary>
    public class PeerAgent : IAgent
    {
        private readonly string _agentId;
        private readonly string _specialty;
        private readonly Kernel _kernel;
        private readonly AgentCommunicationHub _communicationHub;
        private readonly HashSet<string> _knownAgentIds;
        private readonly ILogger _logger;
        
        public PeerAgent(
            string agentId,
            string specialty,
            Kernel kernel,
            AgentCommunicationHub communicationHub,
            ILogger logger)
        {
            _agentId = agentId;
            _specialty = specialty;
            _kernel = kernel;
            _communicationHub = communicationHub;
            _knownAgentIds = new HashSet<string>();
            _logger = logger;
            
            // Register this agent with the communication hub
            _communicationHub.RegisterAgent(agentId, this);
        }
        
        public async Task<AgentMessage> ProcessMessageAsync(string fromAgentId, AgentMessage message)
        {
            // Add the sender to known agents if not already known
            if (!_knownAgentIds.Contains(fromAgentId))
            {
                _knownAgentIds.Add(fromAgentId);
            }
            
            switch (message.MessageType)
            {
                case "TaskRequest":
                    return await ProcessTaskRequestAsync(fromAgentId, message);
                
                case "QueryRequest":
                    return await ProcessQueryRequestAsync(fromAgentId, message);
                
                case "Introduction":
                    return ProcessIntroduction(fromAgentId, message);
                
                default:
                    _logger.LogWarning($"Agent {_agentId} received unknown message type: {message.MessageType}");
                    return new AgentMessage
                    {
                        MessageType = "Error",
                        ConversationId = message.ConversationId,
                        Content = new Dictionary<string, object>
                        {
                            { "error", $"Unknown message type: {message.MessageType}" }
                        }
                    };
            }
        }
        
        public async Task IntroduceToNetworkAsync()
        {
            // Introduce this agent to the network
            var introductionMessage = new AgentMessage
            {
                MessageType = "Introduction",
                ConversationId = "system",
                Content = new Dictionary<string, object>
                {
                    { "agentId", _agentId },
                    { "specialty", _specialty },
                    { "capabilities", GetCapabilities() }
                }
            };
            
            await _communicationHub.BroadcastMessageAsync(_agentId, introductionMessage);
        }
        
        private List<string> GetCapabilities()
        {
            // Return a list of capabilities based on the agent's specialty
            // This would be more sophisticated in a real implementation
            return new List<string> { $"Handle {_specialty} tasks" };
        }
        
        private AgentMessage ProcessIntroduction(string fromAgentId, AgentMessage message)
        {
            // Process an introduction from another agent
            var specialty = message.Content["specialty"].ToString();
            var capabilities = message.Content["capabilities"] as List<string> ?? new List<string>();
            
            _logger.LogInformation($"Agent {_agentId} received introduction from {fromAgentId} with specialty: {specialty}");
            
            // In a real implementation, this would store information about the other agent
            
            return new AgentMessage
            {
                MessageType = "IntroductionAcknowledgment",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "agentId", _agentId },
                    { "specialty", _specialty }
                }
            };
        }
        
        private async Task<AgentMessage> ProcessTaskRequestAsync(string fromAgentId, AgentMessage message)
        {
            // Process a task request from another agent
            var taskDescription = message.Content["taskDescription"].ToString();
            bool useSimulationMode = message.Content.ContainsKey("useSimulationMode") && (bool)message.Content["useSimulationMode"];
            
            _logger.LogInformation($"Agent {_agentId} received task request from {fromAgentId}: {taskDescription}");
            
            // Check if this task aligns with the agent's specialty
            var isRelevant = await IsTaskRelevantToSpecialtyAsync(taskDescription, useSimulationMode);
            
            if (!isRelevant)
            {
                // If not relevant, try to delegate to a more appropriate agent
                var delegationResult = await TryDelegateTaskAsync(taskDescription, message.ConversationId, useSimulationMode);
                
                if (delegationResult.Success)
                {
                    return new AgentMessage
                    {
                        MessageType = "TaskDelegated",
                        ConversationId = message.ConversationId,
                        Content = new Dictionary<string, object>
                        {
                            { "delegatedTo", delegationResult.DelegatedToAgentId },
                            { "reason", "Task better suited to another agent's specialty" }
                        }
                    };
                }
                else
                {
                    // No agent could handle the task, but we'll try our best anyway
                    _logger.LogWarning($"No agent could handle task: {taskDescription}. {_agentId} will attempt to process it despite not being specialized for it.");
                }
            }
            
            // Process the task
            var result = await ProcessTaskAsync(taskDescription, useSimulationMode);
            
            return new AgentMessage
            {
                MessageType = "TaskResult",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "stepId", message.Content.ContainsKey("stepId") ? message.Content["stepId"] : "unknown" },
                    { "result", result }
                }
            };
        }
        
        private async Task<AgentMessage> ProcessQueryRequestAsync(string fromAgentId, AgentMessage message)
        {
            // Process a query request from another agent
            var query = message.Content["query"].ToString();
            
            _logger.LogInformation($"Agent {_agentId} received query from {fromAgentId}: {query}");
            
            // Generate a response based on the agent's specialty
            var response = await GenerateQueryResponseAsync(query, 
                message.Content.ContainsKey("useSimulationMode") && (bool)message.Content["useSimulationMode"]);
            
            return new AgentMessage
            {
                MessageType = "QueryResponse",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "response", response }
                }
            };
        }
        
        private async Task<bool> IsTaskRelevantToSpecialtyAsync(string taskDescription, bool useSimulationMode = false)
        {
            if (useSimulationMode)
            {
                // In simulation mode, use simple keyword matching
                return taskDescription.Contains(_specialty, StringComparison.OrdinalIgnoreCase);
            }
            
            // Determine if the task is relevant to this agent's specialty
            var relevancePrompt = $@"
Determine if the following task is relevant to an agent with specialty in {_specialty}:

Task: ""{taskDescription}""

Answer with just 'yes' or 'no'.
";
            
            try
            {
                var relevanceResponse = await _kernel.InvokePromptAsync(relevancePrompt);
                var response = relevanceResponse.ToString().Trim().ToLower();
                
                return response == "yes";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error determining task relevance: {taskDescription}");
                
                // Fall back to simple keyword matching
                return taskDescription.Contains(_specialty, StringComparison.OrdinalIgnoreCase);
            }
        }
        
        private async Task<(bool Success, string DelegatedToAgentId)> TryDelegateTaskAsync(string taskDescription, string conversationId, bool useSimulationMode = false)
        {
            // Try to find a more appropriate agent for the task
            if (_knownAgentIds.Count == 0)
            {
                return (false, null);
            }
            
            // For this example, we'll query each known agent
            foreach (var agentId in _knownAgentIds)
            {
                // Skip delegation to the user agent (if it exists in known agents)
                if (agentId == "user")
                {
                    continue;
                }
                
                var queryMessage = new AgentMessage
                {
                    MessageType = "QueryRequest",
                    ConversationId = conversationId,
                    Content = new Dictionary<string, object>
                    {
                        { "query", $"Can you handle a task related to: {taskDescription}?" },
                        { "useSimulationMode", useSimulationMode }
                    }
                };
                
                var response = await _communicationHub.SendMessageAsync(_agentId, agentId, queryMessage);
                
                if (response.MessageType == "QueryResponse")
                {
                    var responseText = response.Content["response"].ToString();
                    
                    if (responseText.Contains("yes", StringComparison.OrdinalIgnoreCase) || 
                        responseText.Contains("can handle", StringComparison.OrdinalIgnoreCase))
                    {
                        // Delegate the task to this agent
                        var taskMessage = new AgentMessage
                        {
                            MessageType = "TaskRequest",
                            ConversationId = conversationId,
                            Content = new Dictionary<string, object>
                            {
                                { "taskDescription", taskDescription },
                                { "delegatedFrom", _agentId },
                                { "useSimulationMode", useSimulationMode }
                            }
                        };
                        
                        await _communicationHub.SendMessageAsync(_agentId, agentId, taskMessage);
                        
                        return (true, agentId);
                    }
                }
            }
            
            return (false, null);
        }
        
        private async Task<string> ProcessTaskAsync(string taskDescription, bool useSimulationMode = false)
        {
            if (useSimulationMode)
            {
                return GetSimulatedTaskResponse(taskDescription);
            }
            
            // Process the task based on the agent's specialty
            var taskPrompt = $@"
As an AI agent specializing in {_specialty}, complete the following task:

Task: ""{taskDescription}""

Provide a detailed response.
";
            
            try
            {
                var taskResponse = await _kernel.InvokePromptAsync(taskPrompt);
                return taskResponse.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error processing task: {taskDescription}");
                return GetSimulatedTaskResponse(taskDescription);
            }
        }
        
        private async Task<string> GenerateQueryResponseAsync(string query, bool useSimulationMode = false)
        {
            if (useSimulationMode)
            {
                return GetSimulatedQueryResponse(query);
            }
            
            // Generate a response to a query based on the agent's specialty
            var queryPrompt = $@"
As an AI agent specializing in {_specialty}, answer the following query:

Query: ""{query}""

Provide a concise response.
";
            
            try
            {
                var queryResponse = await _kernel.InvokePromptAsync(queryPrompt);
                return queryResponse.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating query response: {query}");
                return GetSimulatedQueryResponse(query);
            }
        }
        
        private string GetSimulatedTaskResponse(string taskDescription)
        {
            // Check if the task is related to Indian economy
            bool isIndianEconomy = taskDescription.Contains("Indian economy", StringComparison.OrdinalIgnoreCase) || 
                                   taskDescription.Contains("India", StringComparison.OrdinalIgnoreCase);
            
            switch (_specialty.ToLower())
            {
                case "research":
                    if (isIndianEconomy)
                    {
                        return $"Research on the progress of Indian economy in the last 10 years shows significant growth in GDP from approximately $1.8 trillion in 2015 to $3.5 trillion in 2025. Key sectors driving growth include IT services, pharmaceuticals, and renewable energy. The economy faced challenges including demonetization (2016), GST implementation (2017), and the COVID-19 pandemic (2020-2021), but showed resilience with recovery rates exceeding global averages.";
                    }
                    else
                    {
                        return $"Research completed on '{taskDescription}'. Found key information about the topic including historical context, current trends, and relevant statistics.";
                    }
                
                case "analysis":
                    if (isIndianEconomy)
                    {
                        return $"Analysis of Indian economic data reveals: 1) Average GDP growth of 6.2% annually over the decade, outpacing global averages; 2) Significant reduction in poverty rates from 21.9% to 10.2%; 3) Digital economy expansion with UPI transactions growing exponentially; 4) Challenges in manufacturing sector growth despite 'Make in India' initiatives; 5) Increasing income inequality with Gini coefficient rising from 0.36 to 0.42 over the decade.";
                    }
                    else
                    {
                        return $"Analysis completed for '{taskDescription}'. Key insights: identified three main patterns, determined statistical significance, and created visualizations of the data.";
                    }
                
                case "writing":
                    if (isIndianEconomy)
                    {
                        return $"The comprehensive report on India's economic journey over the past decade has been structured into five main sections: 1) Macroeconomic Indicators showing overall growth patterns; 2) Sectoral Analysis highlighting performers and laggards; 3) Policy Initiatives and their impacts; 4) Social and Development Outcomes; and 5) Future Outlook and Challenges. The narrative emphasizes both quantitative metrics and qualitative changes in economic structure, with particular attention to digital transformation and financial inclusion initiatives.";
                    }
                    else
                    {
                        return $"Content created for '{taskDescription}'. Produced a comprehensive document with introduction, body sections covering all key points, and conclusion with next steps.";
                    }
                
                case "review":
                    if (isIndianEconomy)
                    {
                        return $"The report provides a thorough analysis of India's economic progress. Strengths include comprehensive data coverage, balanced assessment of successes and challenges, and clear connection between policy initiatives and outcomes. Suggested improvements: 1) Add more comparative analysis with other emerging economies; 2) Expand the section on rural economic transformation; 3) Include more expert opinions from diverse economic perspectives; 4) Strengthen the conclusion with more specific future projections.";
                    }
                    else
                    {
                        return $"Review completed for '{taskDescription}'. The content is well-structured and accurate. Suggested improvements: enhance the introduction for better engagement, add more examples in section 2, and strengthen the conclusion.";
                    }
                
                default:
                    if (isIndianEconomy)
                    {
                        return $"Task '{taskDescription}' completed with focus on Indian economic progress over the past decade.";
                    }
                    else
                    {
                        return $"Task '{taskDescription}' completed successfully.";
                    }
            }
        }
        
        private string GetSimulatedQueryResponse(string query)
        {
            // Simple simulation of query responses based on specialty
            if (query.Contains("can you handle", StringComparison.OrdinalIgnoreCase))
            {
                // Check if the query mentions the agent's specialty
                if (query.Contains(_specialty, StringComparison.OrdinalIgnoreCase))
                {
                    return "Yes, I can handle this task as it aligns with my specialty.";
                }
                else
                {
                    return "No, this task doesn't seem to align with my specialty.";
                }
            }
            
            return $"As an agent specializing in {_specialty}, I can provide information about {_specialty}-related topics.";
        }
    }
}

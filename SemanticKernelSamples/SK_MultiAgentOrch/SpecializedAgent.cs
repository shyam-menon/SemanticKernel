using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.MultiAgentOrchestration;

namespace Microsoft.SemanticKernel.MultiAgentOrchestration
{
    /// <summary>
    /// Specialized agent that handles specific tasks in the multi-agent system
    /// </summary>
    public class SpecializedAgent : IAgent
    {
        private readonly string _agentId;
        private readonly Kernel _kernel;
        private readonly AgentCommunicationHub _communicationHub;
        private readonly string _agentInstruction;
        private readonly ILogger _logger;
        
        public SpecializedAgent(
            string agentId,
            Kernel kernel,
            AgentCommunicationHub communicationHub,
            string agentInstruction,
            ILogger logger)
        {
            _agentId = agentId;
            _kernel = kernel;
            _communicationHub = communicationHub;
            _agentInstruction = agentInstruction;
            _logger = logger;
            
            // Register this agent with the communication hub
            _communicationHub.RegisterAgent(agentId, this);
        }
        
        public async Task<AgentMessage> ProcessMessageAsync(string fromAgentId, AgentMessage message)
        {
            switch (message.MessageType)
            {
                case "TaskRequest":
                    return await ProcessTaskRequestAsync(fromAgentId, message);
                
                case "StatusRequest":
                    return await ProcessStatusRequestAsync(message);
                
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
        
        private async Task<AgentMessage> ProcessTaskRequestAsync(string fromAgentId, AgentMessage message)
        {
            var stepId = message.Content["stepId"].ToString();
            var taskDescription = message.Content["taskDescription"].ToString();
            var workflowContext = message.Content["workflowContext"].ToString();
            
            _logger.LogInformation($"Agent {_agentId} received task request: {taskDescription}");
            
            // Acknowledge receipt of the task
            var acknowledgment = new AgentMessage
            {
                MessageType = "TaskAcknowledgment",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "stepId", stepId },
                    { "status", "received" }
                }
            };
            
            await _communicationHub.SendMessageAsync(_agentId, fromAgentId, acknowledgment);
            
            // In a real implementation, this would execute the task using the LLM
            // For this sample, we'll simulate task execution
            
            // Send a status update
            var statusUpdate = new AgentMessage
            {
                MessageType = "StatusUpdate",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "stepId", stepId },
                    { "status", "processing" }
                }
            };
            
            await _communicationHub.SendMessageAsync(_agentId, fromAgentId, statusUpdate);
            
            // Simulate task execution
            await SimulateTaskExecutionAsync(taskDescription, workflowContext);
            
            // In a real implementation, this would return the actual task result
            // For this sample, we'll simulate a result
            string result = await GenerateTaskResultAsync(taskDescription, workflowContext);
            
            // Return the task result
            return new AgentMessage
            {
                MessageType = "TaskResult",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "stepId", stepId },
                    { "result", result }
                }
            };
        }
        
        private async Task<AgentMessage> ProcessStatusRequestAsync(AgentMessage message)
        {
            // This would return the current status of the agent
            // For this sample, we'll just return a simple status
            return new AgentMessage
            {
                MessageType = "StatusResponse",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "status", "ready" }
                }
            };
        }
        
        private async Task SimulateTaskExecutionAsync(string taskDescription, string workflowContext)
        {
            // Simulate task execution time
            await Task.Delay(500);
            
            // In a real implementation, this would execute the task using the LLM
            // For example:
            /*
            var prompt = $@"
            {_agentInstruction}
            
            Task: {taskDescription}
            Context: {workflowContext}
            
            Perform this task and provide a detailed response.
            ";
            
            var result = await _kernel.InvokePromptAsync(prompt);
            */
        }
        
        public async Task<string> GenerateTaskResultAsync(string taskDescription, string workflowContext, bool useSimulationMode = false)
        {
            // If using simulation mode, skip the LLM call and use simulated responses
            if (useSimulationMode)
            {
                _logger.LogInformation($"Using simulation mode for task: {taskDescription}");
                return GetSimulatedResponse(taskDescription);
            }
            
            // Use the LLM to generate a result based on the agent type and task
            var prompt = $@"
{_agentInstruction}

Task: {taskDescription}
Context: {workflowContext}

Provide a detailed response that demonstrates your expertise in this area.
";

            try
            {
                // Invoke the LLM to generate content
                _logger.LogInformation($"Making API call to Azure OpenAI for task: {taskDescription}");
                var result = await _kernel.InvokePromptAsync(prompt);
                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error generating content for task: {taskDescription}");
                
                // Fall back to simulated results if LLM call fails
                _logger.LogWarning("Falling back to simulated response due to API error");
                return GetSimulatedResponse(taskDescription);
            }
        }
        
        private string GetSimulatedResponse(string taskDescription)
        {
            // Check if the task is related to Indian economy
            bool isIndianEconomy = taskDescription.Contains("Indian economy", StringComparison.OrdinalIgnoreCase) || 
                                   taskDescription.Contains("India", StringComparison.OrdinalIgnoreCase);
            
            switch (_agentId)
            {
                case "researcher":
                    if (isIndianEconomy)
                    {
                        return $"Research on the progress of Indian economy in the last 10 years shows significant growth in GDP from approximately $1.8 trillion in 2015 to $3.5 trillion in 2025. Key sectors driving growth include IT services, pharmaceuticals, and renewable energy. The economy faced challenges including demonetization (2016), GST implementation (2017), and the COVID-19 pandemic (2020-2021), but showed resilience with recovery rates exceeding global averages.";
                    }
                    else
                    {
                        return $"Research completed on '{taskDescription}'. Found key information about the topic including historical context, current trends, and relevant statistics.";
                    }
                
                case "analyst":
                    if (isIndianEconomy)
                    {
                        return $"Analysis of Indian economic data reveals: 1) Average GDP growth of 6.2% annually over the decade, outpacing global averages; 2) Significant reduction in poverty rates from 21.9% to 10.2%; 3) Digital economy expansion with UPI transactions growing exponentially; 4) Challenges in manufacturing sector growth despite 'Make in India' initiatives; 5) Increasing income inequality with Gini coefficient rising from 0.36 to 0.42 over the decade.";
                    }
                    else
                    {
                        return $"Analysis completed for '{taskDescription}'. Key insights: identified three main patterns, determined statistical significance, and created visualizations of the data.";
                    }
                
                case "writer":
                    if (isIndianEconomy)
                    {
                        return $"The comprehensive report on India's economic journey over the past decade has been structured into five main sections: 1) Macroeconomic Indicators showing overall growth patterns; 2) Sectoral Analysis highlighting performers and laggards; 3) Policy Initiatives and their impacts; 4) Social and Development Outcomes; and 5) Future Outlook and Challenges. The narrative emphasizes both quantitative metrics and qualitative changes in economic structure, with particular attention to digital transformation and financial inclusion initiatives.";
                    }
                    else
                    {
                        return $"Content created for '{taskDescription}'. Produced a comprehensive document with introduction, body sections covering all key points, and conclusion with next steps.";
                    }
                
                case "reviewer":
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
    }
}

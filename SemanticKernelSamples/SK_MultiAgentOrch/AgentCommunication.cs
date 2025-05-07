using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Microsoft.SemanticKernel.MultiAgentOrchestration
{
    /// <summary>
    /// Handles communication between agents in a multi-agent system
    /// </summary>
    public class AgentCommunicationHub
    {
        private readonly Dictionary<string, IAgent> _registeredAgents;
        private readonly ILogger _logger;
        
        public AgentCommunicationHub(ILogger logger)
        {
            _registeredAgents = new Dictionary<string, IAgent>();
            _logger = logger;
        }
        
        public void RegisterAgent(string agentId, IAgent agent)
        {
            if (_registeredAgents.ContainsKey(agentId))
            {
                throw new ArgumentException($"Agent with ID {agentId} is already registered.");
            }
            
            _registeredAgents[agentId] = agent;
            _logger.LogInformation($"Agent {agentId} registered successfully.");
        }
        
        public async Task<AgentMessage> SendMessageAsync(string fromAgentId, string toAgentId, AgentMessage message)
        {
            if (!_registeredAgents.ContainsKey(toAgentId))
            {
                throw new KeyNotFoundException($"Agent with ID {toAgentId} is not registered.");
            }
            
            _logger.LogInformation($"Message from {fromAgentId} to {toAgentId}: {message.MessageType}");
            
            // Record the message for auditing and monitoring
            await RecordMessageAsync(fromAgentId, toAgentId, message);
            
            // Deliver the message to the recipient agent
            var response = await _registeredAgents[toAgentId].ProcessMessageAsync(fromAgentId, message);
            
            // Record the response
            await RecordMessageAsync(toAgentId, fromAgentId, response);
            
            return response;
        }
        
        public async Task BroadcastMessageAsync(string fromAgentId, AgentMessage message, List<string>? excludeAgentIds = null)
        {
            excludeAgentIds ??= new List<string>();
            excludeAgentIds.Add(fromAgentId); // Don't send to self
            
            var tasks = new List<Task>();
            
            foreach (var agentId in _registeredAgents.Keys.Where(id => !excludeAgentIds.Contains(id)))
            {
                tasks.Add(SendMessageAsync(fromAgentId, agentId, message));
            }
            
            await Task.WhenAll(tasks);
        }
        
        private async Task RecordMessageAsync(string fromAgentId, string toAgentId, AgentMessage message)
        {
            // Log detailed information about the message exchange
            _logger.LogDebug($"===== MESSAGE EXCHANGE =====\n" +
                           $"Timestamp: {DateTime.UtcNow}\n" +
                           $"From Agent: {fromAgentId}\n" +
                           $"To Agent: {toAgentId}\n" +
                           $"Message Type: {message.MessageType}\n" +
                           $"Conversation ID: {message.ConversationId}\n" +
                           $"Message ID: {message.MessageId}\n" +
                           $"Content: {JsonSerializer.Serialize(message.Content, new JsonSerializerOptions { WriteIndented = true })}\n" +
                           $"============================");
            
            // In a real implementation, this would store messages in a database or other persistent storage
            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Interface for all agents in the multi-agent system
    /// </summary>
    public interface IAgent
    {
        Task<AgentMessage> ProcessMessageAsync(string fromAgentId, AgentMessage message);
    }

    /// <summary>
    /// Represents a message exchanged between agents
    /// </summary>
    public class AgentMessage
    {
        public string MessageId { get; set; } = Guid.NewGuid().ToString();
        public string MessageType { get; set; } = string.Empty;
        public string ConversationId { get; set; } = string.Empty;
        public Dictionary<string, object> Content { get; set; } = new Dictionary<string, object>();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}

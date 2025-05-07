#### Decentralized Coordination

In this pattern, agents coordinate directly with each other:

```csharp
public class PeerAgent : IAgent
{
    private readonly string _agentId;
    private readonly string _specialty;
    private readonly IKernel _kernel;
    private readonly AgentCommunicationHub _communicationHub;
    private readonly HashSet<string> _knownAgentIds;
    private readonly ILogger _logger;
    
    public PeerAgent(
        string agentId,
        string specialty,
        IKernel kernel,
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
        var capabilities = (List<string>)message.Content["capabilities"];
        
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
        
        _logger.LogInformation($"Agent {_agentId} received task request from {fromAgentId}: {taskDescription}");
        
        // Check if this task aligns with the agent's specialty
        var isRelevant = await IsTaskRelevantToSpecialtyAsync(taskDescription);
        
        if (!isRelevant)
        {
            // If not relevant, try to delegate to a more appropriate agent
            var delegationResult = await TryDelegateTaskAsync(taskDescription, message.ConversationId);
            
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
        }
        
        // Process the task
        var result = await ProcessTaskAsync(taskDescription);
        
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
        var response = await GenerateQueryResponseAsync(query);
        
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
    
    private async Task<bool> IsTaskRelevantToSpecialtyAsync(string taskDescription)
    {
        // Determine if the task is relevant to this agent's specialty
        var relevancePrompt = $@"
Determine if the following task is relevant to an agent with specialty in {_specialty}:

Task: "{taskDescription}"

Answer with just 'yes' or 'no'.
";
        
        var relevanceResponse = await _kernel.InvokePromptAsync(relevancePrompt);
        var response = relevanceResponse.ToString().Trim().ToLower();
        
        return response == "yes";
    }
    
    private async Task<(bool Success, string DelegatedToAgentId)> TryDelegateTaskAsync(string taskDescription, string conversationId)
    {
        // Try to find a more appropriate agent for the task
        if (_knownAgentIds.Count == 0)
        {
            return (false, null);
        }
        
        // In a real implementation, this would use more sophisticated logic
        // to select the most appropriate agent based on task requirements
        
        // For this example, we'll just query each known agent
        foreach (var agentId in _knownAgentIds)
        {
            var queryMessage = new AgentMessage
            {
                MessageType = "QueryRequest",
                ConversationId = conversationId,
                Content = new Dictionary<string, object>
                {
                    { "query", $"Can you handle a task related to: {taskDescription}?" }
                }
            };
            
            var response = await _communicationHub.SendMessageAsync(_agentId, agentId, queryMessage);
            
            if (response.MessageType == "QueryResponse")
            {
                var responseText = response.Content["response"].ToString();
                
                if (responseText.Contains("yes") || responseText.Contains("can handle"))
                {
                    // Delegate the task to this agent
                    var taskMessage = new AgentMessage
                    {
                        MessageType = "TaskRequest",
                        ConversationId = conversationId,
                        Content = new Dictionary<string, object>
                        {
                            { "taskDescription", taskDescription },
                            { "delegatedFrom", _agentId }
                        }
                    };
                    
                    await _communicationHub.SendMessageAsync(_agentId, agentId, taskMessage);
                    
                    return (true, agentId);
                }
            }
        }
        
        return (false, null);
    }
    
    private async Task<string> ProcessTaskAsync(string taskDescription)
    {
        // Process the task based on the agent's specialty
        var taskPrompt = $@"
As an AI agent specializing in {_specialty}, complete the following task:

Task: "{taskDescription}"

Provide a detailed response.
";
        
        var taskResponse = await _kernel.InvokePromptAsync(taskPrompt);
        return taskResponse.ToString();
    }
    
    private async Task<string> GenerateQueryResponseAsync(string query)
    {
        // Generate a response to a query based on the agent's specialty
        var queryPrompt = $@"
As an AI agent specializing in {_specialty}, answer the following query:

Query: "{query}"

Provide a concise response.
";
        
        var queryResponse = await _kernel.InvokePromptAsync(queryPrompt);
        return queryResponse.ToString();
    }
}
```

### Visibility and Monitoring of Multi-Agent Systems

Monitoring multi-agent interactions is crucial for enterprise systems:

```csharp
public class MultiAgentMonitor
{
    private readonly ILogger _logger;
    private readonly string _connectionString;
    
    public MultiAgentMonitor(ILogger logger, string connectionString)
    {
        _logger = logger;
        _connectionString = connectionString;
    }
    
    public async Task RecordAgentInteractionAsync(AgentInteraction interaction)
    {
        try
        {
            // Log the interaction
            _logger.LogInformation(
                "Agent interaction: {FromAgent} -> {ToAgent}, Type: {MessageType}, Conversation: {ConversationId}",
                interaction.FromAgentId,
                interaction.ToAgentId,
                interaction.MessageType,
                interaction.ConversationId);
            
            // Store the interaction in the database
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO AgentInteractions (
                    InteractionId,
                    FromAgentId,
                    ToAgentId,
                    MessageType,
                    ConversationId,
                    Content,
                    Timestamp
                ) VALUES (
                    @interactionId,
                    @fromAgentId,
                    @toAgentId,
                    @messageType,
                    @conversationId,
                    @content,
                    @timestamp
                )";
            
            command.Parameters.AddWithValue("@interactionId", interaction.InteractionId);
            command.Parameters.AddWithValue("@fromAgentId", interaction.FromAgentId);
            command.Parameters.AddWithValue("@toAgentId", interaction.ToAgentId);
            command.Parameters.AddWithValue("@messageType", interaction.MessageType);
            command.Parameters.AddWithValue("@conversationId", interaction.ConversationId);
            command.Parameters.AddWithValue("@content", JsonSerializer.Serialize(interaction.Content));
            command.Parameters.AddWithValue("@timestamp", interaction.Timestamp);
            
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording agent interaction");
        }
    }
    
    public async Task<List<AgentInteraction>> GetConversationInteractionsAsync(string conversationId)
    {
        var interactions = new List<AgentInteraction>();
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    InteractionId,
                    FromAgentId,
                    ToAgentId,
                    MessageType,
                    ConversationId,
                    Content,
                    Timestamp
                FROM AgentInteractions
                WHERE ConversationId = @conversationId
                ORDER BY Timestamp";
            
            command.Parameters.AddWithValue("@conversationId", conversationId);
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                interactions.Add(new AgentInteraction
                {
                    InteractionId = reader.GetString(0),
                    FromAgentId = reader.GetString(1),
                    ToAgentId = reader.GetString(2),
                    MessageType = reader.GetString(3),
                    ConversationId = reader.GetString(4),
                    Content = JsonSerializer.Deserialize<Dictionary<string, object>>(reader.GetString(5)),
                    Timestamp = reader.GetDateTime(6)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving conversation interactions");
        }
        
        return interactions;
    }
    
    public async Task<ConversationSummary> GetConversationSummaryAsync(string conversationId)
    {
        var interactions = await GetConversationInteractionsAsync(conversationId);
        
        if (interactions.Count == 0)
        {
            return null;
        }
        
        var summary = new ConversationSummary
        {
            ConversationId = conversationId,
            StartTime = interactions.Min(i => i.Timestamp),
            EndTime = interactions.Max(i => i.Timestamp),
            Duration = interactions.Max(i => i.Timestamp) - interactions.Min(i => i.Timestamp),
            InteractionCount = interactions.Count,
            ParticipatingAgents = interactions.SelectMany(i => new[] { i.FromAgentId, i.ToAgentId }).Distinct().ToList(),
            MessageTypes = interactions.Select(i => i.MessageType).Distinct().ToList()
        };
        
        return summary;
    }
    
    public async Task<List<AgentPerformanceMetrics>> GetAgentPerformanceMetricsAsync(TimeSpan timeWindow)
    {
        var metrics = new List<AgentPerformanceMetrics>();
        var cutoffTime = DateTime.UtcNow - timeWindow;
        
        try
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            
            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT
                    AgentId,
                    COUNT(*) AS TotalInteractions,
                    SUM(CASE WHEN MessageType = 'TaskResult' THEN 1 ELSE 0 END) AS TasksCompleted,
                    AVG(ResponseTime) AS AvgResponseTime
                FROM (
                    SELECT
                        FromAgentId AS AgentId,
                        MessageType,
                        DATEDIFF(millisecond, 
                            (SELECT MIN(Timestamp) FROM AgentInteractions i2 
                             WHERE i2.ConversationId = i1.ConversationId 
                             AND i2.ToAgentId = i1.FromAgentId
                             AND i2.MessageType = 'TaskRequest'),
                            i1.Timestamp) AS ResponseTime
                    FROM AgentInteractions i1
                    WHERE MessageType = 'TaskResult'
                    AND Timestamp > @cutoffTime
                ) AS AgentStats
                GROUP BY AgentId";
            
            command.Parameters.AddWithValue("@cutoffTime", cutoffTime);
            
            using var reader = await command.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                metrics.Add(new AgentPerformanceMetrics
                {
                    AgentId = reader.GetString(0),
                    TotalInteractions = reader.GetInt32(1),
                    TasksCompleted = reader.GetInt32(2),
                    AverageResponseTimeMs = reader.IsDBNull(3) ? 0 : reader.GetDouble(3)
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving agent performance metrics");
        }
        
        return metrics;
    }
}

public class AgentInteraction
{
    public string InteractionId { get; set; } = Guid.NewGuid().ToString();
    public string FromAgentId { get; set; }
    public string ToAgentId { get; set; }
    public string MessageType { get; set; }
    public string ConversationId { get; set; }
    public Dictionary<string, object> Content { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ConversationSummary
{
    public string ConversationId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration { get; set; }
    public int InteractionCount { get; set; }
    public List<string> ParticipatingAgents { get; set; }
    public List<string> MessageTypes { get; set; }
}

public class AgentPerformanceMetrics
{
    public string AgentId { get; set; }
    public int TotalInteractions { get; set; }
    public int TasksCompleted { get; set; }
    public double AverageResponseTimeMs { get; set; }
}
```
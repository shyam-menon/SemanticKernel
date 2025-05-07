using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.MultiAgentOrchestration;

namespace Microsoft.SemanticKernel.MultiAgentOrchestration
{
    /// <summary>
    /// Orchestrator agent that coordinates the workflow between specialized agents
    /// </summary>
    public class OrchestratorAgent : IAgent
    {
        private readonly Kernel _kernel;
        private readonly AgentCommunicationHub _communicationHub;
        private readonly Dictionary<string, string> _agentSpecialties;
        private readonly ILogger _logger;
        private readonly Dictionary<string, WorkflowPlan> _activeWorkflows = new();
        
        public OrchestratorAgent(
            Kernel kernel,
            AgentCommunicationHub communicationHub,
            Dictionary<string, string> agentSpecialties,
            ILogger logger)
        {
            _kernel = kernel;
            _communicationHub = communicationHub;
            _agentSpecialties = agentSpecialties;
            _logger = logger;
            
            // Register this agent with the communication hub
            _communicationHub.RegisterAgent("orchestrator", this);
        }
        
        public async Task<AgentMessage> ProcessMessageAsync(string fromAgentId, AgentMessage message)
        {
            switch (message.MessageType)
            {
                case "TaskRequest":
                    return await ProcessTaskRequestAsync(message);
                
                case "TaskResult":
                    return await ProcessTaskResultAsync(fromAgentId, message);
                
                case "StatusUpdate":
                    return await ProcessStatusUpdateAsync(fromAgentId, message);
                
                default:
                    _logger.LogWarning($"Orchestrator received unknown message type: {message.MessageType}");
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
        
        public async Task<AgentMessage> ProcessUserRequestAsync(string userRequest, string conversationId)
        {
            // Analyze the user request to determine required agents and workflow
            var taskAnalysis = await AnalyzeTaskAsync(userRequest);
            
            // Create a workflow plan
            var workflowPlan = await CreateWorkflowPlanAsync(taskAnalysis, conversationId);
            
            // Store the workflow plan
            _activeWorkflows[conversationId] = workflowPlan;
            
            // Start executing the workflow
            return await ExecuteWorkflowAsync(workflowPlan);
        }
        
        private async Task<TaskAnalysis> AnalyzeTaskAsync(string userRequest)
        {
            // Use the LLM to analyze the task and determine required agents
            var analysisPrompt = $@"
Analyze the following user request:

""{userRequest}""

Determine which specialized agents should handle different aspects of this request.
Available agents and their specialties:
{string.Join("\n", _agentSpecialties.Select(kv => $"- {kv.Key}: {kv.Value}"))}

Provide a structured analysis including:
1. Primary task category
2. Subtasks that need to be performed
3. Agent assignments for each subtask
4. Dependencies between subtasks (which must be completed before others)

Return your response in a structured format that can be easily parsed.
";
            
            var analysisResponse = await _kernel.InvokePromptAsync(analysisPrompt);
            _logger.LogInformation($"Task analysis: {analysisResponse}");
            
            // For this sample, we'll create a simplified task analysis
            // In a real implementation, you would parse the LLM response
            return new TaskAnalysis
            {
                PrimaryTaskCategory = "Content Creation",
                Subtasks = new List<Subtask>
                {
                    new Subtask
                    {
                        Description = "Research the topic",
                        AssignedAgentId = "researcher",
                        Dependencies = new List<string>()
                    },
                    new Subtask
                    {
                        Description = "Analyze the research findings",
                        AssignedAgentId = "analyst",
                        Dependencies = new List<string> { "Research the topic" }
                    },
                    new Subtask
                    {
                        Description = "Create content based on research and analysis",
                        AssignedAgentId = "writer",
                        Dependencies = new List<string> { "Analyze the research findings" }
                    },
                    new Subtask
                    {
                        Description = "Review and improve the content",
                        AssignedAgentId = "reviewer",
                        Dependencies = new List<string> { "Create content based on research and analysis" }
                    }
                }
            };
        }
        
        private async Task<WorkflowPlan> CreateWorkflowPlanAsync(TaskAnalysis taskAnalysis, string conversationId)
        {
            // Create a workflow plan based on the task analysis
            var workflowPlan = new WorkflowPlan
            {
                ConversationId = conversationId,
                TaskAnalysis = taskAnalysis,
                Steps = new List<WorkflowStep>()
            };
            
            // Convert subtasks to workflow steps, respecting dependencies
            foreach (var subtask in taskAnalysis.Subtasks)
            {
                workflowPlan.Steps.Add(new WorkflowStep
                {
                    StepId = Guid.NewGuid().ToString(),
                    SubtaskDescription = subtask.Description,
                    AssignedAgentId = subtask.AssignedAgentId,
                    Dependencies = subtask.Dependencies,
                    Status = "Pending"
                });
            }
            
            return workflowPlan;
        }
        
        private async Task<AgentMessage> ExecuteWorkflowAsync(WorkflowPlan workflowPlan)
        {
            // Start executing steps that have no dependencies
            var initialSteps = workflowPlan.Steps.Where(s => !s.Dependencies.Any()).ToList();
            
            foreach (var step in initialSteps)
            {
                await ExecuteWorkflowStepAsync(step, workflowPlan);
            }
            
            // Return initial acknowledgment
            return new AgentMessage
            {
                MessageType = "WorkflowStarted",
                ConversationId = workflowPlan.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "workflowId", workflowPlan.ConversationId },
                    { "initialStepsCount", initialSteps.Count }
                }
            };
        }
        
        private async Task ExecuteWorkflowStepAsync(WorkflowStep step, WorkflowPlan workflowPlan)
        {
            // Update step status
            step.Status = "InProgress";
            
            // Create a task message for the assigned agent
            var taskMessage = new AgentMessage
            {
                MessageType = "TaskRequest",
                ConversationId = workflowPlan.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "stepId", step.StepId },
                    { "taskDescription", step.SubtaskDescription },
                    { "workflowContext", workflowPlan.TaskAnalysis.PrimaryTaskCategory }
                }
            };
            
            // Send the task to the assigned agent
            await _communicationHub.SendMessageAsync("orchestrator", step.AssignedAgentId, taskMessage);
        }
        
        private async Task<AgentMessage> ProcessTaskRequestAsync(AgentMessage message)
        {
            // This would handle task requests from other agents or external systems
            // For this example, we'll just acknowledge receipt
            return new AgentMessage
            {
                MessageType = "Acknowledgment",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "status", "received" }
                }
            };
        }
        
        private async Task<AgentMessage> ProcessTaskResultAsync(string fromAgentId, AgentMessage message)
        {
            // Process a task result from an agent
            var stepId = message.Content["stepId"].ToString();
            var result = message.Content["result"].ToString();
            var workflowId = message.ConversationId;
            
            _logger.LogInformation($"Received task result from {fromAgentId} for step {stepId}: {result}");
            
            // Check if we have this workflow
            if (!_activeWorkflows.TryGetValue(workflowId, out var workflowPlan))
            {
                _logger.LogWarning($"Received result for unknown workflow: {workflowId}");
                return new AgentMessage
                {
                    MessageType = "Error",
                    ConversationId = workflowId,
                    Content = new Dictionary<string, object>
                    {
                        { "error", "Unknown workflow" }
                    }
                };
            }
            
            // Find the step
            var step = workflowPlan.Steps.FirstOrDefault(s => s.StepId == stepId);
            if (step == null)
            {
                // If we don't have the exact step ID (which might happen in our simulation),
                // find a step assigned to this agent
                step = workflowPlan.Steps.FirstOrDefault(s => s.AssignedAgentId == fromAgentId);
                
                if (step == null)
                {
                    _logger.LogWarning($"Received result for unknown step: {stepId}");
                    return new AgentMessage
                    {
                        MessageType = "Error",
                        ConversationId = workflowId,
                        Content = new Dictionary<string, object>
                        {
                            { "error", "Unknown step" }
                        }
                    };
                }
            }
            
            // Update step status and result
            step.Status = "Completed";
            step.Result = result;
            
            // Check if there are any dependent steps that can now be started
            var dependentSteps = workflowPlan.Steps
                .Where(s => s.Dependencies.Contains(step.SubtaskDescription) && s.Status == "Pending")
                .ToList();
            
            foreach (var dependentStep in dependentSteps)
            {
                // Check if all dependencies are completed
                var allDependenciesMet = true;
                foreach (var dependency in dependentStep.Dependencies)
                {
                    var dependencyStep = workflowPlan.Steps.FirstOrDefault(s => s.SubtaskDescription == dependency);
                    if (dependencyStep == null || dependencyStep.Status != "Completed")
                    {
                        allDependenciesMet = false;
                        break;
                    }
                }
                
                if (allDependenciesMet)
                {
                    await ExecuteWorkflowStepAsync(dependentStep, workflowPlan);
                }
            }
            
            // Check if all steps are completed
            if (workflowPlan.Steps.All(s => s.Status == "Completed"))
            {
                await FinalizeWorkflowAsync(workflowPlan);
            }
            
            return new AgentMessage
            {
                MessageType = "ResultAcknowledgment",
                ConversationId = workflowId,
                Content = new Dictionary<string, object>
                {
                    { "status", "processed" }
                }
            };
        }
        
        private async Task<AgentMessage> ProcessStatusUpdateAsync(string fromAgentId, AgentMessage message)
        {
            // Process a status update from an agent
            _logger.LogInformation($"Status update from {fromAgentId}: {message.Content["status"]}");
            
            return new AgentMessage
            {
                MessageType = "StatusAcknowledgment",
                ConversationId = message.ConversationId,
                Content = new Dictionary<string, object>
                {
                    { "received", true }
                }
            };
        }
        
        private async Task FinalizeWorkflowAsync(WorkflowPlan workflowPlan)
        {
            _logger.LogInformation($"Workflow {workflowPlan.ConversationId} completed");
            
            // In a real implementation, this would compile the results and generate a final output
            // For this sample, we'll just log the completion
            
            // Remove the workflow from active workflows
            _activeWorkflows.Remove(workflowPlan.ConversationId);
            
            await Task.CompletedTask;
        }
    }

    public class TaskAnalysis
    {
        public string PrimaryTaskCategory { get; set; } = string.Empty;
        public List<Subtask> Subtasks { get; set; } = new List<Subtask>();
    }

    public class Subtask
    {
        public string Description { get; set; } = string.Empty;
        public string AssignedAgentId { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
    }

    public class WorkflowPlan
    {
        public string ConversationId { get; set; } = string.Empty;
        public TaskAnalysis TaskAnalysis { get; set; } = new TaskAnalysis();
        public List<WorkflowStep> Steps { get; set; } = new List<WorkflowStep>();
    }

    public class WorkflowStep
    {
        public string StepId { get; set; } = string.Empty;
        public string SubtaskDescription { get; set; } = string.Empty;
        public string AssignedAgentId { get; set; } = string.Empty;
        public List<string> Dependencies { get; set; } = new List<string>();
        public string Status { get; set; } = string.Empty;
        public string? Result { get; set; }
    }
}

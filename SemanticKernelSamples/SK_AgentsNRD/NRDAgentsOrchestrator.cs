using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using SK_AgentsNRD.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel.ChatCompletion;

namespace SK_AgentsNRD
{
    /// <summary>
    /// Main orchestrator for NRD (Non-Reporting Device) issue resolution agents
    /// </summary>
    public class NRDAgentsOrchestrator
    {
        private readonly Kernel _kernel;
        private readonly ILogger<NRDAgentsOrchestrator> _logger;
        private readonly List<ChatMessageContent> _chatHistory;
        private bool _isComplete;
        private int _maxTurns;
        private bool _simulationMode;

        // Agent names
        private const string MonitoringAgent = "MonitoringAgent";
        private const string DiagnosticAgent = "DiagnosticAgent";
        private const string RemediationAgent = "RemediationAgent";
        private const string KnowledgebaseAgent = "KnowledgebaseAgent";

        /// <summary>
        /// Constructor for NRDAgentsOrchestrator
        /// </summary>
        /// <param name="endpoint">Azure OpenAI endpoint</param>
        /// <param name="apiKey">Azure OpenAI API key</param>
        /// <param name="deploymentName">Azure OpenAI deployment name</param>
        /// <param name="logger">Logger instance</param>
        /// <param name="simulationMode">Whether to run in simulation mode</param>
        public NRDAgentsOrchestrator(string endpoint, string apiKey, string deploymentName, ILogger<NRDAgentsOrchestrator> logger, bool simulationMode = false)
        {
            _logger = logger;
            _chatHistory = new List<ChatMessageContent>();
            _isComplete = false;
            _maxTurns = 10;
            _simulationMode = simulationMode;

            // Setup logging services
            var serviceProvider = new ServiceCollection()
                .AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Information);
                })
                .BuildServiceProvider();

            // Get loggers for plugins
            var jamcPluginLogger = serviceProvider.GetRequiredService<ILogger<JamCPlugin>>();
            var splunkPluginLogger = serviceProvider.GetRequiredService<ILogger<SplunkPlugin>>();
            var credentialPluginLogger = serviceProvider.GetRequiredService<ILogger<CredentialPlugin>>();

            // Initialize kernel with Azure OpenAI
            var builder = Kernel.CreateBuilder()
                .AddAzureOpenAIChatCompletion(deploymentName, endpoint, apiKey);

            // Add plugins with loggers
            builder.Plugins.AddFromObject(new JamCPlugin(jamcPluginLogger), "JamCPlugin");
            builder.Plugins.AddFromObject(new SplunkPlugin(splunkPluginLogger), "SplunkPlugin");
            builder.Plugins.AddFromObject(new CredentialPlugin(credentialPluginLogger), "CredentialPlugin");
            
            _kernel = builder.Build();
        }

        /// <summary>
        /// Determines if the conversation should terminate in simulation mode
        /// </summary>
        private bool ShouldTerminateSimulated(string response)
        {
            // Simple rule-based termination decision for simulation mode
            if (string.IsNullOrEmpty(response) || response.Length < 10)
            {
                return false;
            }
            
            if (response.Contains("ISSUE RESOLVED"))
            {
                return true;
            }
            
            if (response.Contains("RESOLUTION SUMMARY:"))
            {
                return true;
            }
            
            // Check for KnowledgebaseAgent's final response
            if (response.Contains("Best Practices:") && 
                (response.Contains("implement") || response.Contains("Implement")))
            {
                return true;
            }
            
            // Default to not terminating
            return false;
        }

        /// <summary>
        /// Handles an NRD issue for a specific device
        /// </summary>
        /// <param name="deviceId">ID of the device to investigate</param>
        /// <param name="forceSimulation">Force simulation mode regardless of the orchestrator setting</param>
        /// <returns>True if the issue was resolved, false otherwise</returns>
        public async Task<bool> HandleNRDIssue(string deviceId, bool forceSimulation = false)
        {
            bool originalSimulationMode = _simulationMode;
            
            try
            {
                // Set simulation mode if forced
                if (forceSimulation)
                {
                    _simulationMode = true;
                }
                
                // Reset the chat history
                _chatHistory.Clear();
                _isComplete = false;
                Console.WriteLine("Chat session has been reset.\n");
                
                if (_simulationMode)
                {
                    Console.WriteLine("[Running in simulation mode - no API calls will be made]\n");
                }

                // Start with the monitoring agent
                string currentAgent = MonitoringAgent;
                int turn = 1;
                int consecutiveErrors = 0;
                const int maxConsecutiveErrors = 2;

                while (!_isComplete && turn <= _maxTurns)
                {
                    try
                    {
                        Console.WriteLine($"--- Turn {turn} ---");
                        Console.WriteLine($"{currentAgent}:");

                        // Get response from the current agent
                        string response = await GetAgentResponseAsync(currentAgent, deviceId);
                        Console.WriteLine(response);

                        // Add the response to the chat history with agent metadata
                        // Create a dictionary for metadata
                        var metadata = new Dictionary<string, object?> { { "agent", currentAgent } };
                        
                        // Create a new message with the metadata
                        var message = new ChatMessageContent(
                            AuthorRole.Assistant,
                            response,
                            metadata: metadata
                        );
                        
                        _chatHistory.Add(message);

                        // Check if we should terminate
                        _isComplete = await ShouldTerminateAsync(response);

                        if (!_isComplete)
                        {
                            // Determine the next agent
                            currentAgent = await DetermineNextAgentAsync(response);
                            _logger.LogInformation($"Next agent: {currentAgent}");
                            Console.WriteLine($"Next agent: {currentAgent}");
                        }
                        
                        // Reset consecutive errors counter on success
                        consecutiveErrors = 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error during turn {turn}: {ex.Message}");
                        Console.WriteLine($"Error during turn {turn}: {ex.Message}");
                        
                        consecutiveErrors++;
                        
                        // If we've had too many consecutive errors, switch to simulation mode
                        if (consecutiveErrors >= maxConsecutiveErrors && !_simulationMode)
                        {
                            _simulationMode = true;
                            _logger.LogWarning("Switching to simulation mode due to consecutive errors");
                            Console.WriteLine("\n[Switching to simulation mode due to consecutive errors]\n");
                        }
                        // If we're already in simulation mode and still getting errors, terminate
                        else if (_simulationMode && consecutiveErrors >= maxConsecutiveErrors)
                        {
                            _logger.LogError("Too many consecutive errors even in simulation mode. Terminating.");
                            Console.WriteLine("Too many consecutive errors. Terminating.");
                            return false;
                        }
                    }

                    turn++;
                }

                return _isComplete;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error during NRD resolution: {ex.Message}");
                Console.WriteLine($"Error during NRD resolution: {ex.Message}");
                return false;
            }
            finally
            {
                // Restore original simulation mode
                _simulationMode = originalSimulationMode;
            }
        }

        /// <summary>
        /// Gets a response from the specified agent
        /// </summary>
        private async Task<string> GetAgentResponseAsync(string agentName, string deviceId)
        {
            try
            {
                if (_simulationMode)
                {
                    return GetSimulatedResponse(agentName, deviceId);
                }

                var instructions = GetAgentInstructions(agentName);
                var systemMessage = new ChatMessageContent(AuthorRole.System, instructions);

                // Create chat history for the agent including the system message
                var agentChatHistory = new List<ChatMessageContent> { systemMessage };
                agentChatHistory.AddRange(_chatHistory);

                // Create arguments with the device ID
                var arguments = new KernelArguments
                {
                    ["deviceId"] = deviceId
                };

                // Create the prompt template from the chat history
                string prompt = string.Join("\n", agentChatHistory.Select(msg => 
                    $"{msg.Role}: {msg.Content}"));

                // Get response from the agent
                var result = await _kernel.InvokePromptAsync(prompt, arguments);
                return result.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting response from {agentName}: {ex.Message}");
                
                // If we get an error and we're not in simulation mode, switch to simulation mode for this response
                if (!_simulationMode)
                {
                    _logger.LogWarning("Switching to simulation mode due to API error");
                    return GetSimulatedResponse(agentName, deviceId);
                }
                
                throw;
            }
        }

        /// <summary>
        /// Gets instructions for the specified agent
        /// </summary>
        private string GetAgentInstructions(string agentName)
        {
            return agentName switch
            {
                MonitoringAgent => """
                    You are a device monitoring specialist. Your responsibility is to detect NRD (Non-Reporting Device) events.
                    
                    Follow these steps in order:
                    1. Check if a device has not reported data for the past three days using GetDeviceLogs
                    2. Verify the data collection status using VerifyDataCollection
                    3. If no data collection is found, mark the event as needing attention
                    
                    Format your response EXACTLY like this:
                    MONITORING FINDINGS:
                    1. Device ID: [Device ID]
                    2. Last Report Time: [Time]
                    3. Data Collection Status: [Status]
                    4. Attention Required: [Yes/No]
                    
                    Do not attempt to diagnose or fix issues - only report them.
                    """,
                
                DiagnosticAgent => """
                    You are a device diagnostic specialist. Your responsibility is to diagnose NRD (Non-Reporting Device) issues.
                    
                    Follow these steps in order:
                    1. Review the monitoring findings
                    2. Check device status using GetDeviceStatus
                    3. Check credential status using CheckCredentials and GetCredentialStatus
                    4. Identify the root cause of the data collection issue
                    
                    Format your response EXACTLY like this:
                    DIAGNOSTIC FINDINGS:
                    1. Root Cause: [Description of the root cause]
                    2. Credential Status: [Status]
                    3. Recommended Action: [Action]
                    
                    Do not attempt to fix issues - only diagnose them.
                    """,
                
                RemediationAgent => """
                    You are a device remediation specialist. Your responsibility is to fix NRD (Non-Reporting Device) issues.
                    
                    Follow these steps in order:
                    1. Review the diagnostic findings
                    2. If credential issues are found:
                       - Generate new credentials using GenerateCredentials
                       - Validate the new credentials using ValidateCredentials
                    3. Perform a manual data collection using PerformManualCollection
                    4. Verify the fix was successful
                    
                    Format your response EXACTLY like this:
                    REMEDIATION COMPLETED:
                    1. Actions Taken: [Description of actions]
                    2. Verification Result: [Success/Failed]
                    3. Current Status: [Status]
                    
                    If verification fails, indicate that additional diagnosis is needed.
                    """,
                
                KnowledgebaseAgent => """
                    You are a knowledge specialist for device management. Your responsibility is to document the resolution process and provide best practices.
                    
                    Follow these steps:
                    1. Review the entire conversation
                    2. Summarize the issue and resolution
                    3. Provide best practices to prevent similar issues
                    4. Document the steps taken for future reference
                    
                    Format your response EXACTLY like this:
                    RESOLUTION SUMMARY:
                    1. Issue: [Brief description of the issue]
                    2. Root Cause: [Root cause identified]
                    3. Resolution: [Steps taken to resolve]
                    4. Best Practices: [Recommendations to prevent recurrence]
                    
                    End your response with "ISSUE RESOLVED" if the issue was successfully fixed.
                    """,
                
                _ => throw new ArgumentException($"Unknown agent name: {agentName}")
            };
        }

        /// <summary>
        /// Gets a simulated response for the specified agent
        /// </summary>
        private string GetSimulatedResponse(string agentName, string deviceId)
        {
            _logger.LogInformation($"Generating simulated response for {agentName} for device {deviceId}");
            
            // Get device information from the plugins if available
            string credentialStatus = "Unknown";
            string lastReportTime = DateTime.Now.AddDays(-4).ToString();
            bool isReporting = false;
            
            try
            {
                // Try to get actual device information from the plugins
                var jamcLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Plugins.JamCPlugin>();
                var splunkLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Plugins.SplunkPlugin>();
                var credentialLogger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<Plugins.CredentialPlugin>();
                
                var jamcPlugin = new Plugins.JamCPlugin(jamcLogger);
                var splunkPlugin = new Plugins.SplunkPlugin(splunkLogger);
                var credentialPlugin = new Plugins.CredentialPlugin(credentialLogger);
                
                var deviceStatus = jamcPlugin.GetDeviceStatus(deviceId);
                credentialStatus = jamcPlugin.CheckCredentials(deviceId);
                var logs = splunkPlugin.GetDeviceLogs(deviceId);
                isReporting = splunkPlugin.VerifyDataCollection(deviceId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error getting device information for simulation: {ex.Message}");
            }
            
            switch (agentName)
            {
                case MonitoringAgent:
                    return $@"MONITORING FINDINGS:
1. Device ID: {deviceId}
2. Last Report Time: {lastReportTime}
3. Data Collection Status: {(isReporting ? "Active" : "Not Active")}
4. Attention Required: {(isReporting ? "No" : "Yes")}";
                    
                case DiagnosticAgent:
                    if (deviceId == "DEV001" || deviceId == "DEV002" || deviceId == "DEV003")
                    {
                        return $@"DIAGNOSTIC FINDINGS:
1. Root Cause: The device is not reporting data due to credential issues.
2. Credential Status: {credentialStatus}
3. Recommended Action: Generate new credentials for the device.";
                    }
                    else
                    {
                        return $@"DIAGNOSTIC FINDINGS:
1. Root Cause: The device is not reporting data due to network connectivity issues.
2. Credential Status: Valid
3. Recommended Action: Check and restore network connectivity.";
                    }
                    
                case RemediationAgent:
                    if (deviceId == "DEV001" || deviceId == "DEV002" || deviceId == "DEV003")
                    {
                        return $@"REMEDIATION COMPLETED:
1. Actions Taken: Generated new credentials for device {deviceId} and configured them on the device.
2. Verification Result: Success
3. Current Status: Device is now reporting data correctly.";
                    }
                    else
                    {
                        return $@"REMEDIATION COMPLETED:
1. Actions Taken: Restored network connectivity for device {deviceId}.
2. Verification Result: Success
3. Current Status: Device is now reporting data correctly.";
                    }
                    
                case KnowledgebaseAgent:
                    if (deviceId == "DEV001" || deviceId == "DEV002" || deviceId == "DEV003")
                    {
                        return $@"RESOLUTION SUMMARY:
1. Issue: Device {deviceId} was not reporting data (NRD)
2. Root Cause: Credential issues preventing data collection
3. Resolution: Generated new credentials and configured them on the device
4. Best Practices: Implement automated credential rotation and monitoring to prevent future issues

ISSUE RESOLVED";
                    }
                    else
                    {
                        return $@"RESOLUTION SUMMARY:
1. Issue: Device {deviceId} was not reporting data (NRD)
2. Root Cause: Network connectivity issues preventing data collection
3. Resolution: Restored network connectivity for the device
4. Best Practices: Implement network monitoring and automated recovery procedures

ISSUE RESOLVED";
                    }
                    
                default:
                    return $"Unknown agent: {agentName}";
            }
        }

        /// <summary>
        /// Determines the next agent based on the current response
        /// </summary>
        private async Task<string> DetermineNextAgentAsync(string response)
        {
            try
            {
                if (_simulationMode)
                {
                    return DetermineNextAgentSimulated(response);
                }
                
                // Create a prompt to determine the next agent
                var prompt = $"""
                    Examine the provided RESPONSE and choose the next participant.
                    State only the name of the chosen participant without explanation.
                    
                    Choose only from these participants:
                    - {MonitoringAgent}
                    - {DiagnosticAgent}
                    - {RemediationAgent}
                    - {KnowledgebaseAgent}
                    
                    Always follow these rules when choosing the next participant:
                    - If RESPONSE is user input, it is {MonitoringAgent}'s turn.
                    - If RESPONSE contains "MONITORING FINDINGS:" and "Attention Required: Yes", it is {DiagnosticAgent}'s turn.
                    - If RESPONSE contains "DIAGNOSTIC FINDINGS:" and mentions credential issues, it is {RemediationAgent}'s turn.
                    - If RESPONSE contains "REMEDIATION COMPLETED:" and "Verification Result: Failed", it is {DiagnosticAgent}'s turn.
                    - If RESPONSE contains "REMEDIATION COMPLETED:" and "Verification Result: Success", it is {KnowledgebaseAgent}'s turn.
                    
                    RESPONSE:
                    {response}
                    """;

                // Get the next agent from the kernel
                var arguments = new KernelArguments();
                var result = await _kernel.InvokePromptAsync(prompt, arguments);
                var nextAgent = result.ToString().Trim();

                // Validate the next agent
                if (nextAgent != MonitoringAgent && 
                    nextAgent != DiagnosticAgent && 
                    nextAgent != RemediationAgent && 
                    nextAgent != KnowledgebaseAgent)
                {
                    _logger.LogWarning($"Invalid next agent: {nextAgent}. Defaulting to {MonitoringAgent}");
                    return MonitoringAgent;
                }

                _logger.LogInformation($"Next agent: {nextAgent}");
                return nextAgent;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error determining next agent: {ex.Message}");
                
                // If we get an error, switch to simulation mode for this determination
                if (!_simulationMode)
                {
                    _logger.LogWarning("Switching to simulation mode for agent selection due to API error");
                    return DetermineNextAgentSimulated(response);
                }
                
                // Default to monitoring agent if all else fails
                return MonitoringAgent;
            }
        }

        /// <summary>
        /// Determines the next agent based on the current response in simulation mode
        /// </summary>
        private string DetermineNextAgentSimulated(string response)
        {
            // Simple rule-based agent selection for simulation mode
            if (string.IsNullOrEmpty(response) || response.Length < 10)
            {
                return MonitoringAgent;
            }
            
            if (response.Contains("MONITORING FINDINGS:") && response.Contains("Attention Required: Yes"))
            {
                return DiagnosticAgent;
            }
            
            if (response.Contains("DIAGNOSTIC FINDINGS:") && 
                (response.Contains("credential") || response.Contains("Credential")))
            {
                return RemediationAgent;
            }
            
            if (response.Contains("REMEDIATION COMPLETED:") && response.Contains("Verification Result: Failed"))
            {
                return DiagnosticAgent;
            }
            
            if (response.Contains("REMEDIATION COMPLETED:") && response.Contains("Verification Result: Success"))
            {
                return KnowledgebaseAgent;
            }
            
            // Default to monitoring agent
            return MonitoringAgent;
        }

        /// <summary>
        /// Determines if the conversation should terminate
        /// </summary>
        private async Task<bool> ShouldTerminateAsync(string response)
        {
            try
            {
                // Check if we've reached the maximum number of turns
                if (_chatHistory.Count >= _maxTurns)
                {
                    _logger.LogInformation("Terminating due to maximum turns reached");
                    return true;
                }

                if (_simulationMode)
                {
                    return ShouldTerminateSimulated(response);
                }

                // Create a prompt to determine if the conversation should terminate
                var prompt = $"""
                    Examine the provided RESPONSE and determine if the conversation should terminate.
                    Reply with only "terminate" or "continue" without explanation.
                    
                    Terminate if ANY of these conditions are met:
                    - The RESPONSE contains "ISSUE RESOLVED" or indicates the issue has been fixed
                    - The RESPONSE contains a final summary of the resolution
                    - The RESPONSE indicates there is nothing more to do
                    
                    Continue if ANY of these conditions are met:
                    - The RESPONSE requests more information
                    - The RESPONSE indicates further diagnosis is needed
                    - The RESPONSE indicates a remediation action is in progress
                    
                    RESPONSE:
                    {response}
                    """;

                // Get the termination decision from the kernel
                var arguments = new KernelArguments();
                var result = await _kernel.InvokePromptAsync(prompt, arguments);
                var decision = result.ToString().Trim().ToLower();

                _logger.LogInformation($"Termination decision: {decision}");
                return decision == "terminate";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error determining termination: {ex.Message}");
                
                // If we get an error, switch to simulation mode for this determination
                if (!_simulationMode)
                {
                    _logger.LogWarning("Switching to simulation mode for termination decision due to API error");
                    return ShouldTerminateSimulated(response);
                }
                
                // Default to not terminating if all else fails
                return false;
            }
        }

        /// <summary>
        /// Resets the chat session
        /// </summary>
        public void ResetChat()
        {
            _chatHistory.Clear();
            _isComplete = false;
            Console.WriteLine("Chat session has been reset.");
        }

        /// <summary>
        /// Runs test cases for NRD resolution
        /// </summary>
        public async Task RunTests()
        {
            Console.WriteLine("\n=== Running NRD Resolution Tests ===");

            var testCases = new[]
            {
                "DEV001", // Expired credentials
                "DEV002", // Corrupted credentials
                "DEV003", // Missing credentials
                "DEV004"  // Valid credentials but other issues
            };

            foreach (var deviceId in testCases)
            {
                Console.WriteLine($"\nTesting device: {deviceId}");
                await HandleNRDIssue(deviceId);
                await Task.Delay(2000); // Delay between tests
                ResetChat();
            }
        }
    }
}

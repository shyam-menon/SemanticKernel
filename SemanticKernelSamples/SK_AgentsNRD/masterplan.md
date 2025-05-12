# NRD Issue Resolution System Implementation Plan

## Overview

This document outlines the step-by-step plan for implementing a system to identify, diagnose, and resolve Non-Reporting Device (NRD) issues caused by device credential errors. The implementation will use Semantic Kernel agents with mock plugins to simulate communication with underlying systems.

## System Architecture

1. **Agent Types**:
   - Monitoring Agent: Detects NRD events and retrieves logs
   - Diagnostic Agent: Analyzes root causes of NRD issues
   - Remediation Agent: Executes recommended fixes
   - Knowledgebase Agent: Provides information and best practices

2. **Mock Services**:
   - JamC Management System: For device management and status checking
   - Splunk Logging Service: For log retrieval and analysis
   - Credential Management System: For handling device credentials

3. **Workflow**:
   - Sequential agent activation based on issue state
   - Structured message passing between agents
   - Clear termination criteria

## Implementation Steps

### Phase 1: Project Setup and Core Infrastructure

1. **Create Base Project Structure**:
   - Define necessary namespaces and classes
   - Set up logging infrastructure
   - Configure Azure OpenAI integration
   - Create agent group chat framework

2. **Implement State Management**:
   - Create `DeviceState` class to track device status
   - Implement state persistence between agent calls
   - Define state transitions for the NRD resolution workflow

3. **Define Communication Protocol**:
   - Establish message formats for inter-agent communication
   - Define trigger phrases for agent handoffs
   - Create termination criteria

### Phase 2: Mock Service Implementation

1. **Create JamC Plugin**:
   - Implement device status checking
   - Add credential validation functions
   - Create device enrollment simulation
   - Add credential update capabilities

2. **Create Splunk Plugin**:
   - Implement log retrieval functions
   - Add log analysis capabilities
   - Create data collection verification

3. **Create Credential Management Plugin**:
   - Implement credential generation
   - Add credential validation
   - Create credential rotation capabilities

### Phase 3: Agent Implementation

1. **Implement Monitoring Agent**:
   - Create agent with instructions for NRD event detection
   - Implement log retrieval functionality
   - Add logic to flag events needing attention
   - Define output format for diagnostic agent handoff

2. **Implement Diagnostic Agent**:
   - Create agent with instructions for root cause analysis
   - Implement credential error detection
   - Add recommendation generation
   - Define output format for remediation agent handoff

3. **Implement Remediation Agent**:
   - Create agent with instructions for executing fixes
   - Implement credential renewal process
   - Add verification of fixes
   - Define output format for completion

4. **Implement Knowledgebase Agent**:
   - Create agent with domain knowledge about NRD issues
   - Implement best practices recommendations
   - Add historical solution retrieval

### Phase 4: Orchestration and Integration

1. **Implement Agent Selection Strategy**:
   - Create logic to determine next agent based on message content
   - Implement agent routing based on issue state
   - Add fallback mechanisms

2. **Implement Termination Strategy**:
   - Define completion criteria
   - Implement verification of issue resolution
   - Create session summary generation

3. **Create Main Handler**:
   - Implement main entry point for NRD issue handling
   - Add session management
   - Implement error handling and logging

### Phase 5: Testing and Refinement

1. **Create Test Cases**:
   - Define scenarios for credential errors
   - Create test data for device states
   - Implement automated test suite

2. **Implement Simulation Mode**:
   - Add ability to run in simulation without real API calls
   - Create realistic mock responses
   - Implement configurable failure scenarios

3. **Add Logging and Reporting**:
   - Implement comprehensive logging
   - Create resolution reports
   - Add performance metrics

## Detailed Implementation Plan

### 1. Core Classes Implementation

```csharp
// DeviceState.cs - Tracks the state of a device
public class DeviceState
{
    public string DeviceId { get; set; }
    public bool IsReporting { get; set; }
    public DateTime LastReportTime { get; set; }
    public string CredentialStatus { get; set; }
    public string LastError { get; set; }
    public bool DataCollectionEnabled { get; set; }
}

// NRDAgentsOrchestrator.cs - Main orchestrator for the agents
public class NRDAgentsOrchestrator
{
    private readonly Kernel _kernel;
    private readonly AgentGroupChat _agentChat;
    private readonly ChatCompletionAgent _monitoringAgent;
    private readonly ChatCompletionAgent _diagnosticAgent;
    private readonly ChatCompletionAgent _remediationAgent;
    private readonly ChatCompletionAgent _knowledgebaseAgent;
    private readonly ILogger<NRDAgentsOrchestrator> _logger;
    
    // Constructor, agent creation methods, and handler methods will be implemented here
}
```

### 2. Plugin Implementation

```csharp
// JamCPlugin.cs - Mock plugin for JamC device management system
public class JamCPlugin
{
    private readonly ILogger<JamCPlugin> _logger;
    private Dictionary<string, DeviceState> _devices;
    
    // Methods for device management, credential checking, etc.
    [KernelFunction, Description("Gets the current status of a device")]
    public string GetDeviceStatus(string deviceId) { /* Implementation */ }
    
    [KernelFunction, Description("Checks device credential status")]
    public string CheckCredentials(string deviceId) { /* Implementation */ }
    
    [KernelFunction, Description("Updates device credentials")]
    public string UpdateCredentials(string deviceId) { /* Implementation */ }
}

// SplunkPlugin.cs - Mock plugin for Splunk log service
public class SplunkPlugin
{
    private readonly ILogger<SplunkPlugin> _logger;
    
    // Methods for log retrieval and analysis
    [KernelFunction, Description("Retrieves logs for a device")]
    public string GetDeviceLogs(string deviceId, int days) { /* Implementation */ }
    
    [KernelFunction, Description("Verifies data collection for a device")]
    public bool VerifyDataCollection(string deviceId) { /* Implementation */ }
}
```

### 3. Agent Implementation

```csharp
// Implementation in NRDAgentsOrchestrator.cs

private ChatCompletionAgent CreateMonitoringAgent()
{
    return new ChatCompletionAgent
    {
        Name = "MonitoringAgent",
        Instructions = """
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
        Kernel = _kernel,
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
    };
}

private ChatCompletionAgent CreateDiagnosticAgent()
{
    return new ChatCompletionAgent
    {
        Name = "DiagnosticAgent",
        Instructions = """
        You are a device diagnostic specialist. Your responsibility is to diagnose NRD (Non-Reporting Device) issues.
        
        Follow these steps in order:
        1. Review the monitoring findings
        2. Check device status using GetDeviceStatus
        3. Check credential status using CheckCredentials
        4. Identify the root cause of the data collection issue
        
        Format your response EXACTLY like this:
        DIAGNOSTIC FINDINGS:
        1. Root Cause: [Description of the root cause]
        2. Credential Status: [Status]
        3. Recommended Action: [Action]
        
        Do not attempt to fix issues - only diagnose them.
        """,
        Kernel = _kernel,
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
    };
}

private ChatCompletionAgent CreateRemediationAgent()
{
    return new ChatCompletionAgent
    {
        Name = "RemediationAgent",
        Instructions = """
        You are a device remediation specialist. Your responsibility is to fix NRD (Non-Reporting Device) issues.
        
        Follow these steps in order:
        1. Review the diagnostic findings
        2. Execute the recommended action using UpdateCredentials
        3. Verify the fix using VerifyDataCollection
        4. Document the steps taken
        
        Format your response EXACTLY like this:
        REMEDIATION COMPLETED:
        1. Actions Taken: [Description of actions]
        2. Verification Result: [Result]
        3. Current Status: [Status]
        
        If verification fails, indicate that additional diagnosis is needed.
        """,
        Kernel = _kernel,
        Arguments = new KernelArguments(new AzureOpenAIPromptExecutionSettings()
        {
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        })
    };
}
```

### 4. Orchestration Implementation

```csharp
// Implementation in NRDAgentsOrchestrator.cs

private KernelFunctionSelectionStrategy CreateSelectionStrategy()
{
    var selectionFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
        Examine the provided RESPONSE and choose the next participant.
        State only the name of the chosen participant without explanation.
        
        Choose only from these participants:
        - {{{MonitoringAgent}}}
        - {{{DiagnosticAgent}}}
        - {{{RemediationAgent}}}
        - {{{KnowledgebaseAgent}}}
        
        Always follow these rules when choosing the next participant:
        - If RESPONSE is user input, it is {{{MonitoringAgent}}}'s turn.
        - If RESPONSE contains "MONITORING FINDINGS:" and "Attention Required: Yes", it is {{{DiagnosticAgent}}}'s turn.
        - If RESPONSE contains "DIAGNOSTIC FINDINGS:" and mentions credential issues, it is {{{RemediationAgent}}}'s turn.
        - If RESPONSE contains "REMEDIATION COMPLETED:" and "Verification Result: Failed", it is {{{DiagnosticAgent}}}'s turn.
        - If RESPONSE contains "REMEDIATION COMPLETED:" and "Verification Result: Success", it is {{{KnowledgebaseAgent}}}'s turn.
        
        RESPONSE:
        {{$lastmessage}}
        """,
        safeParameterNames: "lastmessage");

    return new KernelFunctionSelectionStrategy(selectionFunction, _kernel);
}

private KernelFunctionTerminationStrategy CreateTerminationStrategy()
{
    var terminationFunction = AgentGroupChat.CreatePromptFunctionForStrategy(
        """
        Your task is to determine if the NRD resolution session should end.
        The session should ONLY end when ALL of these have occurred:
        1. MonitoringAgent has identified an NRD issue
        2. DiagnosticAgent has diagnosed the root cause
        3. RemediationAgent has fixed the issue and verification was successful
        4. KnowledgebaseAgent has provided documentation
        
        Analyze this message:
        {{$lastmessage}}
        
        Respond with ONLY one of these:
        - 'CONTINUE' if any of the above steps are missing
        - 'COMPLETE' if you see "ISSUE RESOLVED" in the KnowledgebaseAgent's response
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

public async Task HandleNRDIssue(string deviceId)
{
    try
    {
        await _agentChat.ResetAsync();
        _agentChat.IsComplete = false;

        _agentChat.AddChatMessage(new ChatMessageContent(AuthorRole.User, $"Device {deviceId} appears to be an NRD. Please investigate."));

        var turnCount = 0;

        await foreach (var response in _agentChat.InvokeAsync())
        {
            turnCount++;
            Console.WriteLine($"\n--- Turn {turnCount} ---");
            Console.WriteLine($"{response.AuthorName.ToUpperInvariant()}:{Environment.NewLine}{response.Content}");
        }

        Console.WriteLine("\n=== NRD Resolution Session Complete ===");
        Console.WriteLine($"Total turns: {turnCount}");
    }
    catch (Exception ex)
    {
        _logger.LogError($"Error during NRD resolution: {ex.Message}");
        Console.WriteLine($"Error during NRD resolution: {ex.Message}");
    }
}
```

## Testing and Validation

1. **Test Scenarios**:
   - Device with expired credentials
   - Device with corrupted credentials
   - Device with missing credentials
   - Device with correct credentials but other issues

2. **Validation Criteria**:
   - Correct identification of NRD issues
   - Accurate diagnosis of credential problems
   - Successful remediation of issues
   - Proper documentation of resolution steps

## Next Steps

1. Implement the core classes and infrastructure
2. Develop the mock plugins for JamC, Splunk, and credential management
3. Create the specialized agents with appropriate instructions
4. Implement the orchestration logic
5. Test with various NRD scenarios
6. Refine agent instructions based on test results
7. Add comprehensive logging and reporting

## Conclusion

This implementation plan provides a structured approach to building a Semantic Kernel-based system for resolving NRD issues caused by device credential errors. By following this plan, we will create a robust system that can automatically detect, diagnose, and resolve these issues while maintaining proper documentation of the process.
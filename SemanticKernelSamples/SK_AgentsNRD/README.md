# NRD Issue Resolution System

## Overview

The NRD (Non-Reporting Device) Issue Resolution System is a multi-agent AI application built with Semantic Kernel that automatically identifies, diagnoses, and resolves issues with devices that have stopped reporting data. This proof-of-concept demonstrates how specialized AI agents can work together to solve complex operational problems with minimal human intervention.

## Key Features

- **Multi-Agent Architecture**: Specialized agents for monitoring, diagnosis, remediation, and documentation
- **Automatic Issue Resolution**: End-to-end workflow from detection to resolution
- **Simulation Mode**: Test and demonstrate without making API calls or incurring costs
- **Graceful Error Handling**: Automatic fallback mechanisms when API errors occur
- **Detailed Logging**: Comprehensive logging of the resolution process

## System Architecture

### Core Components

1. **NRDAgentsOrchestrator**: Coordinates the workflow between agents
2. **DeviceState**: Tracks device status and credential information
3. **Mock Plugins**:
   - **JamCPlugin**: Simulates device management system
   - **SplunkPlugin**: Simulates log retrieval and data collection verification
   - **CredentialPlugin**: Simulates credential management

### Agent Roles

1. **MonitoringAgent**: Detects NRD events and retrieves logs
2. **DiagnosticAgent**: Analyzes root causes of NRD issues
3. **RemediationAgent**: Executes recommended fixes
4. **KnowledgebaseAgent**: Provides documentation and best practices

## How It Works

### Resolution Workflow

1. **Detection**: MonitoringAgent identifies a device that is not reporting data
2. **Diagnosis**: DiagnosticAgent determines the root cause (e.g., expired credentials, network issues)
3. **Remediation**: RemediationAgent takes appropriate action to fix the issue
4. **Verification**: The system verifies that the device is now reporting correctly
5. **Documentation**: KnowledgebaseAgent documents the resolution and best practices

### Agent Selection Logic

The orchestrator determines which agent to activate next based on the current state:
- If monitoring detects an issue → DiagnosticAgent
- If diagnosis identifies credential issues → RemediationAgent
- If remediation is successful → KnowledgebaseAgent
- If remediation fails → DiagnosticAgent (for reassessment)

### Termination Criteria

The workflow terminates when:
- The issue has been successfully resolved
- The KnowledgebaseAgent has documented the resolution
- The maximum number of turns has been reached

## Technical Implementation

### Built With

- **.NET 8**: Core framework
- **Semantic Kernel 1.18.2+**: For AI orchestration and agent management
- **Azure OpenAI**: For LLM capabilities (with simulation fallback)

### Key Classes and Methods

- **NRDAgentsOrchestrator.HandleNRDIssue()**: Main entry point for issue resolution
- **GetAgentResponseAsync()**: Retrieves responses from agents
- **DetermineNextAgentAsync()**: Selects the next agent in the workflow
- **ShouldTerminateAsync()**: Determines if the resolution is complete

## Integration Guide

### Prerequisites

- Azure OpenAI service (optional - system can run in simulation mode)
- .NET 8 SDK

### Configuration

1. **Azure OpenAI Credentials**:
   - Set environment variables: `AZURE_ENDPOINT`, `AZURE_API_KEY`, and `DEPLOYMENT_NAME`
   - Or use user secrets (recommended for development):
     ```
     dotnet user-secrets set AZURE_ENDPOINT <your-endpoint>
     dotnet user-secrets set AZURE_API_KEY <your-api-key>
     dotnet user-secrets set DEPLOYMENT_NAME <your-deployment-name>
     ```

2. **Plugin Integration**:
   - Replace mock plugins with real implementations by maintaining the same interface
   - Ensure proper error handling in real implementations

### Execution Modes

1. **Real Mode**: Uses Azure OpenAI for agent responses
2. **Simulation Mode**: Uses predefined responses for testing and demonstration

## Adapting to Your Environment

### Replacing Mock Services

1. **Device Management System**:
   - Implement a real JamCPlugin that connects to your device management system
   - Maintain the same interface for GetDeviceStatus and CheckCredentials

2. **Log Management System**:
   - Implement a real SplunkPlugin that connects to your log management system
   - Ensure proper implementation of GetDeviceLogs and VerifyDataCollection

3. **Credential Management System**:
   - Implement a real CredentialPlugin that connects to your credential management system
   - Maintain the same interface for credential operations

### Extending the System

1. **Additional Agents**:
   - Add new specialized agents by following the agent pattern
   - Update the orchestrator to include new agents in the workflow

2. **New Issue Types**:
   - Extend the diagnostic and remediation capabilities for different types of issues
   - Update agent instructions to handle new scenarios

3. **Integration with Notification Systems**:
   - Add notification capabilities to alert operators of resolved issues
   - Implement escalation for issues that cannot be automatically resolved

## Best Practices for Implementation

1. **Agent Design**:
   - Keep agent instructions focused on specific roles
   - Ensure clear handoffs between agents

2. **Error Handling**:
   - Implement robust error handling at all levels
   - Provide graceful degradation paths

3. **Logging and Monitoring**:
   - Log all agent actions and decisions
   - Monitor resolution success rates and performance

4. **Security**:
   - Secure all credential management
   - Implement proper authentication for system access

## Testing

1. **Simulation Testing**:
   - Use simulation mode to test the full workflow without API costs
   - Create test cases for different device issues

2. **Integration Testing**:
   - Test with real services in a controlled environment
   - Verify correct handling of edge cases

## License

[Specify your license information here]

## Contact

[Your contact information]

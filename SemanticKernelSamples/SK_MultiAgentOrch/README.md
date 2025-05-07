# Multi-Agent Orchestration with Semantic Kernel

This sample demonstrates how to implement both centralized and decentralized orchestration patterns using Semantic Kernel, where multiple specialized agents collaborate to solve complex tasks.

## Latest Updates

- **Decentralized Orchestration**: Added implementation of the decentralized orchestration pattern where agents coordinate directly with each other without a central orchestrator
- **PlantUML Diagrams**: Added visual diagrams (`CentralOrch.puml` and `DecentralOrch.puml`) to illustrate both orchestration patterns
- **Enhanced Error Handling**: Improved error handling and task delegation in the decentralized pattern
- **User Agent Integration**: Added a dummy user agent to facilitate communication from user to the agent network

## Features

- **Multiple Orchestration Patterns**:
  - **Centralized Orchestration**: A central orchestrator agent coordinates the workflow and delegates tasks
  - **Decentralized Orchestration**: Agents coordinate directly with each other without a central orchestrator
- **Specialized Agents**: Four domain-specific agents (researcher, analyst, writer, reviewer) with different skills
- **Message-Based Communication**: Structured communication between agents using a message-passing system
- **Workflow Management**: Creation and execution of workflow plans with dependency tracking
- **Simulation Mode**: Option to run with or without making actual API calls to Azure OpenAI
- **Detailed Logging**: Comprehensive logging of agent interactions
- **Report Generation**: Creation of structured reports based on agent contributions

## Architecture

The sample consists of the following main components:

1. **AgentCommunicationHub**: Handles message passing between agents
2. **OrchestratorAgent**: Coordinates the workflow and delegates tasks in the centralized pattern
3. **PeerAgent**: Agents that coordinate directly with each other in the decentralized pattern
4. **SpecializedAgent**: Domain-specific agents with different skills for the centralized pattern
5. **AgentMessage**: The message format used for communication
6. **Main Program**: Sets up the system and handles user input

## Prerequisites

- .NET 8.0 SDK or later
- Azure OpenAI service (for real mode)
- Environment variables:
  - AZURE_ENDPOINT: Your Azure OpenAI endpoint
  - AZURE_API_KEY: Your Azure OpenAI API key

## Running the Sample

1. Set the required environment variables:

   ```shell
   set AZURE_ENDPOINT=<your-azure-openai-endpoint>
   set AZURE_API_KEY=<your-azure-openai-api-key>
   ```

2. Build and run the application:

   ```shell
   dotnet build
   dotnet run
   ```

3. Enter your request when prompted, for example:

   ```text
   Progress of Indian economy in the last 10 years
   ```

4. Choose the execution mode:
   - Simulation mode: No API calls, faster, no costs
   - Real mode: Makes API calls to Azure OpenAI, may incur costs

5. Choose the orchestration pattern:
   - Centralized: A central orchestrator agent coordinates all tasks
   - Decentralized: Agents coordinate directly with each other

6. The system will generate a report based on the contributions of all agents

## Output

- **Console Output**: Shows the progress of the workflow and agent interactions
- **Log Files**: Detailed logs of agent interactions in the `logs` directory
- **Reports**: Generated reports in Markdown format in the `reports` directory

## Orchestration Patterns

### Centralized Orchestration

In the centralized pattern, a central orchestrator agent:

- Analyzes the user request and breaks it down into subtasks
- Assigns tasks to specialized agents based on their capabilities
- Manages dependencies between tasks
- Collects and compiles results from all agents

### Decentralized Orchestration

In the decentralized pattern, agents:

- Introduce themselves to the network with their capabilities
- Receive tasks directly from users or other agents
- Determine if they can handle a task or need to delegate
- Coordinate directly with other agents without a central controller
- Self-organize to complete complex tasks

## Visualization

The project includes PlantUML diagrams to visualize both orchestration patterns:

- `CentralOrch.puml`: Illustrates the centralized orchestration pattern with the OrchestratorAgent as the central coordinator
- `DecentralOrch.puml`: Shows the decentralized orchestration pattern with peer agents communicating directly

To view these diagrams, you can use any PlantUML viewer or the PlantUML extension in Visual Studio Code.

## Extending the Sample

This sample provides a foundation for building complex multi-agent systems with Semantic Kernel. You can extend it by:

1. Adding more specialized agents
2. Implementing more sophisticated task analysis
3. Adding error handling and retry logic
4. Implementing proper state management
5. Creating a UI to visualize the workflow progress

## Learn More

For more information about Semantic Kernel, see:

- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Semantic Kernel GitHub Repository](https://github.com/microsoft/semantic-kernel)
- [Multi-Agent Orchestration in Semantic Kernel](https://learn.microsoft.com/en-us/semantic-kernel/)

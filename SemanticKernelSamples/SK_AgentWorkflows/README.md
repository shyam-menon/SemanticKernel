# Semantic Kernel Agent Workflows

This project demonstrates various workflow patterns for building AI applications using Microsoft's Semantic Kernel framework. The examples are inspired by the Anthropic article on LLM workflow patterns and adapted to showcase Semantic Kernel's capabilities.

## Prerequisites

- Windows 11 (64-bit)
- .NET 8.0 or later
- Azure OpenAI API access
- Environment variables:
  - `AZURE_ENDPOINT`: Your Azure OpenAI endpoint
  - `AZURE_API_KEY`: Your Azure OpenAI API key

## Project Structure

```
SK_AgentWorkflows/
├── Examples/           # Workflow pattern implementations
├── Prompts/           # Prompt templates
├── Tools/             # Native function implementations
└── Program.cs         # Main entry point
```

## Workflow Examples

### 1. Prompt Chaining
Demonstrates how to chain multiple prompts together to break down complex tasks into manageable steps. Shows context passing and error handling between steps.

**Key Features:**
- Sequential prompt execution
- Context preservation
- Structured data handling
- Error management

### 2. Routing
Shows how to analyze and route requests to appropriate handlers based on content and intent.

**Key Features:**
- Request analysis
- Dynamic routing
- Confidence scoring
- Specialized handlers

### 3. Tool Use
Illustrates how to create and use native functions as tools within Semantic Kernel.

**Key Features:**
- Native function integration
- Tool discovery
- Parameter handling
- Error handling

### 4. Parallelization
Demonstrates processing multiple items concurrently while maintaining order and handling failures.

**Key Features:**
- Concurrent processing
- Order preservation
- Partial failure handling
- Result aggregation

### 5. Orchestrator-Workers
Implements a content creation system with specialized workers coordinated by an orchestrator.

**Key Features:**
- Task decomposition
- Worker specialization
- Dependency tracking
- Progress monitoring

### 6. Evaluator-Optimizer
Shows how to implement feedback loops for iterative content improvement.

**Key Features:**
- Quality evaluation
- Improvement suggestions
- Iterative refinement
- Progress tracking

### 7. Agents
Demonstrates autonomous agents that can plan and execute tasks using available tools.

**Key Features:**
- Task planning
- Tool selection
- Progress monitoring
- State management

## Best Practices

This project follows several best practices for Semantic Kernel development:

1. **Project Organization**
   - Separate prompts from code
   - Clear directory structure
   - Consistent naming conventions

2. **Error Handling**
   - JSON validation
   - Meaningful error messages
   - Graceful failure handling

3. **Security**
   - Environment variables for credentials
   - Input validation
   - Secure communication

4. **Performance**
   - Async operations
   - Proper resource cleanup
   - Efficient prompt design

## References

- [Semantic Kernel Documentation](https://learn.microsoft.com/en-us/semantic-kernel/)
- [Semantic Kernel GitHub Repository](https://github.com/microsoft/semantic-kernel)
- [Anthropic's LLM Workflow Patterns](https://www.anthropic.com/index/claude-pattern-for-llm-workflows)

## Contributing

Feel free to contribute by:
1. Opening issues for bugs or suggestions
2. Submitting pull requests with improvements
3. Adding new workflow patterns
4. Improving documentation

## License

This project is licensed under the MIT License - see the LICENSE file for details.

---
Last Updated: January 27, 2025

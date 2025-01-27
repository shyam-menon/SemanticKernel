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
![Technical Docs Assistant Architecture](https://www.plantuml.com/plantuml/png/XLTDR-Cs4BtxLn2-z1GLk_qeq4EnWHltiCqcZLlieGWALfwrkP58WQIIsCN-zv8QatwIkNOE0VZcSTuRpT2YtZalhU_RHgoebTghMhmdgwaH7urz-jIO5t4llG-GcytSazATjB8LjMaxeqdxbTyr92pLNkfDGq1fb2Q_wqWOwxS60l_A-cbZJQ_N2zCOArwsokF6DNsIVUENHliFiYLnRPLiol6LsfDuytOs-qOiJDiQBHR9c9jrGKTkICmvD1V_pKJu-_lUcat8KzNkKKWdRgNI75Yz_-Tw4xQcobsWTDPy7cXXr-TGvUnB57jJnNNRUR7OXkrARuvgaq2IpKzCp6zUPTirvBvbQc86jJah8NhOFXZyZJHPwUc1NCZbwF4OMXhRId_N-wxt37yP_W__9jvOPX0u0ET4Al_gXoSG0WdZRMNrp6SGgE3e99Uuklf5JAMPEW479WeEB1GxON3JtbRYmVH-r2m6pZOfKg0xl1or9KBqHqzsztZw67-bJbhValRGjGlj0Pq3L9RBZtg0c7hH0ROqXCeDWepyE8-YCGnBQ5n9Ah91vWAR6_LT9VuqfX5_EJfsAY7dMXKvWP9QTLZYixkLVdlNHNKLz0gT03KtnvqVmfPd4ijUrxmtAZ0OCJTD1Q6XwB4o5_fz9UwbbKr3ZTeFWMEzay2viYVKL7w2iOhGXPgSUw1dHI_kyYHYdi8eNclPxFR4wV9KyPBf9zmLQQzYppCz8zCTgxvjfLLx6hOwvV8MZkpAemloF6_ZeYC7DS_GGATCuZR-K8axMs_9UIkziLVn_K4MNa2bwBc68lz0JqqjXB0X68OVemTo94E8Tq3WerMUx0byirRUdC9BMOVt4jLFkENo036tC3uBeprfZwPpYoC8_IsKaQaCJ-bO790PP6gfOA0o2DJIGm1-Z9H7CXO3GuG5npXMa3YZMfa5rfd30330CJCO04Rq2F6JOi0pc2muAd1AZf1HTeICiYS-PJzMukPPDlrmYEuwhrgr9tiyP8NWkJEMwPcTZbYC170ygDsEtrfuhXZdHATuodlva6JLuX41PRoACRTa1OYGjMmEpbltzQiQ30pwuVwn1130HoHmPeY3UbOPvoqd6YFbP6D4LDN4iVXPp9T4w_WD8dutfflpSFrKYUlD-Ak1r-T6Y1bfR7Y5lHkG-qPgpKE0yCqhrRqt9_YjqIh15sU61HWe2qE3uvgvPIj7PWHAJ3O7nCEQkMVlKk1tMA9R_h9YF7sKHMm09bbZ5JYEM6cCWtUEGu5CGC_0OPWfMRoSWesWSfatHifI0F2qOjeuhDThsVdXMeVZ6hzPnU4xDjwkIQz9ruhSB4QYrEGhCk5yaKSWtP8paEzx6IbTEd8m_Lnc81sasRlWgc-R_m40)

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

using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace SK_AgentWorkflows.Examples;

public class ToolUse
{
    public static async Task RunAsync()
    {
        // Get Azure OpenAI credentials from environment variables
        var deploymentName = "GPT-4o";
        var endpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        var apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");

        if (string.IsNullOrEmpty(deploymentName) || string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Please set the following environment variables:");
            Console.WriteLine("- AZURE_ENDPOINT");
            Console.WriteLine("- AZURE_API_KEY");
            return;
        }

        // Initialize the kernel with Azure OpenAI
        var kernelBuilder = Kernel.CreateBuilder();
        kernelBuilder.AddAzureOpenAIChatCompletion(
            deploymentName: deploymentName,
            endpoint: endpoint,
            apiKey: apiKey
        );
        
        var kernel = kernelBuilder.Build();

        // Register our calculator tool
        var calculator = new CalculatorTool();
        var calculatorFunctions = kernel.ImportPluginFromObject(calculator, "Calculator");

        // Create the main agent function
        var mathSolverFunction = kernel.CreateFunctionFromPrompt(@"
            You are a helpful math assistant that can solve mathematical problems.
            You have access to a calculator tool with the following functions:
            - Add(x, y): Adds two numbers
            - Subtract(x, y): Subtracts y from x
            - Multiply(x, y): Multiplies two numbers
            - Divide(x, y): Divides x by y

            Solve the given math problem step by step, using the calculator tool when needed.
            Show your work and explain each step.

            Problem: {{$input}}
            
            Let me solve this step by step.");

        while (true)
        {
            Console.WriteLine("\nEnter a math problem (or 'exit' to quit):");
            Console.WriteLine("Example: If I have 5 boxes with 3 items each, and I give away 2 boxes, how many items do I have left?");
            
            var problem = Console.ReadLine();
            if (string.IsNullOrEmpty(problem) || problem.Equals("exit", StringComparison.OrdinalIgnoreCase))
                break;

            try
            {
                var result = await kernel.InvokeAsync(mathSolverFunction, new() { ["input"] = problem });
                Console.WriteLine(result.GetValue<string>());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
        }
    }
}

public class CalculatorTool
{
    [Description("Add two numbers")]
    [KernelFunction]
    public double Add(
        [Description("First number")] double x,
        [Description("Second number")] double y)
    {
        return x + y;
    }

    [Description("Subtract second number from first number")]
    [KernelFunction]
    public double Subtract(
        [Description("First number")] double x,
        [Description("Second number")] double y)
    {
        return x - y;
    }

    [Description("Multiply two numbers")]
    [KernelFunction]
    public double Multiply(
        [Description("First number")] double x,
        [Description("Second number")] double y)
    {
        return x * y;
    }

    [Description("Divide first number by second number")]
    [KernelFunction]
    public double Divide(
        [Description("First number")] double x,
        [Description("Second number")] double y)
    {
        if (y == 0)
            throw new DivideByZeroException("Cannot divide by zero");
        return x / y;
    }
}

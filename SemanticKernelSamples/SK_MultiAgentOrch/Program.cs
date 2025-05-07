using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.MultiAgentOrchestration;
using System.Text;
using System.Text.Json;

// Initialize logging
var logsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "logs");
if (!Directory.Exists(logsDirectory))
{
    Directory.CreateDirectory(logsDirectory);
}

var logFilePath = Path.Combine(logsDirectory, $"agent_interactions_{DateTime.Now:yyyyMMdd_HHmmss}.log");
using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
    builder.AddFile(logFilePath);
    builder.SetMinimumLevel(LogLevel.Debug); // Use Debug level to capture more detailed interactions
});

var logger = loggerFactory.CreateLogger<Program>();
logger.LogInformation("Starting Multi-Agent Orchestration Sample");
logger.LogInformation($"Detailed logs will be written to: {logFilePath}");

// Get Azure OpenAI credentials from environment variables
var deploymentName = "gpt-4";
var endpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
var apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");

if (string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Please set the following environment variables:");
    Console.WriteLine("- AZURE_ENDPOINT");
    Console.WriteLine("- AZURE_API_KEY");
    return;
}

// Create the agent communication hub
var communicationHub = new AgentCommunicationHub(loggerFactory.CreateLogger<AgentCommunicationHub>());

// Define agent specialties
var agentSpecialties = new Dictionary<string, string>
{
    { "researcher", "Researching information and gathering data" },
    { "analyst", "Analyzing data and providing insights" },
    { "writer", "Creating well-written content based on research and analysis" },
    { "reviewer", "Reviewing and improving content for accuracy and quality" }
};

// Create specialized agents
var agents = await CreateSpecializedAgentsAsync(apiKey, endpoint, communicationHub, loggerFactory, deploymentName);

// Create the orchestrator agent
var orchestrator = new OrchestratorAgent(
    CreateKernel(apiKey, endpoint, deploymentName),
    communicationHub,
    agentSpecialties,
    loggerFactory.CreateLogger<OrchestratorAgent>());

// Create the decentralized orchestration instance
var decentralizedOrchestration = new DecentralizedOrchestration(loggerFactory, apiKey, endpoint, deploymentName);

// Process a user request
Console.WriteLine("Enter your request (or 'exit' to quit):");
string? userInput;
while ((userInput = Console.ReadLine()) != "exit")
{
    if (string.IsNullOrEmpty(userInput))
    {
        continue;
    }
    
    // Ask the user whether to use simulation mode or real mode
    Console.WriteLine("\nChoose execution mode:");
    Console.WriteLine("1. Simulation mode (no API calls, faster, no costs)");
    Console.WriteLine("2. Real mode (makes API calls to Azure OpenAI, may incur costs)");
    Console.Write("Enter your choice (1 or 2): ");
    
    string? modeChoice = Console.ReadLine();
    bool useSimulationMode = modeChoice != "2"; // Default to simulation mode unless explicitly choosing 2
    
    if (useSimulationMode)
    {
        Console.WriteLine("\nRunning in simulation mode (no API calls will be made).");
    }
    else
    {
        Console.WriteLine("\nRunning in real mode (API calls will be made to Azure OpenAI).");
    }
    
    // Ask the user whether to use centralized or decentralized orchestration
    Console.WriteLine("\nChoose orchestration pattern:");
    Console.WriteLine("1. Centralized (orchestrator agent coordinates all tasks)");
    Console.WriteLine("2. Decentralized (agents coordinate directly with each other)");
    Console.Write("Enter your choice (1 or 2): ");
    
    string? patternChoice = Console.ReadLine();
    bool useDecentralizedPattern = patternChoice == "2"; // Default to centralized unless explicitly choosing 2
    
    if (useDecentralizedPattern)
    {
        Console.WriteLine("\nUsing decentralized orchestration pattern.");
    }
    else
    {
        Console.WriteLine("\nUsing centralized orchestration pattern.");
    }
    
    var conversationId = Guid.NewGuid().ToString();
    logger.LogInformation($"Processing user request with conversation ID: {conversationId}");
    
    try
    {
        if (useDecentralizedPattern)
        {
            // Use decentralized orchestration pattern
            await decentralizedOrchestration.RunAsync(userInput, useSimulationMode);
        }
        else
        {
            // Use centralized orchestration pattern
            var response = await orchestrator.ProcessUserRequestAsync(userInput, conversationId);
            logger.LogInformation($"Initial orchestrator response: {response.MessageType}");
            
            // In a real application, you would continue monitoring the workflow
            // For this sample, we'll just wait a bit and then check the status
            await Task.Delay(2000);
            
            // Simulate receiving updates from agents
            await SimulateAgentUpdatesAsync(communicationHub, conversationId, agents, userInput, useSimulationMode);
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing request");
    }
    
    Console.WriteLine("\nEnter your next request (or 'exit' to quit):");
}

// Helper methods
static Kernel CreateKernel(string apiKey, string endpoint, string deploymentName = "gpt-4")
{
    var builder = Kernel.CreateBuilder();
    
    // Configure Azure OpenAI chat completion service
    builder.AddAzureOpenAIChatCompletion(
        deploymentName: deploymentName,
        endpoint: endpoint,
        apiKey: apiKey);
    
    return builder.Build();
}

static async Task<Dictionary<string, SpecializedAgent>> CreateSpecializedAgentsAsync(
    string apiKey, 
    string endpoint, 
    AgentCommunicationHub communicationHub,
    ILoggerFactory loggerFactory,
    string deploymentName = "gpt-4")
{
    var agents = new Dictionary<string, SpecializedAgent>();
    
    // Create researcher agent
    var researcherKernel = CreateKernel(apiKey, endpoint, deploymentName);
    var researcher = new SpecializedAgent(
        "researcher",
        researcherKernel,
        communicationHub,
        "You are a skilled researcher who excels at finding and summarizing information. Your task is to gather relevant data and facts on the given topic.",
        loggerFactory.CreateLogger<SpecializedAgent>());
    agents.Add("researcher", researcher);
    
    // Create analyst agent
    var analystKernel = CreateKernel(apiKey, endpoint, deploymentName);
    var analyst = new SpecializedAgent(
        "analyst",
        analystKernel,
        communicationHub,
        "You are a data analyst who excels at interpreting information and extracting insights. Your task is to analyze the provided data and identify key patterns, trends, and conclusions.",
        loggerFactory.CreateLogger<SpecializedAgent>());
    agents.Add("analyst", analyst);
    
    // Create writer agent
    var writerKernel = CreateKernel(apiKey, endpoint, deploymentName);
    var writer = new SpecializedAgent(
        "writer",
        writerKernel,
        communicationHub,
        "You are a skilled writer who excels at creating clear, engaging, and well-structured content. Your task is to create high-quality written material based on the research and analysis provided.",
        loggerFactory.CreateLogger<SpecializedAgent>());
    agents.Add("writer", writer);
    
    // Create reviewer agent
    var reviewerKernel = CreateKernel(apiKey, endpoint, deploymentName);
    var reviewer = new SpecializedAgent(
        "reviewer",
        reviewerKernel,
        communicationHub,
        "You are a meticulous reviewer who excels at improving content quality. Your task is to review written material for accuracy, clarity, and effectiveness, and provide specific suggestions for improvement.",
        loggerFactory.CreateLogger<SpecializedAgent>());
    agents.Add("reviewer", reviewer);
    
    return agents;
}

static async Task SimulateAgentUpdatesAsync(
    AgentCommunicationHub communicationHub, 
    string conversationId, 
    Dictionary<string, SpecializedAgent> agents,
    string userRequest,
    bool useSimulationMode)
{
    // Store the agent outputs for the final report
    Dictionary<string, string> agentOutputs = new Dictionary<string, string>();
    
    // Get task-specific content from the specialized agents
    string researchContent = await agents["researcher"].GenerateTaskResultAsync(
        $"Research the {userRequest}", 
        "Content Creation",
        useSimulationMode);
    
    // Store the research content
    agentOutputs["research"] = researchContent;
    
    // Simulate the researcher completing their task
    var researchResult = new AgentMessage
    {
        MessageType = "TaskResult",
        ConversationId = conversationId,
        Content = new Dictionary<string, object>
        {
            { "stepId", Guid.NewGuid().ToString() },
            { "result", researchContent }
        }
    };
    
    await communicationHub.SendMessageAsync("researcher", "orchestrator", researchResult);
    Console.WriteLine("\nResearcher has completed their task and sent results to the orchestrator.");
    
    // Simulate the analyst completing their task
    await Task.Delay(1000);
    
    string analysisContent = await agents["analyst"].GenerateTaskResultAsync(
        $"Analyze the research findings on {userRequest}", 
        "Content Creation",
        useSimulationMode);
    
    // Store the analysis content
    agentOutputs["analysis"] = analysisContent;
    
    var analysisResult = new AgentMessage
    {
        MessageType = "TaskResult",
        ConversationId = conversationId,
        Content = new Dictionary<string, object>
        {
            { "stepId", Guid.NewGuid().ToString() },
            { "result", analysisContent }
        }
    };
    
    await communicationHub.SendMessageAsync("analyst", "orchestrator", analysisResult);
    Console.WriteLine("Analyst has completed their task and sent results to the orchestrator.");
    
    // Simulate the writer completing their task
    await Task.Delay(1000);
    
    string writingContent = await agents["writer"].GenerateTaskResultAsync(
        $"Create content based on research and analysis about {userRequest}", 
        "Content Creation",
        useSimulationMode);
    
    // Store the writing content
    agentOutputs["writing"] = writingContent;
    
    var writingResult = new AgentMessage
    {
        MessageType = "TaskResult",
        ConversationId = conversationId,
        Content = new Dictionary<string, object>
        {
            { "stepId", Guid.NewGuid().ToString() },
            { "result", writingContent }
        }
    };
    
    await communicationHub.SendMessageAsync("writer", "orchestrator", writingResult);
    Console.WriteLine("Writer has completed their task and sent results to the orchestrator.");
    
    // Simulate the reviewer completing their task
    await Task.Delay(1000);
    
    string reviewContent = await agents["reviewer"].GenerateTaskResultAsync(
        $"Review and improve the content about {userRequest}", 
        "Content Creation",
        useSimulationMode);
    
    // Store the review content
    agentOutputs["review"] = reviewContent;
    
    var reviewResult = new AgentMessage
    {
        MessageType = "TaskResult",
        ConversationId = conversationId,
        Content = new Dictionary<string, object>
        {
            { "stepId", Guid.NewGuid().ToString() },
            { "result", reviewContent }
        }
    };
    
    await communicationHub.SendMessageAsync("reviewer", "orchestrator", reviewResult);
    Console.WriteLine("Reviewer has completed their task and sent results to the orchestrator.");
    
    // Create the final report content
    var finalReport = new StringBuilder();
    finalReport.AppendLine($"# FINAL REPORT: {userRequest}");
    finalReport.AppendLine("## Generated by Multi-Agent System");
    finalReport.AppendLine($"## Date: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
    finalReport.AppendLine();
    finalReport.AppendLine("### Executive Summary");
    finalReport.AppendLine("This report was created through collaboration between specialized AI agents, each contributing their expertise to produce a comprehensive document.");
    finalReport.AppendLine();
    finalReport.AppendLine("### Research Findings");
    finalReport.AppendLine(agentOutputs["research"]);
    finalReport.AppendLine();
    finalReport.AppendLine("### Analysis");
    finalReport.AppendLine(agentOutputs["analysis"]);
    finalReport.AppendLine();
    finalReport.AppendLine("### Content");
    finalReport.AppendLine(agentOutputs["writing"]);
    finalReport.AppendLine();
    finalReport.AppendLine("### Review and Improvements");
    finalReport.AppendLine(agentOutputs["review"]);
    
    // Save the report to a file
    var reportsDirectory = Path.Combine(Directory.GetCurrentDirectory(), "reports");
    if (!Directory.Exists(reportsDirectory))
    {
        Directory.CreateDirectory(reportsDirectory);
    }
    
    var reportFilePath = Path.Combine(reportsDirectory, $"report_{DateTime.Now:yyyyMMdd_HHmmss}.md");
    await File.WriteAllTextAsync(reportFilePath, finalReport.ToString());
    
    // Display the final result in the console
    Console.WriteLine("\nOrchestrator has compiled the final result:");
    Console.WriteLine("==================================================");
    Console.WriteLine("Task completed successfully. Final document has been created with all agent contributions incorporated.");
    Console.WriteLine("Research provided comprehensive background information.");
    Console.WriteLine("Analysis identified key patterns and insights.");
    Console.WriteLine("Writing created a clear and engaging narrative.");
    Console.WriteLine("Review ensured accuracy and suggested final improvements.");
    Console.WriteLine("==================================================");
    Console.WriteLine($"\nThe complete report has been saved to: {reportFilePath}");
    
    // Simulate final result from orchestrator
    await Task.Delay(1000);
}

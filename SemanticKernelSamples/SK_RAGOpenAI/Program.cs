using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using System.Text;
using SK_RAGOpenAI;

#pragma warning disable SKEXP0001

// Document model class
public class Document
{
    [VectorStoreRecordKey]
    public string Id { get; set; }

    [VectorStoreRecordData(IsFilterable = true)]
    public string Title { get; set; }

    [VectorStoreRecordData(IsFullTextSearchable = true)]
    public string Content { get; set; }

    [VectorStoreRecordVector(1536)] // 1536 dimensions for OpenAI ada-002
    public ReadOnlyMemory<float> ContentEmbedding { get; set; }
}

// Create a helper class to read markdown files
public class MarkdownFileReader
{
    public static async Task<List<(string Title, string Content)>> ReadMarkdownFilesFromDirectory(string directoryPath)
    {
        var documents = new List<(string Title, string Content)>();

        foreach (var file in Directory.GetFiles(directoryPath, "*.md"))
        {
            var content = await File.ReadAllTextAsync(file);
            var title = Path.GetFileNameWithoutExtension(file);

            // Simple markdown parsing - you might want to use a proper markdown parser
            // This is a basic example that treats the first line as title if it's a header
            var lines = content.Split('\n');
            if (lines.Length > 0 && lines[0].StartsWith("# "))
            {
                title = lines[0].Substring(2).Trim();
                content = string.Join("\n", lines.Skip(1)).Trim();
            }

            documents.Add((title, content));
        }

        return documents;
    }
}

public class Program
{
    
    // Replace with your actual API key
    private static string? OpenAIApiKey => Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public static async Task Main()
    {
        // Initialize services
        #pragma warning disable SKEXP0010
        var embeddingGeneration = new OpenAITextEmbeddingGenerationService(
            modelId: "text-embedding-ada-002",
            apiKey: OpenAIApiKey);

        // Create kernel with chat completion service
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion("gpt-4o-mini", OpenAIApiKey)
            .Build();

        // Initialize vector store and collection
        var vectorStore = new InMemoryVectorStore();
        var collection = vectorStore.GetCollection<string, Document>("documents");
        await collection.CreateCollectionIfNotExistsAsync();

        // Load documents from markdown files
        Console.WriteLine("RAG with Open AI and InMemory vector store. Loading markdown files...");
        var markdownPath = Path.Combine(Directory.GetCurrentDirectory(), "docs"); // Folder containing your markdown files
        var documents = await MarkdownFileReader.ReadMarkdownFilesFromDirectory(markdownPath);
       

        // Ingest documents
        Console.WriteLine("Ingesting documents...");
        foreach (var doc in documents)
        {
            var embedding = await embeddingGeneration.GenerateEmbeddingAsync(doc.Content);

            await collection.UpsertAsync(new Document
            {
                Id = Guid.NewGuid().ToString(),
                Title = doc.Title,
                Content = doc.Content,
                ContentEmbedding = embedding
            });
        }

        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var userStoryAgent = new UserStoryAgent(chatCompletionService, collection, embeddingGeneration);

        // Create a search function that we'll use in our RAG prompt
        var searchFunction = async (string query) =>
        {
            var queryEmbedding = await embeddingGeneration.GenerateEmbeddingAsync(query);
            var searchResults = await collection.VectorizedSearchAsync(queryEmbedding, new() { Top = 2 });

            var results = new List<string>();
            await foreach (var result in searchResults.Results)
            {
                results.Add($"Title: {result.Record.Title}\nContent: {result.Record.Content}\n");
            }

            return string.Join("\n", results);
        };

        // Create chat history
        var chatHistory = new ChatHistory();

        while (true)
        {
            // Get user input
            Console.Write("\nEnter your question (or 'exit' to quit): ");
            var input = Console.ReadLine();

            if (string.IsNullOrEmpty(input) || input.ToLower() == "exit")
                break;

            if (InputAnalyzer.IsUserStoryRequest(input))
            {
                var userStory = await userStoryAgent.GenerateUserStory(input);
                await File.WriteAllTextAsync($"UserStory_{DateTime.Now:yyyyMMddHHmmss}.md", userStory);
                Console.WriteLine("\nGenerated User Story:\n" + userStory);
            }
            else
            {
                var response = await ProcessRagQuery(input, collection, embeddingGeneration, chatCompletionService);
                Console.WriteLine("\nResponse: " + response);               

                // Add AI response to chat history
                chatHistory.AddAssistantMessage(response);
            }

           

            
        }
    }

    private static async Task<string> ProcessRagQuery(
   string query,
   IVectorStoreRecordCollection<string, Document> collection,
   ITextEmbeddingGenerationService embeddingGeneration,
   IChatCompletionService chatCompletionService)
    {
        // Generate embedding for query
        var queryEmbedding = await embeddingGeneration.GenerateEmbeddingAsync(query);

        // Search for relevant documents
        var searchResults = await collection.VectorizedSearchAsync(queryEmbedding, new() { Top = 2 });

        // Build context from search results
        var contextBuilder = new StringBuilder();
        await foreach (var result in searchResults.Results)
        {
            contextBuilder.AppendLine($"Content: {result.Record.Content}\n");
        }

        // Create RAG prompt
        var prompt = $$"""
       Use the following information to answer the question. 
       If you cannot answer based on the provided information, say "I don't have enough information to answer that question."

       Context:
       {{contextBuilder}}

       Question: {{query}}

       Answer:
       """;

        // Get AI response
        var chatHistory = new ChatHistory();
        chatHistory.AddUserMessage(prompt);
        var response = await chatCompletionService.GetChatMessageContentAsync(chatHistory);

        return response.Content;
    }
}
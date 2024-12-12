using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.InMemory;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using System.Text;

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
            .AddOpenAIChatCompletion("gpt-3.5-turbo", OpenAIApiKey)
            .Build();

        // Initialize vector store and collection
        var vectorStore = new InMemoryVectorStore();
        var collection = vectorStore.GetCollection<string, Document>("documents");
        await collection.CreateCollectionIfNotExistsAsync();

        // Load documents from markdown files
        Console.WriteLine("Loading markdown files...");
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
            var question = Console.ReadLine();

            if (string.IsNullOrEmpty(question) || question.ToLower() == "exit")
                break;

            // Search for relevant documents
            var contextInfo = await searchFunction(question);

            // Create RAG prompt
            var prompt = $$"""
                Use the following information to answer the question. 
                If you cannot answer the question based on the information provided, say "I don't have enough information to answer that question."

                Context information:
                {{contextInfo}}

                Question: {{question}}

                Answer: 
                """;

            // Add user question to chat history
            chatHistory.AddUserMessage(prompt);

            // Get AI response
            var response = await kernel.GetRequiredService<IChatCompletionService>()
                .GetChatMessageContentAsync(chatHistory);

            // Print response
            Console.WriteLine("\nAssistant: " + response.Content);

            // Add AI response to chat history
            chatHistory.AddAssistantMessage(response.Content);
        }
    }
}
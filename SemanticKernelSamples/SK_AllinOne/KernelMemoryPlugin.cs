using Microsoft.SemanticKernel;
using Microsoft.KernelMemory;
using System.ComponentModel;
using System.Reflection;

public class KernelMemoryPlugin
{
    private readonly IKernelMemory _memory;

    public KernelMemoryPlugin(IKernelMemory memory)
    {
        _memory = memory;
    }

    [KernelFunction, Description("Import text into memory")]
    public async Task<string> ImportTextAsync(string text, string documentId, string collection)
    {
        return await _memory.ImportTextAsync(
            text: text,
            documentId: documentId,
            tags: new TagCollection { { "collection", collection } }
        );
    }

    [KernelFunction, Description("Search for information in memory")]
    public async Task<string> SearchAsync(string query, string collection)
    {
        var result = await _memory.SearchAsync(
            query: query,
            filter: new MemoryFilter().ByTag("collection", collection)
        );

        return string.Join("\n", result.Results.Select(r => r.ToString()));
    }

    [KernelFunction, Description("Ask a question and get an answer based on the information in memory")]
    public async Task<string> AskAsync(string question, string collection)
    {
        var answer = await _memory.AskAsync(
            question: question,
            filter: new MemoryFilter().ByTag("collection", collection)
        );

        return answer.Result;
    }

    [KernelFunction, Description("Import embedded resource into memory")]
    public async Task<string> ImportEmbeddedResourceAsync(string resourceName, string documentId, string collection)
    {
        string text = await EmbeddedResourceHelper.ReadAllTextAsync(resourceName);
        return await ImportTextAsync(text, documentId, collection);
    }

    [KernelFunction, Description("Perform RAG using memory")]
    public async Task<string> PerformRAGAsync(string question, string collection)
    {
        // Retrieve relevant information from memory
        var searchResults = await SearchAsync(question, collection);

        // Combine the search results with the question
        string combinedInput = $"{searchResults}\n\n{question}";

        // Use the combined input to get an answer
        return await AskAsync(combinedInput, collection);
    }
}


public static class EmbeddedResourceHelper
{
    public static async Task<string> ReadAllTextAsync(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using Stream stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            throw new FileNotFoundException("Resource not found", resourceName);
        }

        using StreamReader reader = new(stream);
        return await reader.ReadToEndAsync();
    }
}


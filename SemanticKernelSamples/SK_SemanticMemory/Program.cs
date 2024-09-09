using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using Microsoft.SemanticKernel.Embeddings;
using Elastic.Transport;

class Program
{
    static async Task Main(string[] args)
    {

        // Get the Azure API key from environment variables
        var apiKey = Environment.GetEnvironmentVariable("AZURE_API_KEY");

        if (string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Please set the AZURE_API_KEY environment variable.");
            return;
        }

        var endpoint = Environment.GetEnvironmentVariable("AZURE_ENDPOINT");
        if (string.IsNullOrEmpty(endpoint))
        {
            Console.WriteLine("Please set the AZURE_ENDPOINT environment variable.");
            return;
        }
        // Initialize Semantic Kernel
#pragma warning disable SKEXP0010
        var builder = Kernel.CreateBuilder()
            .AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: "text-embedding-ada-002",
                endpoint: endpoint,
                apiKey: apiKey
            )
            .AddAzureOpenAIChatCompletion(
            "GPT-4o",
            endpoint,
            apiKey,
            "GPT-4o");
        var kernel = builder.Build();

        // Create a memory store
        #pragma warning disable SKEXP0050
        var memoryStore = new VolatileMemoryStore();
        #pragma warning disable SKEXP0001
        var embeddingGenerator = kernel.GetRequiredService<ITextEmbeddingGenerationService>();
        var memory = new SemanticTextMemory(memoryStore, embeddingGenerator);

        // Save some information to memory
        await memory.SaveInformationAsync("default", "HP Smart Device Services(SDS) is a cloud-based technology that allows HP resellers to significantly reduce service costs, maximize device uptime, and deliver an exceptional service experience to their customers", "Device connectivity");
        await memory.SaveInformationAsync("default", "The Managed Print Central (MPC) tool in the context of Contract Billing Automation (CBA) is a web-based application that enables partners to create and manage contracts for CBA customers. It facilitates device onboarding, pricing, and billing for CBA contracts. Specifically, MPC is used to convert legacy customers to CBA and archive their old proposals and opportunities.", "Sales");

        // Load and process PDF
        string pdfPath = "ManagedServices_CBA.pdf";
        await ProcessPdfAndStoreInMemory(memory, pdfPath);

        // Simulating RAG
        while (true)
        {
            Console.Write("Enter your question (or 'exit' to quit): ");
            string question = Console.ReadLine();
            if (question.ToLower() == "exit") break;

            var answer = await PerformRAG(kernel, memory, question);
            Console.WriteLine($"Answer: {answer}\n");
        }
    }

    static async Task ProcessPdfAndStoreInMemory(ISemanticTextMemory memory, string pdfPath)
    {
        var text = ExtractTextFromPdf(pdfPath);
        var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string result = string.Join(" ", lines);
        var chunks = TextChunker.SplitPlainTextLines(result, 1000);

        for (int i = 0; i < chunks.Count; i++)
        {
            string chunkText = string.Join(Environment.NewLine, chunks[i]);
            await memory.SaveInformationAsync($"chunk{i}", chunkText, $"chunk{i}");
        }
    }

    static string ExtractTextFromPdf(string path)
    {
        using (PdfReader reader = new PdfReader(path))
        {
            StringWriter output = new StringWriter();
            for (int i = 1; i <= reader.NumberOfPages; i++)
            {
                output.WriteLine(PdfTextExtractor.GetTextFromPage(reader, i));
            }
            return output.ToString();
        }
    }

    static async Task<string> PerformRAG(Kernel kernel, ISemanticTextMemory memory, string question)
    {
        // Search for relevant memories
        var searchResults = memory.SearchAsync("default", question, limit: 1, minRelevanceScore: 0.7);

        var anyResults = false;
        await foreach (var mem in searchResults)
        {
            anyResults = true;
            Console.WriteLine($"Found memory - ID: {mem.Metadata.Id}, Score: {mem.Relevance}, Text: {mem.Metadata.Text.Substring(0, Math.Min(50, mem.Metadata.Text.Length))}...");
        }

        if (!anyResults)
        {
            Console.WriteLine("No results found in memory.");
            return "I couldn't find any relevant information to answer your question.";
        }

        // Reset the enumerator
        searchResults = memory.SearchAsync("default", question, limit: 1, minRelevanceScore: 0.7);
        var relevantMemory = await searchResults.FirstOrDefaultAsync();

        if (relevantMemory == null)
        {
            Console.WriteLine("No memory met the relevance threshold.");
            return "I couldn't find any sufficiently relevant information to answer your question.";
        }

        Console.WriteLine($"Using memory - ID: {relevantMemory.Metadata.Id}, Score: {relevantMemory.Relevance}");

        // Use OpenAI to generate an answer
        var prompt = $"""
            Use the following piece of context to answer the question at the end.
            If you don't know the answer, just say that you don't know. Don't try to make up an answer.

            Context: {relevantMemory.Metadata.Text}

            Human: {question}

            Assistant: Let me answer that based on the context provided:
            """;

        var result = await kernel.InvokePromptAsync(prompt);
        return result.GetValue<string>() ?? "Sorry, I couldn't generate an answer.";
    }
}
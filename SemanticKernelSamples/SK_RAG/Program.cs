using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;

// 1.  Get the API key from environment variables
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

if (string.IsNullOrEmpty(apiKey))
{
    Console.WriteLine("Please set the OPENAI_API_KEY environment variable.");
    return;
}

//2. Create a Kernel Builder. Using Serverless Memory so that external setup is not needed
var memory = new KernelMemoryBuilder()
    .WithOpenAIDefaults(apiKey)
    .Build<MemoryServerless>();

//3. Feed the memory with the document and a web page
await memory.ImportDocumentAsync("ManagedServices_CBA.pdf", documentId: "doc001");
await memory.ImportWebPageAsync("https://www.hp.com/us-en/services/workforce-solutions/workforce-computing/managed-device-services.html", documentId: "doc002");

Console.Clear();

//4. Check if documents from PDF and web pages are ready
if (await memory.IsDocumentReadyAsync("doc001") && await memory.IsDocumentReadyAsync("doc002"))
{
  
    Console.WriteLine("Managed Services Document is ready, you can start asking questions!\n\n");
   

    //5. Start the chat
    while (true  )
    {
        var readUserInput = Console.ReadLine();

        Func<string, Task> Chat = async(string input) =>
        {
            //Get the answer using memory
            var answer = await memory.AskAsync(input);
            Console.WriteLine($"Question: {input}\n\nAnswer: {answer.Result}");

            Console.WriteLine("Sources:\n");

            foreach (var source in answer.RelevantSources)
            {
                Console.WriteLine($"  - {source.SourceName}  - {source.Link} [{source.Partitions.First().LastUpdate:D}]");
            }
        };
        await Chat(readUserInput);

        Console.WriteLine("\n\n");

    }

}
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable SKEXP0001

namespace SK_RAGOpenAI
{
    public class UserStoryAgent
    {
        private readonly IChatCompletionService _chatCompletionService;
        private readonly IVectorStoreRecordCollection<string, Document> _collection;
        private readonly ITextEmbeddingGenerationService _embeddingService;

        public UserStoryAgent(
            IChatCompletionService chatCompletionService,
            IVectorStoreRecordCollection<string, Document> collection,
            ITextEmbeddingGenerationService embeddingService)
        {
            _chatCompletionService = chatCompletionService;
            _collection = collection;
            _embeddingService = embeddingService;
        }

        public async Task<string> GenerateUserStory(string input)
        {
            // Search for relevant context
            var searchEmbedding = await _embeddingService.GenerateEmbeddingAsync(input);
            var searchResults = await _collection.VectorizedSearchAsync(searchEmbedding, new() { Top = 3 });

            var contextBuilder = new StringBuilder();
            await foreach (var result in searchResults.Results)
            {
                contextBuilder.AppendLine(result.Record.Content);
            }

            var prompt = $@"Use the following reference information and input to create a detailed user story in markdown format.
Follow the exact structure and formatting from the reference information.
Include all sections as shown in the reference: User Story ID/Title, Description, Actors, Preconditions, Postconditions, Main Flow, Alternative Flows, Business Rules, Data Requirements, Non-functional Requirements, and Assumptions/Dependencies.

Reference Information:
{contextBuilder}

Input:
{input}

Generate the user story:";

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(prompt);

            var response = await _chatCompletionService.GetChatMessageContentAsync(chatHistory);
            return response.Content;
        }
    }
}

using AiNewsBot_Backend.Core.Data.Contexts;
using AiNewsBot_Backend.Core.Data.Entities;
using AiNewsBot_Backend.Core.Helpers;
using AiNewsBot_Backend.Core.Models;
using OpenRouter.NET;
using OpenRouter.NET.Models;

namespace AiNewsBot_Backend.API.Services;

public class AiService
{
    private readonly ILogger<AiService> _logger;
    
    public AiService(ILogger<AiService> logger)
    {
        _logger = logger;
    }
    
    /// <summary>
    /// Формирует новый пост через ИИ из <paramref name="fullText"/>
    /// </summary>
    /// <param name="fullText"></param>
    /// <param name="aiChatClientSettings"></param>
    /// <param name="aiChatClient"></param>
    public async Task<string> ProcessAiSummarizeAsync(string fullText, string postId,
        PostsContext dbContext, OpenRouterClient aiChatClient, AiChatClientSettings aiChatClientSettings)
    {
        List<string> summaries = await SummarizeNewsPostAsync(fullText, aiChatClient, aiChatClientSettings);

        if (summaries.Count == 0)
        {
            return string.Empty;
        }

        string finallyText = string.Join("\n", summaries);
        
        await dbContext.Posts.AddAsync(new Post() { AiText = finallyText, PostId = postId, SourceText = fullText});
        await dbContext.SaveChangesAsync();
        
        return finallyText;
    }
    
    private async Task<List<string>> SummarizeNewsPostAsync(string fullText,
        OpenRouterClient aiChatClient, AiChatClientSettings aiChatClientSettings
    )
    {
        List<string> chunks = AiUtilities.SplitTextIntoChunks(fullText);
        List<string> summaries = new();

        _logger.LogInformation($"Обработка {chunks.Count} чанков");

        foreach (var chunk in chunks)
        {
            try
            {
                ChatCompletionRequest request = new()
                {
                    Model = aiChatClientSettings.Model,
                    Messages = new List<Message>
                    {
                        Message.FromSystem(aiChatClientSettings.SystemText),
                        Message.FromUser(aiChatClientSettings.StartUserText + $"\n{chunk}")
                    }
                };
                ChatCompletionResponse response = await aiChatClient.CreateChatCompletionAsync(request);

                if (response.Choices == null)
                {
                    _logger.LogError($"Ошибка обращения к ИИ при обработке чанка");
                    return new List<string>();
                }

                _logger.LogInformation($"Чанк успешно обработан");
                summaries.Add(response.Choices[0].Message.Content.ToString());
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, $"Ошибка обработки чанка");
            }
        }

        return summaries;
    }
}
using AiNewsBot_Backend.API.Models;
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
    private readonly PostsContext _dbContext;
    private readonly OpenRouterClient _aiChatClient;
    private readonly AiChatClientSettings _aiChatClientSettings;
    
    public AiService(ILogger<AiService> logger, PostsContext dbContext, OpenRouterClient aiChatClient, AiChatClientSettings aiChatClientSettings)
    {
        _dbContext = dbContext;
        _logger = logger;
        _aiChatClient = aiChatClient;
        _aiChatClientSettings = aiChatClientSettings;
    }
    
    /// <summary>
    /// Формирует новый пост через ИИ из <paramref name="fullText"/>
    /// </summary>
    /// <param name="fullText"></param>
    /// <param name="aiChatClientSettings"></param>
    /// <param name="aiChatClient"></param>
    public async Task<string> ProcessAiSummarizeAsync(PostCreateInfo postCreateInfo)
    {
        List<string> summaries = await SummarizeNewsPostAsync(postCreateInfo.Text);

        if (summaries.Count == 0)
        {
            return string.Empty;
        }

        string finallyText = string.Join("\n", summaries);
        
        await _dbContext.Posts.AddAsync(new Post() { AiText = finallyText, PostId = postCreateInfo.PostId, SourceText = postCreateInfo.Text});
        await _dbContext.SaveChangesAsync();
        
        return finallyText;
    }
    
    private async Task<List<string>> SummarizeNewsPostAsync(string fullText)
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
                    Model = _aiChatClientSettings.Model,
                    Messages = new List<Message>
                    {
                        Message.FromSystem(_aiChatClientSettings.SystemText),
                        Message.FromUser(_aiChatClientSettings.StartUserText + $"\n{chunk}")
                    }
                };
                ChatCompletionResponse response = await _aiChatClient.CreateChatCompletionAsync(request);

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
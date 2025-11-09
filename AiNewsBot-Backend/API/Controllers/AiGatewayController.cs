using System.ClientModel;
using AiNewsBot_Backend.API.Models;
using AiNewsBot_Backend.Core.Helpers;
using AiNewsBot_Backend.Core.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OpenAI.Chat;
using OpenRouter.NET;
using OpenRouter.NET.Models;

namespace AiNewsBot_Backend.API.Controllers;


[ApiController]
[Route("ai-gateway")]
public class AiGatewayController : ControllerBase
{
    private readonly OpenRouterClient _aiChatClient;
    private readonly ILogger<AiGatewayController> _logger;
    
    public AiGatewayController(OpenRouterClient aiChatClient, ILogger<AiGatewayController> logger)
    {
        _aiChatClient = aiChatClient;
        _logger = logger;
    }
    
    [HttpPost("summarize-post")]
    public async Task<IActionResult> SummarizePost([FromBody] AnalyzePostBody contentBody, [FromServices] AiChatClientSettings aiChatClientSettings)
    {
        List<string> chunks = AiUtilities.SplitTextIntoChunks(contentBody.Text);
        List<object> summaries = new();
        
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

                ChatCompletionResponse response = await _aiChatClient.CreateChatCompletionAsync(request);

                if (response.Choices == null)
                {
                    _logger.LogError($"Ошибка обращения к ИИ при обработке чанка");
                    return StatusCode(500, new APIResponse() { Message = "Ошибка при обработке чанка." });
                }

                _logger.LogInformation($"Чанк успешно обработан");
                summaries.Add(response.Choices[0].Message.Content);
            }
            catch (Exception e)
            {
                _logger.LogInformation(e, $"Ошибка обработки чанка");
                return StatusCode(500, new APIResponse() { Message = "Ошибка при обработке чанка." });
            }
        }

        string finallySummary = string.Join("\n", summaries);

        return Ok(new APIResponse() { Data = finallySummary});
    }
}
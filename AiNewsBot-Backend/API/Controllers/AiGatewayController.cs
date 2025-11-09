using System.ClientModel;
using AiNewsBot_Backend.API.Models;
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
        ChatCompletionRequest request = new()
        {
            Model = "deepseek/deepseek-chat-v3.1",
            Messages = new List<Message>
            {
                Message.FromSystem(aiChatClientSettings.SystemText),
                Message.FromUser(aiChatClientSettings.StartUserText + $"\n {contentBody.Text}")
            }
        };
        
        ChatCompletionResponse response = await _aiChatClient.CreateChatCompletionAsync(request);

        if (response.Choices == null)
        {
            return StatusCode(500, new APIResponse() {Message = "Ошибка при выполнении запроса к ИИ."});
        }

        return Ok(new APIResponse() { Data = response.Choices[0].Message?.Content});
    }
}
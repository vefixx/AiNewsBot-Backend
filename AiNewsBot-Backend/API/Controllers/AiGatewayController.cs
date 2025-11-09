using System.ClientModel;
using System.Net.WebSockets;
using System.Runtime.CompilerServices;
using AiNewsBot_Backend.API.Models;
using AiNewsBot_Backend.API.Services;
using AiNewsBot_Backend.Core.Data.Contexts;
using AiNewsBot_Backend.Core.Data.Entities;
using AiNewsBot_Backend.Core.Helpers;
using AiNewsBot_Backend.Core.Models;
using Hangfire;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using OpenAI.Chat;
using OpenRouter.NET;
using OpenRouter.NET.Models;

namespace AiNewsBot_Backend.API.Controllers;

[ApiController]
[Route("ai-gateway")]
public class AiGatewayController : ControllerBase
{
    private readonly PostsContext _dbContext;
    private readonly OpenRouterClient _aiChatClient;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<AiGatewayController> _logger;
    private readonly AiChatClientSettings _aiChatClientSettings;

    public AiGatewayController(PostsContext dbContext, OpenRouterClient aiChatClient,
        AiChatClientSettings aiChatClientSettings, ILogger<AiGatewayController> logger,
        IBackgroundJobClient backgroundJobClient)
    {
        _dbContext = dbContext;
        _aiChatClient = aiChatClient;
        _aiChatClientSettings = aiChatClientSettings;
        _logger = logger;
        _backgroundJobClient = backgroundJobClient;
    }

    [HttpPost("summarize-post")]
    public async Task<IActionResult> SummarizePost([FromBody] AnalyzePostBody contentBody)
    {
        string jobId =
            _backgroundJobClient.Enqueue<AiService>(service => service.ProcessAiSummarizeAsync(contentBody.Text,
                contentBody.PostId, _dbContext, _aiChatClient, _aiChatClientSettings));
        return Ok(new APIResponse() { Data = new JobIdData() { JobId = jobId } });
    }

    /// <summary>
    /// Формирует новый пост через ИИ из <paramref name="fullText"/>
    /// </summary>
    /// <param name="fullText"></param>
    /// <param name="aiChatClientSettings"></param>
    /// <param name="aiChatClient"></param>
    public async Task<string> ProcessAiSummarizeAsync(string fullText, string postId)
    {
        List<string> summaries = await SummarizeNewsPostAsync(fullText);

        if (summaries.Count == 0)
        {
            return string.Empty;
        }

        string finallyText = string.Join("\n", summaries);

        await _dbContext.Posts.AddAsync(new Post() { AiText = finallyText, PostId = postId, SourceText = fullText });
        await _dbContext.SaveChangesAsync();

        return finallyText;
    }

    private async Task<List<string>> SummarizeNewsPostAsync(string fullText
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

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts()
    {
        var posts = await _dbContext.Posts.ToListAsync();
        return Ok(new APIResponse() { Data = posts });
    }

    [HttpGet("summarize-post/job")]
    public async Task<IActionResult> GetTaskResult(string jobId)
    {
        var jobMonitoringApi = JobStorage.Current.GetMonitoringApi();
        var jobDetails = jobMonitoringApi.JobDetails(jobId);

        if (jobDetails == null) return NotFound(new APIResponse() { Message = "Задача с таким jobId не найдена" });

        var latestState = jobDetails.History.LastOrDefault();
        if (latestState == null)
            return Ok(new JobResultStatus() { Status = "Enqueued" });

        string state = latestState.StateName;

        if (state == "Succeeded" && latestState.Data.ContainsKey("Result"))
        {
            string result = latestState.Data["Result"];
            result = JsonConvert.DeserializeObject<string>(result)!;
            return Ok(new JobResultStatus() { Status = state, Result = result });
        }

        return Ok(new JobResultStatus() { Status = state, Result = null });
    }
}
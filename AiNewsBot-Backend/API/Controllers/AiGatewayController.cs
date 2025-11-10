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
    public async Task<IActionResult> SummarizePost([FromBody] PostCreateInfo postCreateInfo)
    {
        string jobId =
            _backgroundJobClient.Enqueue<AiService>(service => service.ProcessAiSummarizeAsync(postCreateInfo));
        return Ok(new APIResponse() { Data = new JobIdData() { JobId = jobId } });
    }

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts()
    {
        var posts = await _dbContext.Posts.ToListAsync();
        return Ok(new APIResponse() { Data = posts });
    }

    [HttpGet("posts/ids")]
    public async Task<IActionResult> GetPostIds()
    {
        var posts = await _dbContext.Posts.Select(p => p.PostId).ToListAsync();
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
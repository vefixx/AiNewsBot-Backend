using System.ClientModel;
using System.ClientModel.Primitives;
using AiNewsBot_Backend.API.Models;
using AiNewsBot_Backend.Core.Data.Contexts;
using AiNewsBot_Backend.Core.Data.Entities;
using AiNewsBot_Backend.Core.Helpers;
using AiNewsBot_Backend.Core.Models;
using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;
using OpenAI;
using OpenAI.Chat;
using OpenRouter.NET;
using Telegram.BotAPI;

namespace AiNewsBot_Backend;

public class Program
{
    public static void Main(string[] args)
    {
        DotNetEnv.Env.Load(".env");

        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
            logging.RequestBodyLogLimit = 256;
            logging.ResponseBodyLogLimit = 256;
        });
        
        builder.Services.AddHangfire(config =>
        {
            config.UseMemoryStorage();
        });

        builder.Services.AddDbContext<PostsContext>(options =>
        {
            options.UseSqlite($"Data Source=Core/Database/app.db");
        });

        builder.Services.AddMvc()
            .ConfigureApiBehaviorOptions(opt
                =>
            {
                opt.InvalidModelStateResponseFactory =
                    (context => new BadRequestObjectResult(
                        new APIResponse() { Message = "Некорректные данные запроса" }));
            });

        builder.Services.AddControllers(options =>
        {
            options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
        });

        TelegramBotClient botClient = new TelegramBotClient(DotNetEnv.Env.GetString("TELEGRAM_BOT_TOKEN"));
        OpenRouterClient openAiChatClient = new(DotNetEnv.Env.GetString("AI_KEY"));
        AiChatClientSettings aiChatClientSettings =
            JsonHelper.ReadJson<AiChatClientSettings>("AIChatClientSettings.json");

        builder.Services.AddSingleton(botClient);
        builder.Services.AddSingleton(openAiChatClient);
        builder.Services.AddSingleton(aiChatClientSettings);
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseHttpLogging();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHangfireDashboard();
        app.UseHangfireServer();

        app.Run();
    }
}
using System.ClientModel;
using System.ClientModel.Primitives;
using AiNewsBot_Backend.API.Models;
using AiNewsBot_Backend.Core.Helpers;
using AiNewsBot_Backend.Core.Models;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
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

        builder.Services.AddSingleton<TelegramBotClient>(botClient);
        builder.Services.AddSingleton<OpenRouterClient>(openAiChatClient);
        builder.Services.AddSingleton<AiChatClientSettings>(aiChatClientSettings);
        builder.Services.AddControllers();

        var app = builder.Build();

        app.UseHttpLogging();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
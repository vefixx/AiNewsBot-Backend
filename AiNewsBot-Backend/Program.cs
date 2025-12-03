using Hangfire;
using Hangfire.MemoryStorage;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.AspNetCore.Mvc.ModelBinding.Metadata;
using Microsoft.EntityFrameworkCore;

namespace AiNewsBot_Backend;

public class Program
{
    private const string DotenvPath = ".env";
    
    public static void Main(string[] args)
    {
        DotNetEnv.Env.Load(DotenvPath);

        #region Builder

        var builder = WebApplication.CreateBuilder(args);
        builder.WebHost.UseUrls("http://localhost:3232");
        
        // Logging
        builder.Services.AddHttpLogging(logging =>
        {
            logging.LoggingFields = HttpLoggingFields.All;
            logging.RequestBodyLogLimit = 256;
            logging.ResponseBodyLogLimit = 256;
        });
        
        // Hangfire
        builder.Services.AddHangfire(config =>
        {
            config.UseMemoryStorage();
        });
        
        // EFCore
        // ...
        
        // Controllers
        builder.Services.AddControllers(options =>
        {
            options.ModelMetadataDetailsProviders.Add(new SystemTextJsonValidationMetadataProvider());
        });
        builder.Services.AddControllers();
        
        // Singleton's & Scopes-

        #endregion

        #region App

        var app = builder.Build();

        app.UseHttpLogging();
        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();
        app.MapHangfireDashboard();
        app.UseHangfireServer();

        app.Run();

        #endregion
    }
}
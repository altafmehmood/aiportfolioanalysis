using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Collections;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Create early logger for startup debugging
var loggerFactory = LoggerFactory.Create(config => config.AddConsole().SetMinimumLevel(LogLevel.Information));
var startupLogger = loggerFactory.CreateLogger("Startup");

startupLogger.LogInformation("🚀 Starting ASP.NET Core application");
startupLogger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
startupLogger.LogInformation("Content Root: {ContentRoot}", builder.Environment.ContentRootPath);
startupLogger.LogInformation("Application Name: {ApplicationName}", builder.Environment.ApplicationName);

try
{
    startupLogger.LogInformation("📝 Configuring services...");

    // Log all environment variables for debugging
    startupLogger.LogInformation("Environment Variables:");
    foreach (DictionaryEntry env in Environment.GetEnvironmentVariables())
    {
        var key = env.Key?.ToString();
        var value = env.Value?.ToString();
        
        // Mask sensitive values
        if (key != null && (key.Contains("SECRET") || key.Contains("PASSWORD") || key.Contains("KEY")))
        {
            value = value?.Length > 0 ? "***MASKED***" : "(empty)";
        }
        
        startupLogger.LogInformation("  {Key} = {Value}", key, value);
    }

    // Configure for reverse proxy
    startupLogger.LogInformation("🔄 Configuring forwarded headers for reverse proxy");
    builder.Services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                                  Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
        options.KnownNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Add services to the container.
    startupLogger.LogInformation("📖 Adding OpenAPI services");
    builder.Services.AddOpenApi();

    // Add Authentication
    startupLogger.LogInformation("🔐 Configuring authentication...");
    
    var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
    var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
    
    startupLogger.LogInformation("Google OAuth Configuration:");
    startupLogger.LogInformation("  ClientId configured: {HasClientId}", !string.IsNullOrEmpty(googleClientId));
    startupLogger.LogInformation("  ClientSecret configured: {HasClientSecret}", !string.IsNullOrEmpty(googleClientSecret));
    
    // Also check environment variables
    var envClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENTID");
    var envClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENTSECRET");
    startupLogger.LogInformation("Environment Variables:");
    startupLogger.LogInformation("  GOOGLE_CLIENTID: {HasEnvClientId}", !string.IsNullOrEmpty(envClientId));
    startupLogger.LogInformation("  GOOGLE_CLIENTSECRET: {HasEnvClientSecret}", !string.IsNullOrEmpty(envClientSecret));

    // Validate Google OAuth configuration - allow missing in test environments
    var isDevelopment = builder.Environment.IsDevelopment();
    var isTest = builder.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase);
    
    startupLogger.LogInformation("Environment flags: Development={IsDevelopment}, Test={IsTest}", isDevelopment, isTest);

    if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
    {
        if (isDevelopment || isTest)
        {
            startupLogger.LogWarning("⚠️ Google OAuth credentials not configured - authentication will be disabled");
        }
        else
        {
            startupLogger.LogError("❌ Google OAuth configuration is required for production");
            startupLogger.LogError("Missing Authentication:Google:ClientId or Authentication:Google:ClientSecret");
            startupLogger.LogError("Please ensure GOOGLE_CLIENTID and GOOGLE_CLIENTSECRET are configured");
            throw new InvalidOperationException("Google OAuth configuration is required. Please configure Authentication:Google:ClientId and Authentication:Google:ClientSecret.");
        }
    }
    else
    {
        startupLogger.LogInformation("✅ Google OAuth credentials are configured");
    }

    // Configure authentication only if OAuth credentials are available
    bool hasOAuthCredentials = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);
    startupLogger.LogInformation("Has OAuth credentials: {HasOAuthCredentials}", hasOAuthCredentials);

    if (hasOAuthCredentials)
    {
        startupLogger.LogInformation("🔒 Setting up full authentication with Google OAuth");
        builder.Services.AddAuthentication(options =>
        {
            options.DefaultScheme = "Cookies";
            options.DefaultChallengeScheme = "Google";
        })
        .AddCookie("Cookies", options =>
        {
                options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Cookie.HttpOnly = true; // Secure cookies - use separate tokens for SPA if needed
            options.ExpireTimeSpan = TimeSpan.FromDays(30);
            options.SlidingExpiration = true;
        })
        .AddGoogle("Google", options =>
        {
            options.ClientId = googleClientId!;
            options.ClientSecret = googleClientSecret!;
            options.CallbackPath = "/signin-google";
            options.CorrelationCookie.SameSite = SameSiteMode.Lax;
            options.CorrelationCookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        });
        startupLogger.LogInformation("✅ Google OAuth authentication configured");
    }
    else
    {
        startupLogger.LogInformation("🔓 Setting up minimal authentication for test environments");
        // Add minimal authentication for test environments
        builder.Services.AddAuthentication()
            .AddCookie("Cookies");
    }

    startupLogger.LogInformation("📋 Adding authorization services");
    builder.Services.AddAuthorization();
    
    startupLogger.LogInformation("🏥 Adding health check services");
    builder.Services.AddHealthChecks()
        .AddCheck("self", () => HealthCheckResult.Healthy("Application is running"));

    // Get frontend URL configuration
    startupLogger.LogInformation("🌐 Configuring frontend URL and CORS...");
    var defaultFrontendUrl = builder.Environment.IsDevelopment() ? "http://localhost:4200" : "http://example.com";
    var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? defaultFrontendUrl;
    
    startupLogger.LogInformation("Frontend URL configuration:");
    startupLogger.LogInformation("  Default: {DefaultUrl}", defaultFrontendUrl);
    startupLogger.LogInformation("  Configured: {ConfiguredUrl}", frontendUrl);
    startupLogger.LogInformation("  Final: {FinalUrl}", frontendUrl);

    if (!Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontendUri))
    {
        startupLogger.LogError("❌ Invalid Frontend:BaseUrl configuration: '{FrontendUrl}'. Must be a valid absolute URL.", frontendUrl);
        throw new InvalidOperationException($"Invalid Frontend:BaseUrl configuration: '{frontendUrl}'. Must be a valid absolute URL.");
    }
    
    startupLogger.LogInformation("✅ Frontend URL validated: {FrontendUri}", frontendUri);
    
    startupLogger.LogInformation("🔗 Configuring CORS policy");
    builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(corsBuilder =>
        {
            if (builder.Environment.IsDevelopment())
            {
                startupLogger.LogInformation("Development CORS: Allowing {Origin} with full methods", frontendUrl);
                corsBuilder.WithOrigins(frontendUrl)
                          .WithMethods("GET", "POST", "PUT", "DELETE")
                          .WithHeaders("Content-Type", "Authorization")
                          .AllowCredentials();
            }
            else
            {
                startupLogger.LogInformation("Production CORS: Restrictive policy for {Origin}", frontendUrl);
                // Production: More restrictive CORS
                corsBuilder.WithOrigins(frontendUrl)
                          .WithMethods("GET", "POST")
                          .WithHeaders("Content-Type", "Authorization")
                          .AllowCredentials();
            }
        });
    });

    // Configure SPA services
    startupLogger.LogInformation("🖥️ Configuring SPA static files");
    builder.Services.AddSpaStaticFiles(configuration =>
    {
        configuration.RootPath = "wwwroot";
    });

    startupLogger.LogInformation("🏗️ Building application...");
    var app = builder.Build();
    startupLogger.LogInformation("✅ Application built successfully");

    // Use the same frontend URL configuration for endpoints
    var frontendUrlForEndpoints = frontendUrl;
    startupLogger.LogInformation("Using frontend URL for endpoints: {FrontendUrlForEndpoints}", frontendUrlForEndpoints);

    // Configure the HTTP request pipeline.
    startupLogger.LogInformation("🔧 Configuring HTTP request pipeline...");
    
    if (app.Environment.IsDevelopment())
    {
        startupLogger.LogInformation("📖 Adding OpenAPI for development");
        app.MapOpenApi();
    }

    startupLogger.LogInformation("🔄 Adding forwarded headers middleware");
    app.UseForwardedHeaders();
    
    startupLogger.LogInformation("🔒 Adding HTTPS redirection middleware");
    app.UseHttpsRedirection();
    
    startupLogger.LogInformation("🔗 Adding CORS middleware");
    app.UseCors();
    
    startupLogger.LogInformation("🔐 Adding authentication middleware");
    app.UseAuthentication();
    
    startupLogger.LogInformation("📋 Adding authorization middleware");
    app.UseAuthorization();

    var summaries = new[]
    {
        "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
    };

    startupLogger.LogInformation("🛤️ Mapping endpoints...");

    // Authentication endpoints
    app.MapGet("/api/auth/login", (HttpContext context) => 
    {
        return Results.Challenge(new AuthenticationProperties 
        { 
            RedirectUri = "/api/auth/callback" 
        }, new[] { "Google" });
    });

    app.MapGet("/api/auth/callback", (HttpContext context) =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            // User is authenticated, session is established
            // Redirect to dashboard without exposing user data in URL
            return Results.Redirect($"{frontendUrlForEndpoints}/dashboard");
        }
        
        return Results.Redirect($"{frontendUrlForEndpoints}/login?error=authentication_failed");
    });

    app.MapGet("/api/auth/user", (HttpContext context) =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return Results.Ok(new
            {
                Name = context.User.Identity.Name,
                Email = context.User.FindFirst(ClaimTypes.Email)?.Value,
                Picture = context.User.FindFirst("picture")?.Value
            });
        }
        return Results.Unauthorized();
    }).RequireAuthorization();

    app.MapPost("/api/auth/logout", (HttpContext context) =>
    {
        return Results.SignOut(new AuthenticationProperties
        {
            RedirectUri = frontendUrlForEndpoints
        }, new[] { "Cookies" });
    });

    // Health check endpoint
    startupLogger.LogInformation("🏥 Mapping health check endpoint");
    app.MapHealthChecks("/health");

    startupLogger.LogInformation("🌤️ Mapping weather forecast endpoint");
    app.MapGet("/weatherforecast", () =>
    {
        var forecast =  Enumerable.Range(1, 5).Select(index =>
            new WeatherForecast
            (
                DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                Random.Shared.Next(-20, 55),
                summaries[Random.Shared.Next(summaries.Length)]
            ))
            .ToArray();
        return forecast;
    })
    .WithName("GetWeatherForecast");

    startupLogger.LogInformation("📁 Configuring static files");
    app.UseStaticFiles();
    app.UseSpaStaticFiles();

    // Configure SPA
    startupLogger.LogInformation("🖥️ Configuring SPA routing");
    app.MapWhen(x => !x.Request.Path.Value?.StartsWith("/weatherforecast") == true && 
                     !x.Request.Path.Value?.StartsWith("/api/") == true, builder =>
    {
        builder.UseSpa(spa =>
        {
            spa.Options.SourcePath = "wwwroot";
            spa.Options.DefaultPageStaticFileOptions = new StaticFileOptions
            {
                FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(
                    Path.Combine(app.Environment.ContentRootPath, "wwwroot"))
            };

            if (app.Environment.IsDevelopment())
            {
                spa.UseProxyToSpaDevelopmentServer("http://localhost:4200");
            }
        });
    });

    startupLogger.LogInformation("🎯 All endpoints mapped successfully");
    startupLogger.LogInformation("🚀 Starting application server...");
    
    app.Run();
}
catch (Exception ex)
{
    startupLogger.LogCritical(ex, "💥 Fatal error during application startup");
    startupLogger.LogCritical("Exception Type: {ExceptionType}", ex.GetType().Name);
    startupLogger.LogCritical("Exception Message: {ExceptionMessage}", ex.Message);
    startupLogger.LogCritical("Stack Trace: {StackTrace}", ex.StackTrace);
    
    if (ex.InnerException != null)
    {
        startupLogger.LogCritical("Inner Exception: {InnerExceptionType} - {InnerExceptionMessage}", 
            ex.InnerException.GetType().Name, ex.InnerException.Message);
    }
    
    throw; // Re-throw to ensure the container fails fast
}

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
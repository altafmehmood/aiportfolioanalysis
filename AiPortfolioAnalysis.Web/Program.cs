using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using System.Collections;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Authentication
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var hasGoogleAuth = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

// Validate Google OAuth configuration in production
if (!builder.Environment.IsDevelopment() && !hasGoogleAuth)
{
    var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
    logger.LogError("Google OAuth configuration is required in production environment");
    logger.LogError("Missing Authentication:Google:ClientId or Authentication:Google:ClientSecret");
    logger.LogError("Please ensure GOOGLE_CLIENTID and GOOGLE_CLIENTSECRET secrets are configured in the deployment pipeline");
    throw new InvalidOperationException("Google OAuth configuration is required in production. Please configure Authentication:Google:ClientId and Authentication:Google:ClientSecret.");
}

var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    if (hasGoogleAuth)
    {
        options.DefaultChallengeScheme = "Google";
    }
})
.AddCookie("Cookies", options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true; // Secure cookies - use separate tokens for SPA if needed
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
});

if (hasGoogleAuth)
{
    authBuilder.AddGoogle("Google", options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.CallbackPath = "/signin-google";
    });
}

builder.Services.AddAuthorization();

// Get frontend URL configuration with validation and logging
// Use environment-aware fallback
var defaultFrontendUrl = builder.Environment.IsDevelopment() ? "http://localhost:4200" : "http://example.com";
var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? defaultFrontendUrl;
var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Configuration");
logger.LogInformation("=== Frontend URL Configuration ====");
logger.LogInformation("Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("Frontend:BaseUrl from config: {FrontendUrl}", frontendUrl);
logger.LogInformation("All Frontend configuration values:");
foreach (var config in builder.Configuration.AsEnumerable().Where(c => c.Key.StartsWith("Frontend")))
{
    logger.LogInformation("  {Key} = {Value}", config.Key, config.Value);
}
logger.LogInformation("Environment variables related to Frontend:");
foreach (DictionaryEntry envVar in Environment.GetEnvironmentVariables())
{
    var key = envVar.Key.ToString();
    if (key?.Contains("Frontend", StringComparison.OrdinalIgnoreCase) == true)
    {
        logger.LogInformation("  ENV {Key} = {Value}", key, envVar.Value);
    }
}
if (!Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontendUri))
{
    logger.LogError("Invalid Frontend:BaseUrl configuration: '{FrontendUrl}'. Must be a valid absolute URL.", frontendUrl);
    throw new InvalidOperationException($"Invalid Frontend:BaseUrl configuration: '{frontendUrl}'. Must be a valid absolute URL.");
}
logger.LogInformation("Final frontend URL: {FrontendUrl}", frontendUrl);
logger.LogInformation("======================================");
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        if (builder.Environment.IsDevelopment())
        {
            corsBuilder.WithOrigins(frontendUrl)
                      .WithMethods("GET", "POST", "PUT", "DELETE")
                      .WithHeaders("Content-Type", "Authorization")
                      .AllowCredentials();
        }
        else
        {
            // Production: More restrictive CORS
            corsBuilder.WithOrigins(frontendUrl)
                      .WithMethods("GET", "POST")
                      .WithHeaders("Content-Type", "Authorization")
                      .AllowCredentials();
        }
    });
});

// Configure SPA services
builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath = "wwwroot";
});

var app = builder.Build();

// Use the same frontend URL configuration for endpoints
var frontendUrlForEndpoints = frontendUrl;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

// Authentication endpoints
app.MapGet("/api/auth/login", (HttpContext context) => 
{
    app.Logger.LogInformation("=== OAuth Login Initiated ===");
    app.Logger.LogInformation("Request URL: {RequestUrl}", context.Request.GetDisplayUrl());
    app.Logger.LogInformation("Has Google Auth: {HasGoogleAuth}", hasGoogleAuth);
    app.Logger.LogInformation("Frontend URL for redirects: {FrontendUrl}", frontendUrlForEndpoints);
    
    if (!hasGoogleAuth)
    {
        app.Logger.LogWarning("Google authentication not configured");
        return Results.BadRequest(new { error = "Google authentication not configured" });
    }
    
    app.Logger.LogInformation("Initiating Google OAuth challenge with callback: /api/auth/callback");
    return Results.Challenge(new AuthenticationProperties 
    { 
        RedirectUri = "/api/auth/callback" 
    }, new[] { "Google" });
});

app.MapGet("/api/auth/callback", (HttpContext context) =>
{
    app.Logger.LogInformation("=== OAuth Callback Received ===");
    app.Logger.LogInformation("Request URL: {RequestUrl}", context.Request.GetDisplayUrl());
    app.Logger.LogInformation("User Authenticated: {IsAuthenticated}", context.User.Identity?.IsAuthenticated);
    app.Logger.LogInformation("Frontend URL for redirect: {FrontendUrl}", frontendUrlForEndpoints);
    
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var user = new
        {
            Name = context.User.Identity.Name,
            Email = context.User.FindFirst(ClaimTypes.Email)?.Value,
            Picture = context.User.FindFirst("picture")?.Value
        };
        
        app.Logger.LogInformation("User details - Name: {Name}, Email: {Email}", user.Name, user.Email);
        
        try
        {
            var userJson = JsonSerializer.Serialize(user);
            var redirectUrl = $"{frontendUrlForEndpoints}/dashboard?user={Uri.EscapeDataString(userJson)}";
            app.Logger.LogInformation("Redirecting to: {RedirectUrl}", redirectUrl);
            app.Logger.LogInformation("=================================");
            return Results.Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            app.Logger.LogError(ex, "Failed to serialize user data for redirect");
            var fallbackUrl = $"{frontendUrlForEndpoints}/dashboard";
            app.Logger.LogInformation("Fallback redirect to: {FallbackUrl}", fallbackUrl);
            app.Logger.LogInformation("=================================");
            return Results.Redirect(fallbackUrl);
        }
    }
    
    var errorUrl = $"{frontendUrlForEndpoints}/login?error=authentication_failed";
    app.Logger.LogWarning("Authentication failed, redirecting to: {ErrorUrl}", errorUrl);
    app.Logger.LogInformation("=================================");
    return Results.Redirect(errorUrl);
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
    app.Logger.LogInformation("=== OAuth Logout Initiated ===");
    app.Logger.LogInformation("Logout redirect URL: {LogoutUrl}", frontendUrlForEndpoints);
    app.Logger.LogInformation("=================================");
    return Results.SignOut(new AuthenticationProperties
    {
        RedirectUri = frontendUrlForEndpoints
    }, new[] { "Cookies" });
});

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

app.UseStaticFiles();
app.UseSpaStaticFiles();

// Configure SPA
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

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using System.Collections;
using System.Security.Claims;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

// Configure for reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                              Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Authentication
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

// Validate Google OAuth configuration is always required
if (string.IsNullOrEmpty(googleClientId) || string.IsNullOrEmpty(googleClientSecret))
{
    var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
    logger.LogError("Google OAuth configuration is required");
    logger.LogError("Missing Authentication:Google:ClientId or Authentication:Google:ClientSecret");
    logger.LogError("Please ensure GOOGLE_CLIENTID and GOOGLE_CLIENTSECRET are configured");
    throw new InvalidOperationException("Google OAuth configuration is required. Please configure Authentication:Google:ClientId and Authentication:Google:ClientSecret.");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "Cookies";
    options.DefaultChallengeScheme = "Google";
})
.AddCookie("Cookies", options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
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

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// Get frontend URL configuration
var defaultFrontendUrl = builder.Environment.IsDevelopment() ? "http://localhost:4200" : "http://example.com";
var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? defaultFrontendUrl;

if (!Uri.TryCreate(frontendUrl, UriKind.Absolute, out var frontendUri))
{
    throw new InvalidOperationException($"Invalid Frontend:BaseUrl configuration: '{frontendUrl}'. Must be a valid absolute URL.");
}
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

app.UseForwardedHeaders();
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
app.MapHealthChecks("/health");

// Note: ACME challenge endpoint no longer needed - Caddy handles this automatically

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
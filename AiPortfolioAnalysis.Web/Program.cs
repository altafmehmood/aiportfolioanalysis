using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Log configuration for debugging
var logger = LoggerFactory.Create(config => config.AddConsole()).CreateLogger("Startup");
logger.LogInformation("🔍 Configuration Debug:");
logger.LogInformation("  Environment: {Environment}", builder.Environment.EnvironmentName);
logger.LogInformation("  Authentication:Google:ClientId present: {HasClientId}", 
    !string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientId"]));
logger.LogInformation("  Authentication:Google:ClientSecret present: {HasClientSecret}", 
    !string.IsNullOrEmpty(builder.Configuration["Authentication:Google:ClientSecret"]));
logger.LogInformation("  Frontend:BaseUrl: {FrontendUrl}", 
    builder.Configuration["Frontend:BaseUrl"] ?? "(not set)");

// Configure for reverse proxy
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// Add OpenAPI
builder.Services.AddOpenApi();

// Configure Authentication
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];

bool hasOAuthCredentials = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

// Allow missing OAuth in Test/Development environments
var isTestOrDev = builder.Environment.IsDevelopment() || 
                  builder.Environment.EnvironmentName.Equals("Test", StringComparison.OrdinalIgnoreCase);

if (hasOAuthCredentials)
{
    builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "Google";
    })
    .AddCookie("Cookies", options =>
    {
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? 
            CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
        options.Cookie.HttpOnly = true;
    })
    .AddGoogle("Google", options =>
    {
        options.ClientId = googleClientId!;
        options.ClientSecret = googleClientSecret!;
        options.CallbackPath = "/signin-google";
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = builder.Environment.IsDevelopment() ? 
            CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    });
}
else if (!isTestOrDev)
{
    throw new InvalidOperationException("Google OAuth configuration is required for Production environment.");
}
else
{
    // Test/Development with minimal auth
    builder.Services.AddAuthentication().AddCookie("Cookies");
}

builder.Services.AddAuthorization();
builder.Services.AddHealthChecks();

// Get frontend URL - workflows set this dynamically
var frontendUrl = builder.Configuration["Frontend:BaseUrl"];
if (string.IsNullOrEmpty(frontendUrl))
{
    if (builder.Environment.IsDevelopment())
    {
        frontendUrl = "http://localhost:4200";
    }
    else
    {
        throw new InvalidOperationException("Frontend:BaseUrl configuration is required for non-Development environments.");
    }
}

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(corsBuilder =>
    {
        corsBuilder.WithOrigins(frontendUrl)
                  .WithMethods("GET", "POST")
                  .WithHeaders("Content-Type", "Authorization")
                  .AllowCredentials();
    });
});

var app = builder.Build();

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseForwardedHeaders();
app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

// Authentication endpoints - only if OAuth is configured
if (hasOAuthCredentials)
{
    app.MapGet("/api/auth/login", () => 
        Results.Challenge(new AuthenticationProperties { RedirectUri = "/api/auth/callback" }, ["Google"]));

    app.MapGet("/api/auth/callback", (HttpContext context) =>
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            return Results.Redirect($"{frontendUrl}/dashboard");
        }
        return Results.Redirect($"{frontendUrl}/login?error=authentication_failed");
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

    app.MapPost("/api/auth/logout", () =>
        Results.SignOut(new AuthenticationProperties { RedirectUri = frontendUrl }, ["Cookies"]));
}

// Health check
app.MapHealthChecks("/health");

// API status endpoint
app.MapGet("/", () => Results.Ok(new { 
    message = "AI Portfolio Analysis API", 
    status = "running",
    environment = app.Environment.EnvironmentName,
    hasOAuth = hasOAuthCredentials
}));

app.Run();
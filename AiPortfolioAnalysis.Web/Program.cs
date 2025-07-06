using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Add Authentication
var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
var hasGoogleAuth = !string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret);

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
    options.Cookie.SecurePolicy = app.Environment.IsDevelopment() 
        ? CookieSecurePolicy.SameAsRequest 
        : CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = false; // Allow JavaScript access for SPA
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

// Add CORS
var frontendUrl = builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";
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

// Get frontend URL for use in endpoints
var frontendUrlForEndpoints = app.Configuration["Frontend:BaseUrl"] ?? "http://localhost:4200";

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
app.MapGet("/api/auth/login", () => 
{
    if (!hasGoogleAuth)
    {
        return Results.BadRequest(new { error = "Google authentication not configured" });
    }
    return Results.Challenge(new AuthenticationProperties 
    { 
        RedirectUri = "/api/auth/callback" 
    }, new[] { "Google" });
});

app.MapGet("/api/auth/callback", (HttpContext context) =>
{
    if (context.User.Identity?.IsAuthenticated == true)
    {
        var user = new
        {
            Name = context.User.Identity.Name,
            Email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value,
            Picture = context.User.FindFirst("picture")?.Value
        };
        return Results.Redirect($"{frontendUrlForEndpoints}/dashboard?user={Uri.EscapeDataString(System.Text.Json.JsonSerializer.Serialize(user))}");
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
            Email = context.User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value,
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
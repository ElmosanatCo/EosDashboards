using System.Threading.RateLimiting;
using EosDashboards.Api.Auth;
using EosDashboards.Api.Errors;
using EosDashboards.Api.Preferences;
using EosDashboards.Api.Security;
using EosDashboards.Application.Abstractions;
using EosDashboards.Application.Auth;
using EosDashboards.Application.Preferences;
using EosDashboards.Infrastructure;
using EosDashboards.Infrastructure.Persistence;
using EosDashboards.Infrastructure.Sms;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ExceptionHandler>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<ApiSecurityOptions>()
    .Bind(builder.Configuration.GetSection(ApiSecurityOptions.SectionName))
    .ValidateOnStart();
builder.Services.AddSingleton<IValidateOptions<ApiSecurityOptions>, ApiSecurityOptionsValidator>();

builder.Services.AddScoped<ICorrelationContext, HttpCorrelationContext>();
builder.Services.AddScoped<RefreshCookieService>();
builder.Services.AddScoped<TrustedOriginFilter>();
builder.Services.AddScoped<StartSignIn>();
builder.Services.AddScoped<VerifyOtp>();
builder.Services.AddScoped<StartPasswordReset>();
builder.Services.AddScoped<CompletePasswordReset>();
builder.Services.AddScoped<ChangePassword>();
builder.Services.AddScoped<RefreshSession>();
builder.Services.AddScoped<Logout>();
builder.Services.AddScoped<GetMyPreferences>();
builder.Services.AddScoped<UpdateMyPreferences>();
builder.Services.AddScoped<IAuthorizationHandler, SessionAuthorizationHandler>();

var authentication = builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.MapInboundClaims = false);
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<TokenValidationParameters>((options, validationParameters) =>
        options.TokenValidationParameters = validationParameters);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ActiveUser", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new ActiveSessionRequirement());
    });
    options.AddPolicy("SystemAdministrator", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.AddRequirements(new SystemAdministratorRequirement());
    });
});

var allowedOrigins = builder.Configuration
    .GetSection($"{ApiSecurityOptions.SectionName}:AllowedOrigins")
    .Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(allowedOrigins)
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials()));

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth-sensitive", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

var app = builder.Build();

app.UseExceptionHandler();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" }));
app.MapGet("/health/ready", async (
    EosDashboardDbContext database,
    IOptions<SmsOptions> smsOptions,
    CancellationToken cancellationToken) =>
{
    try
    {
        _ = smsOptions.Value;
        return await database.Database.CanConnectAsync(cancellationToken)
            ? Results.Ok(new { status = "ready" })
            : Results.Json(new { status = "not_ready" }, statusCode: 503);
    }
    catch
    {
        return Results.Json(new { status = "not_ready" }, statusCode: 503);
    }
});
app.MapAuthEndpoints();
app.MapPreferenceEndpoints();

app.Run();

public partial class Program;

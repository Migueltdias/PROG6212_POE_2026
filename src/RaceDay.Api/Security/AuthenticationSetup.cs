using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RaceDay.Api.Models;

namespace RaceDay.Api.Security;

public sealed class SessionEndedException(string message) : Exception(message);

public static class AuthenticationSetup
{
    public static IServiceCollection AddRaceDayAuthentication(this IServiceCollection services)
    {
        services.AddOptions<JwtSettings>()
            .BindConfiguration(JwtSettings.SectionName)
            .PostConfigure<IHostEnvironment>((settings, environment) =>
            {
                if (string.IsNullOrWhiteSpace(settings.SigningKey) && environment.IsDevelopment())
                {
                    settings.SigningKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
                }
            })
            .Validate(s => Encoding.UTF8.GetByteCount(s.SigningKey) >= 32,
                "Jwt:SigningKey must be at least 32 characters. Set it with dotnet user-secrets or the Jwt__SigningKey environment variable.")
            .ValidateOnStart();

        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<ISessionStore, InMemorySessionStore>();
        services.AddSingleton<IPasswordHasher<User>, PasswordHasher<User>>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtSettings>, TimeProvider>((options, jwtOptions, clock) =>
            {
                var jwt = jwtOptions.Value;

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwt.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwt.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = TokenService.CreateSigningKey(jwt.SigningKey),
                    ValidateLifetime = true,
                    LifetimeValidator = (notBefore, expires, _, _) =>
                        TokenService.IsWithinLifetime(notBefore, expires, clock.GetUtcNow().UtcDateTime),
                    NameClaimType = RaceDayClaims.Email,
                    RoleClaimType = RaceDayClaims.Role
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = ValidateSession,
                    OnChallenge = WriteUnauthorized,
                    OnForbidden = WriteForbidden
                };
            });

        services.AddAuthorization(options =>
            options.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());
        return services;
    }

    private static Task ValidateSession(TokenValidatedContext context)
    {
        var store = context.HttpContext.RequestServices.GetRequiredService<ISessionStore>();
        var principal = context.Principal!;
        var sessionId = principal.GetSessionId();
        var session = sessionId is null ? null : store.GetActive(sessionId);

        if (session is null || session.UserId != principal.TryGetUserId() || session.Role != principal.GetRole())
        {
            context.Fail(new SessionEndedException(
                "Your session has ended because you logged out, changed your password or it expired. Log in again."));
            return Task.CompletedTask;
        }

        store.Touch(session);
        return Task.CompletedTask;
    }

    private static async Task WriteUnauthorized(JwtBearerChallengeContext context)
    {
        context.HandleResponse();

        var detail = context.AuthenticateFailure switch
        {
            null => "This endpoint requires you to be logged in. Send the token from POST /api/auth/login as a Bearer token.",
            SessionEndedException ended => ended.Message,
            SecurityTokenExpiredException or SecurityTokenInvalidLifetimeException =>
                "The access token has expired. Call POST /api/auth/refresh or log in again.",
            _ => "The access token is not valid."
        };

        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.Headers.WWWAuthenticate = "Bearer";
        await WriteProblem(context.HttpContext, StatusCodes.Status401Unauthorized, "Unauthorized", detail);
    }

    private static Task WriteForbidden(ForbiddenContext context)
    {
        var role = context.HttpContext.User.GetRole() ?? "unknown";
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return WriteProblem(context.HttpContext, StatusCodes.Status403Forbidden, "Forbidden",
            $"Your role ({role}) is not allowed to use this endpoint.");
    }

    private static async Task WriteProblem(HttpContext httpContext, int status, string title, string detail)
    {
        var problems = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title, Detail = detail }
        });
    }
}

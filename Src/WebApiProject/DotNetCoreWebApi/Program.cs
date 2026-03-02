using DotNetCoreWebApi.Authorization.Handlers;
using DotNetCoreWebApi.Authorization.Requirements;
using DotNetCoreWebApi.Authorization.Services;
using DotNetCoreWebApi.Middleware;
using DotNetLibrary.Core.NotificationProcessor.Contracts;
using DotNetLibrary.Core.NotificationProcessor.Models;
using DotNetLibrary.Core.NotificationProcessor.Services;
using DotNetLibrary.Data;
using DotNetLibrary.Data.Contracts;
using DotNetLibrary.Data.Repository;
using DotNetLibrary.Domain.Contracts;
using DotNetLibrary.Domain.Services;
using DotNetLibrary.Shared.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using Scalar.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);
builder.Services.Configure<OAuthOptions>(builder.Configuration.GetSection("OAuth"));
builder.Services.Configure<NotificationOptions>(builder.Configuration.GetSection("Notification"));

var oauthOptions = builder.Configuration.GetSection("OAuth").Get<OAuthOptions>() ?? new OAuthOptions();

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<ApplicationDbContext>();
    })
    .AddServer(options =>
    {
        options.SetAuthorizationEndpointUris("/connect/authorize")
            .SetEndSessionEndpointUris("/connect/logout")
            .SetIntrospectionEndpointUris("/connect/introspect")
            .SetTokenEndpointUris("/connect/token");

        options.AllowAuthorizationCodeFlow()
            .RequireProofKeyForCodeExchange();
        options.AllowPasswordFlow();
        options.AllowRefreshTokenFlow();
        options.AllowClientCredentialsFlow();
        options.AllowCustomFlow("external");
        options.AcceptAnonymousClients();

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(oauthOptions.AccessTokenExpirationMinutes));
        options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(oauthOptions.RefreshTokenExpirationMinutes));
        options.AddDevelopmentSigningCertificate();
        options.AddEphemeralEncryptionKey();

        options.RegisterScopes(Scopes.OpenId, Scopes.OfflineAccess, Scopes.Email, Scopes.Profile, Scopes.Roles, oauthOptions.DefaultScope);

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableTokenEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    options.AddPolicy(PermissionCodes.UserRead, policy => policy.RequireClaim("permission", PermissionCodes.UserRead));
    options.AddPolicy(PermissionCodes.UserWrite, policy => policy.RequireClaim("permission", PermissionCodes.UserWrite));
    options.AddPolicy(PermissionCodes.RoleRead, policy => policy.RequireClaim("permission", PermissionCodes.RoleRead));
    options.AddPolicy(PermissionCodes.RoleWrite, policy => policy.RequireClaim("permission", PermissionCodes.RoleWrite));

    options.AddPolicy(AuthorizationPolicyNames.AdminOnly, policy => policy.RequireRole("Admin"));
    options.AddPolicy(AuthorizationPolicyNames.AdminOrSelf, policy => policy.Requirements.Add(new AdminOrSelfRequirement()));
});

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHttpClient(nameof(EmailNotificationChannel));

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserOtpRepository, UserOtpRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<IPermissionRepository, PermissionRepository>();
builder.Services.AddScoped<IRolePermissionMapRepository, RolePermissionMapRepository>();
builder.Services.AddScoped<INotificationChannel, EmailNotificationChannel>();
builder.Services.AddScoped<INotificationChannel, SmsNotificationChannel>();
builder.Services.AddScoped<INotificationProcessor, NotificationProcessor>();
builder.Services.AddScoped<IUserManagementService, UserManagementService>();
builder.Services.AddScoped<ILookupService, LookupService>();
builder.Services.AddScoped<IRolePermissionService, RolePermissionService>();
builder.Services.AddScoped<ExternalIdTokenValidator>();
builder.Services.AddScoped<IUserClaimsService, UserClaimsService>();
builder.Services.AddSingleton<IAuthorizationHandler, AdminOrSelfHandler>();
builder.Services.AddScoped<Microsoft.AspNetCore.Identity.IPasswordHasher<DotNetLibrary.Data.Entities.User>, Microsoft.AspNetCore.Identity.PasswordHasher<DotNetLibrary.Data.Entities.User>>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    // Custom route with HttpContext access
    app.MapScalarApiReference("/api-specs", (options, httpContext) =>
    {
        var isAdmin = httpContext.User.IsInRole("Admin");
        options.WithTitle(isAdmin ? "Admin API" : "Public API");
    }).AllowAnonymous();
}

app.UseGlobalExceptionHandling();
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Document the OpenIddict token endpoint for OpenAPI/Scalar without changing behavior
app.MapControllers();

app.Run();

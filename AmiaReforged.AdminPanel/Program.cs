using System.Security.Claims;
using AmiaReforged.AdminPanel.Components;
using AmiaReforged.AdminPanel.Configuration;
using AmiaReforged.AdminPanel.Hubs;
using AmiaReforged.AdminPanel.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Serilog;

namespace AmiaReforged.AdminPanel;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Load environment variables from .env file if it exists
        if (File.Exists(".env"))
        {
            Env.Load(".env");
        }

        // Configure Serilog early for startup logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("Amia Admin Panel initializing...");

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog();

            // Bind configuration
            var adminConfig = new AdminPanelConfig();
            builder.Configuration.GetSection("AdminPanel").Bind(adminConfig);

            // Override from environment variables
            adminConfig.DefaultAdminUsername = Environment.GetEnvironmentVariable("ADMIN_USERNAME") ?? adminConfig.DefaultAdminUsername;
            adminConfig.DefaultAdminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? adminConfig.DefaultAdminPassword;
            adminConfig.DevUsername = Environment.GetEnvironmentVariable("DEV_USERNAME") ?? adminConfig.DevUsername;
            adminConfig.DevPassword = Environment.GetEnvironmentVariable("DEV_PASSWORD") ?? adminConfig.DevPassword;
            adminConfig.DiscordWebhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") ?? adminConfig.DiscordWebhookUrl;
            adminConfig.DockerSocketPath = Environment.GetEnvironmentVariable("DOCKER_SOCKET") ?? adminConfig.DockerSocketPath;

            builder.Services.AddSingleton(adminConfig);

            // WorldEngine API — a bare named HttpClient (base address set per-request from the endpoint service)
            builder.Services.AddHttpClient("WorldEngine", client =>
            {
                client.Timeout = TimeSpan.FromSeconds(15);
            });
            builder.Services.AddSingleton<IWorldEngineEndpointService, WorldEngineEndpointService>();
            builder.Services.AddScoped<EncounterApiService>();
            builder.Services.AddScoped<ItemApiService>();
            builder.Services.AddScoped<ResourceNodeApiService>();

            // Cookie Authentication
            builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Account/Login";
                    options.LogoutPath = "/Account/Logout";
                    options.AccessDeniedPath = "/Account/Login";
                    options.ExpireTimeSpan = TimeSpan.FromHours(24);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAuthorization();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddCascadingAuthenticationState();

            // Services
            builder.Services.AddSingleton<IDockerMonitorService, DockerMonitorService>();
            builder.Services.AddSingleton<IMonitoringConfigService, MonitoringConfigService>();
            builder.Services.AddHostedService<ContainerHealthMonitor>();

            // SignalR
            builder.Services.AddSignalR();

            // Blazor
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error", createScopeForErrors: true);
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHub<ContainerLogHub>("/hubs/logs");

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            // Login endpoint - handles form POST for cookie authentication
            app.MapPost("/api/auth/login", async (HttpContext context, AdminPanelConfig config) =>
            {
                var form = await context.Request.ReadFormAsync();
                var username = form["username"].ToString();
                var password = form["password"].ToString();
                var rememberMe = form["rememberMe"] == "true";
                var returnUrl = form["returnUrl"].ToString();

                string? role = null;

                // Check Admin credentials
                if (username == config.DefaultAdminUsername && password == config.DefaultAdminPassword)
                {
                    role = "Admin";
                }
                // Check Dev credentials
                else if (username == config.DevUsername && password == config.DevPassword)
                {
                    role = "Dev";
                }

                if (role != null)
                {
                    var claims = new List<Claim>
                    {
                        new(ClaimTypes.Name, username),
                        new(ClaimTypes.Role, role)
                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = rememberMe,
                        ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24)
                    };

                    await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);
                    return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
                }

                return Results.Redirect($"/Account/Login?error=invalid&returnUrl={Uri.EscapeDataString(returnUrl ?? "/")}");
            }).AllowAnonymous();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Admin Panel terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}

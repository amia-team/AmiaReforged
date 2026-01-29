using AmiaReforged.AdminPanel.Components;
using AmiaReforged.AdminPanel.Configuration;
using AmiaReforged.AdminPanel.Data;
using AmiaReforged.AdminPanel.Hubs;
using AmiaReforged.AdminPanel.Services;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
            adminConfig.DefaultAdminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? adminConfig.DefaultAdminPassword;
            adminConfig.DiscordWebhookUrl = Environment.GetEnvironmentVariable("DISCORD_WEBHOOK_URL") ?? adminConfig.DiscordWebhookUrl;
            adminConfig.DockerSocketPath = Environment.GetEnvironmentVariable("DOCKER_SOCKET") ?? adminConfig.DockerSocketPath;

            builder.Services.AddSingleton(adminConfig);

            // Database
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? Environment.GetEnvironmentVariable("ADMIN_DB_CONNECTION")
                ?? "Host=localhost;Database=admin_panel;Username=amia;Password=amia";

            builder.Services.AddDbContextFactory<AdminPanelDbContext>(options =>
                options.UseNpgsql(connectionString));
            builder.Services.AddDbContext<AdminPanelDbContext>(options =>
                options.UseNpgsql(connectionString));

            // Identity
            builder.Services.AddIdentity<AdminUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                options.Lockout.MaxFailedAccessAttempts = 5;
            })
            .AddEntityFrameworkStores<AdminPanelDbContext>()
            .AddDefaultTokenProviders();

            builder.Services.ConfigureApplicationCookie(options =>
            {
                options.LoginPath = "/Account/Login";
                options.LogoutPath = "/Account/Logout";
                options.AccessDeniedPath = "/Account/AccessDenied";
                options.ExpireTimeSpan = TimeSpan.FromHours(24);
                options.SlidingExpiration = true;
            });

            // Services
            builder.Services.AddSingleton<IDockerMonitorService, DockerMonitorService>();
            builder.Services.AddScoped<IMonitoringConfigService, MonitoringConfigService>();
            builder.Services.AddHostedService<ContainerHealthMonitor>();

            // SignalR
            builder.Services.AddSignalR();

            // Blazor
            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var app = builder.Build();

            // Migrate database and seed admin user
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AdminPanelDbContext>();
                await context.Database.MigrateAsync();

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AdminUser>>();
                var adminUser = await userManager.FindByNameAsync(adminConfig.DefaultAdminUsername);
                if (adminUser == null)
                {
                    adminUser = new AdminUser
                    {
                        UserName = adminConfig.DefaultAdminUsername,
                        Email = adminConfig.DefaultAdminEmail,
                        EmailConfirmed = true
                    };
                    var result = await userManager.CreateAsync(adminUser, adminConfig.DefaultAdminPassword);
                    if (result.Succeeded)
                    {
                        Log.Information("Created default admin user: {Username}", adminConfig.DefaultAdminUsername);
                    }
                    else
                    {
                        Log.Error("Failed to create admin user: {Errors}", string.Join(", ", result.Errors.Select(e => e.Description)));
                    }
                }
            }

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

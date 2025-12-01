using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using CoffeeShopSimulation.Data;
using CoffeeShopSimulation.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. configuration

// get the connection string from environment variable first (for production), then appsettings.json
// Prioritize DATABASE_URL for PostgreSQL on Render
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL") // Render PostgreSQL provides this first
?? Environment.GetEnvironmentVariable("DATABASE_CONNECTION_STRING")
?? builder.Configuration.GetConnectionString("DefaultConnection") // Fall back to appsettings.json for local dev
?? "Data Source=coffee_loyalty.db";

// Determine which database provider to use
// Check the connection string format first - if it looks like PostgreSQL, use PostgreSQL
bool usePostgreSQL = !string.IsNullOrEmpty(connectionString) && (
                         connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
                         connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
                         connectionString.StartsWith("Host=", StringComparison.OrdinalIgnoreCase));

// Log which database provider we're using (for debugging)
Console.WriteLine($"=== Database Configuration ===");
Console.WriteLine($"Using PostgreSQL: {usePostgreSQL}");
Console.WriteLine($"Connection String Preview: {(string.IsNullOrEmpty(connectionString) ? "EMPTY" : connectionString.Substring(0, Math.Min(50, connectionString.Length)) + "...")}");

// 2. add services

// add database context for entity framework core
// Automatically switch between SQLite (local) and PostgreSQL (production)
builder.Services.AddDbContext<DatabaseContext>(options =>
{
    if (usePostgreSQL)
    {
        // PostgreSQL connection (Render)
        // Handle Render's DATABASE_URL format: postgres://user:pass@host:port/dbname or postgresql://...
        string postgresConnection = connectionString;
        
        if (connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
            connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                // Parse Render's DATABASE_URL format: postgresql://user:pass@host:port/dbname
                var uri = new Uri(connectionString);
                
                // Extract user info (username:password)
                var userInfoParts = uri.UserInfo.Split(new[] { ':' }, 2);
                if (userInfoParts.Length == 2)
                {
                    var username = Uri.UnescapeDataString(userInfoParts[0]);
                    var password = Uri.UnescapeDataString(userInfoParts[1]);
                    var database = uri.LocalPath.TrimStart('/');
                    var port = uri.Port > 0 ? uri.Port : 5432;
                    
                    postgresConnection = $"Host={uri.Host};Port={port};Database={database};Username={username};Password={password};SSL Mode=Require;Trust Server Certificate=true;";
                    Console.WriteLine($"Parsed PostgreSQL connection: Host={uri.Host}, Port={port}, Database={database}");
                }
            }
            catch (Exception ex)
            {
                // Log error but continue - the connection string might already be in Npgsql format
                Console.WriteLine($"ERROR: Failed to parse PostgreSQL connection string: {ex.Message}");
                Console.WriteLine($"Connection string: {connectionString?.Substring(0, Math.Min(100, connectionString?.Length ?? 0))}");
            }
        }
        
        Console.WriteLine("Configuring database context to use PostgreSQL (Npgsql)");
        options.UseNpgsql(postgresConnection);
    }
    else
    {
        // SQLite connection (local development)
        Console.WriteLine("Configuring database context to use SQLite");
        options.UseSqlite(connectionString);
    }
});

// configure identity system (login/register/user management)
builder.Services.AddDefaultIdentity<LoyaltyUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<DatabaseContext>();

// Configure cookies to work behind proxy (Render, Heroku, etc.)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.HttpOnly = true;
});

// Configure forwarding for proxy environments
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | 
                                Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

// add a no-op email sender (required by Identity but not used since email confirmation is disabled)
builder.Services.AddTransient<IEmailSender, NoOpEmailSender>();

// add MVC controllers and views (mvc is the model view controller framework)
builder.Services.AddControllersWithViews();

// add support for razor pages (where identity UI lives)
builder.Services.AddRazorPages();

var app = builder.Build();

// 3. configure HTTP request pipeline

// Use forwarded headers for proxy environments (Render, Heroku, etc.)
app.UseForwardedHeaders();

// Run database migrations on startup (both dev and production)
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Starting database migration...");
        
        // Check if database can be connected
        if (db.Database.CanConnect())
        {
            logger.LogInformation("Database connection successful. Applying migrations...");
            db.Database.Migrate();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogWarning("Cannot connect to database. Migrations will be skipped.");
        }
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database. Error: {Error}", ex.Message);
        
        // Log the full exception details for debugging
        if (ex.InnerException != null)
        {
            logger.LogError("Inner exception: {InnerError}", ex.InnerException.Message);
        }
    }
}

// check for development environment
if (app.Environment.IsDevelopment())
{
    // enable detailed error pages during development
    app.UseDeveloperExceptionPage(); 
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // Only use HSTS in production if not behind a proxy
    // app.UseHsts(); // Commented out for Render compatibility
}

// Only redirect to HTTPS if not behind a proxy (Render handles HTTPS)
// app.UseHttpsRedirection(); // Commented out for Render compatibility
app.UseStaticFiles(); // enables serving CSS, JS, images from wwwroot

app.UseRouting();

// user login/session tracking must be here and in this order
app.UseAuthentication();
app.UseAuthorization();

// map the default login/register pages built into identity
app.MapRazorPages(); 

// map API controllers
app.MapControllers();

// map the controllers for your custom web pages (Home, API, etc.)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Landing}/{id?}"); // start at landing page

app.Run();

// no-op email sender implementation (required by Identity but not used)
public class NoOpEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // do nothing - email confirmation is disabled
        return Task.CompletedTask;
    }
}
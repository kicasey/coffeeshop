using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http;
using CoffeeShopSimulation.Data;
using CoffeeShopSimulation.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. configuration

// get the connection string from appsettings.json
// Ignore DATABASE_URL if set (that was for PostgreSQL)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
?? "Data Source=./coffee_loyalty.db";

// 2. add services

// add database context for entity framework core
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite(connectionString));

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
// SQLite will create the database file automatically if it doesn't exist
using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        
        logger.LogInformation("Applying database migrations...");
        
        // Migrate will create the database if it doesn't exist
        db.Database.Migrate();
        
        logger.LogInformation("Database migrations completed successfully.");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database.");
        
        // Don't crash the app - log and continue
        // The app might still work if the database already exists
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
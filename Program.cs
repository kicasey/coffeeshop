using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using CoffeeShopSimulation.Data;
using CoffeeShopSimulation.Models;

var builder = WebApplication.CreateBuilder(args);

// --- 1. CONFIGURATION ---

// Get the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                       ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// --- 2. ADD SERVICES ---

// Add Database Context for Entity Framework Core (using the DatabaseContext name you chose)
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite(connectionString));

// Configure Identity System (Login/Register/User Management)
builder.Services.AddDefaultIdentity<LoyaltyUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<DatabaseContext>();

// Add a no-op email sender (required by Identity but not used since email confirmation is disabled)
builder.Services.AddTransient<IEmailSender, NoOpEmailSender>();

// Add MVC Controllers and Views
builder.Services.AddControllersWithViews();

// Add support for Razor Pages (where Identity UI lives)
builder.Services.AddRazorPages();

var app = builder.Build();

// --- 3. CONFIGURE HTTP REQUEST PIPELINE ---

// Check for development environment
if (app.Environment.IsDevelopment())
{
    // Enable detailed error pages during development
    app.UseDeveloperExceptionPage(); 
    
    // Optional: Auto-migrate database on startup in development. 
    // This can help ensure the DB is always ready.
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<DatabaseContext>();
        db.Database.Migrate();
    }
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Enables serving CSS, JS, images from wwwroot

app.UseRouting();

// CRITICAL: These two lines must be here and in this order for user login/session tracking
app.UseAuthentication();
app.UseAuthorization();

// Map the default login/register pages built into Identity
app.MapRazorPages(); 

// Map API controllers
app.MapControllers();

// Map the controllers for your custom web pages (Home, API, etc.)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// No-op email sender implementation (required by Identity but not used)
public class NoOpEmailSender : IEmailSender
{
    public Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        // Do nothing - email confirmation is disabled
        return Task.CompletedTask;
    }
}
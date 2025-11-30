using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using CoffeeShopSimulation.Data;
using CoffeeShopSimulation.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. configuration

// get the connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// 2. add services

// add database context for entity framework core
builder.Services.AddDbContext<DatabaseContext>(options =>
    options.UseSqlite(connectionString));

// configure identity system (login/register/user management)
builder.Services.AddDefaultIdentity<LoyaltyUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddEntityFrameworkStores<DatabaseContext>();

// add a no-op email sender (required by Identity but not used since email confirmation is disabled)
builder.Services.AddTransient<IEmailSender, NoOpEmailSender>();

// add MVC controllers and views (mvc is the model view controller framework)
builder.Services.AddControllersWithViews();

// add support for razor pages (where identity UI lives)
builder.Services.AddRazorPages();

var app = builder.Build();

// 3. configure HTTP request pipeline

// check for development environment
if (app.Environment.IsDevelopment())
{
    // enable detailed error pages during development
    app.UseDeveloperExceptionPage(); 
    
    // auto-migrate database on startup in development
    // ensures the DB is always ready
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
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using CoffeeShopSimulation.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using CoffeeShopSimulation.Data;
using Microsoft.Extensions.DependencyInjection;

namespace CoffeeShopSimulation.Controllers
{
    public class HomeController : Controller // controller is the class that handles the requests and returns the views
    {
        private readonly UserManager<LoyaltyUser> _userManager;
        private readonly DatabaseContext _context;
        
        public HomeController(UserManager<LoyaltyUser> userManager, DatabaseContext context) //constructor
        {
            _userManager = userManager;
            _context = context;
        }
        
        public async Task<IActionResult> Index(string redeem = null) 
        {
            // check if user is logged in and pass birthday info
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId); // find the user by id
                if (user != null)
                {
                    ViewBag.IsBirthday = user.IsBirthdayToday() && !user.HasUsedBirthdayDiscountToday();
                    ViewBag.IsBirthdayButRedeemed = user.IsBirthdayToday() && user.HasUsedBirthdayDiscountToday();
                    ViewBag.HasRedeemDiscount = !string.IsNullOrEmpty(redeem);
                    ViewBag.RedeemSize = redeem;
                }
            }
            return View();
        }
        
        public async Task<IActionResult> Landing()
        {
            // landing page - show different content for logged in vs guest
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    ViewBag.IsLoggedIn = true;
                    ViewBag.Points = user.LoyaltyPoints;
                    return View();
                }
            }
            ViewBag.IsLoggedIn = false;
            return View();
        }
        
        public async Task<IActionResult> OrderComplete(int? orderId, int? points, int? pointsEarned, string? total, bool? hasDiscount = false, string? discountType = null)
        {
            // Order completion page
            ViewBag.OrderId = orderId ?? 0;
            ViewBag.PointsEarned = pointsEarned ?? 0; // Points earned from this order only
            ViewBag.Total = total ?? "$0.00";
            ViewBag.HasDiscount = hasDiscount ?? false;
            ViewBag.DiscountType = discountType;
            
            // Check if user is actually logged in (not a guest)
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            ViewBag.IsLoggedIn = !string.IsNullOrEmpty(userId);
            
            // Get current total points from database (this is the actual total after the order)
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    ViewBag.Points = user.LoyaltyPoints; // Current total points from database
                }
                else
                {
                    ViewBag.Points = points ?? 0; // Fallback to passed value
                }
            }
            else
            {
                ViewBag.Points = 0; // Guest
            }
            
            return View();
        }
        
        [Authorize]
        public async Task<IActionResult> Rewards()
        {
            // Rewards/redeem page - requires login
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    ViewBag.Points = user.LoyaltyPoints;
                    ViewBag.IsBirthday = user.IsBirthdayToday();
                    return View();
                }
            }
            return RedirectToAction("Landing");
        }
        
        [Authorize]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> RedeemReward([FromBody] dynamic requestData)
        {
            var userId = User.FindFirstValue(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in." });
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found." });
            }
            
            // Extract size from request
            string size = "medium";
            if (requestData is System.Text.Json.JsonElement jsonElement)
            {
                if (jsonElement.TryGetProperty("size", out var sizeProp))
                {
                    size = sizeProp.GetString() ?? "medium";
                }
            }
            
            // Define point costs (updated amounts)
            int pointsNeeded = size.ToLower() switch
            {
                "small" => 200,
                "medium" => 300,
                "large" => 400,
                _ => 0
            };
            
            if (pointsNeeded == 0)
            {
                return Json(new { success = false, message = "Invalid size." });
            }
            
            if (user.LoyaltyPoints < pointsNeeded)
            {
                return Json(new { success = false, message = $"Not enough points. You need {pointsNeeded} points but only have {user.LoyaltyPoints}." });
            }
            
            // Deduct points
            user.LoyaltyPoints -= pointsNeeded;
            var result = await _userManager.UpdateAsync(user);
            
            if (result.Succeeded)
            {
                // Return success with redirect info
                return Json(new { 
                    success = true, 
                    message = $"Enjoy your free {size} drink! Your new point balance is {user.LoyaltyPoints}.",
                    redirectUrl = $"/Home/Index?redeem={size.ToLower()}",
                    size = size.ToLower()
                });
            }
            
            return Json(new { success = false, message = "Failed to redeem points." });
        }
        
        [Authorize]
        public async Task<IActionResult> OrderHistory()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return RedirectToAction("Landing");
            }
            
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return RedirectToAction("Landing");
            }
            
            // Get all orders for this user, ordered by date descending
            var orders = await _context.DrinkOrders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Include(o => o.Ingredients)
                .ToListAsync();
            
            ViewBag.Orders = orders ?? new List<DrinkOrder>();
            ViewBag.User = user;
            
            // Calculate lifetime stats (handle empty orders gracefully)
            ViewBag.TotalOrders = orders?.Count ?? 0;
            ViewBag.TotalSpent = orders?.Sum(o => o.TotalCost) ?? 0m;
            // Lifetime points = total money spent * 5 (all-time points earned, regardless of redemptions)
            ViewBag.LifetimePoints = (int)Math.Floor(user.CurrentMoneySpentAsDecimal * 5.0m);
            
            // Get last order
            ViewBag.LastOrder = orders?.FirstOrDefault();
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> GetOrder(int id) // get the order by id
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier); // find the user by id 
            if (userId == null)
            {
                return Json(new { success = false, message = "Not logged in." }); // if user is not logged in, return false
            }
            
            var order = await _context.DrinkOrders
                .Include(o => o.Ingredients)
                .FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId); // get the order by id and user id
            
            if (order == null)
            {
                return Json(new { success = false, message = "Order not found." }); // if order is not found, return false
            }
            
            var ingredients = order.Ingredients.Select(i => new // select the ingredients
            {
                name = i.Name,
                type = i.Type,
                price = i.Price,
                color = ingredientColors.ContainsKey(i.Name) ? ingredientColors[i.Name] : "#8B7355"
            }).ToList();
            
            return Json(new { success = true, ingredients = ingredients });
        }
        
        private static readonly Dictionary<string, string> ingredientColors = new Dictionary<string, string> // ingredient colors
        {
            {"Espresso Shot", "#4A2C2A"},
            {"Extra Shot", "#3D1F1D"},
            {"Whole Milk", "#FFF8DC"},
            {"Oat Milk", "#F5DEB3"},
            {"Light Cream", "#FFFDD0"},
            {"Caramel Syrup", "#D2691E"},
            {"Vanilla Syrup", "#F5F5DC"},
            {"Strawberry Syrup", "#FF69B4"},
            {"Chocolate Syrup", "#6B4423"},
            {"Pumpkin Spice Syrup", "#FF8C00"},
            {"Matcha Syrup", "#98FB98"},
            {"Lavender Syrup", "#E6E6FA"},
            {"Whipped Cream", "#FFFFFF"},
            {"Cinnamon", "#D2691E"},
            {"Chocolate Chips", "#654321"},
            {"Water", "#ADD8E6"},
            {"Sprinkles", "transparent"}
        };

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() // error page
        {
            return View(new Models.ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CoffeeShopSimulation.Data;
using CoffeeShopSimulation.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using System.Collections.Generic;
using System;

namespace CoffeeShopSimulation.Controllers
{
    // Designates this class as an API Controller, handling web requests
    [Route("api/coffee")]
    [ApiController]
    public class ApiController : ControllerBase
    {
        // Private fields for accessing database and user manager
        private readonly DatabaseContext _context;
        private readonly UserManager<LoyaltyUser> _userManager;

        // Constructor: Dependency Injection to get the database and user manager objects
        public ApiController(DatabaseContext context, UserManager<LoyaltyUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // --- API Endpoint: SaveOrder ---
        // This is called by the JavaScript when the user hits 'Submit Order'
        [HttpPost("saveorder")]
        public async Task<IActionResult> SaveOrder([FromBody] object requestBody)
        {
            try
            {
                // 1. Check if user is logged in (optional - guests can order too)
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                bool isLoggedIn = !string.IsNullOrEmpty(userId);

                // Parse ingredients from request - handle flexible JSON format
                List<Ingredient> ingredients = null;
                
                var jsonString = System.Text.Json.JsonSerializer.Serialize(requestBody);
                var jsonDoc = System.Text.Json.JsonDocument.Parse(jsonString);
                
                if (jsonDoc.RootElement.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    // Direct array - parse it
                    ingredients = System.Text.Json.JsonSerializer.Deserialize<List<Ingredient>>(
                        jsonString,
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }
                else if (jsonDoc.RootElement.TryGetProperty("ingredients", out var ingredientsProp))
                {
                    // Object with ingredients property
                    ingredients = System.Text.Json.JsonSerializer.Deserialize<List<Ingredient>>(
                        ingredientsProp.GetRawText(),
                        new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                }

                // Validate ingredients
                if (ingredients == null || ingredients.Count == 0)
                {
                    return BadRequest(new { message = "No ingredients provided. Please add ingredients to your drink." });
                }
                
                // Filter out items with invalid prices and ensure they have required fields
                var validIngredients = ingredients
                    .Where(i => i != null && i.Price >= 0 && !string.IsNullOrEmpty(i.Name) && !string.IsNullOrEmpty(i.Type))
                    .ToList();
                
                if (validIngredients.Count == 0)
                {
                    return BadRequest(new { message = "Invalid ingredients provided. Please ensure all ingredients have valid names and prices." });
                }

                // 2. Get size and delivery option from request body
                string? orderSize = "medium"; // Default
                string? deliveryOption = "pickup";
                
                if (requestBody is System.Text.Json.JsonElement jsonElement)
                {
                    if (jsonElement.TryGetProperty("size", out var sizeProp))
                    {
                        orderSize = sizeProp.GetString()?.ToLower() ?? "medium";
                    }
                    if (jsonElement.TryGetProperty("deliveryOption", out var deliveryProp))
                    {
                        deliveryOption = deliveryProp.GetString()?.ToLower() ?? "pickup";
                    }
                }
                
                // Calculate base cost from ingredients (only count ingredients with price > 0)
                decimal baseCost = validIngredients.Where(i => i.Price > 0).Sum(i => i.Price);
                
                // Apply size multipliers
                decimal sizeMultiplier = orderSize?.ToLower() switch
                {
                    "small" => 0.85m,
                    "large" => 1.25m,
                    _ => 1.0m // medium
                };
                
                decimal subtotal = baseCost * sizeMultiplier;
                
                // Add delivery fee (only if delivery option is selected)
                decimal deliveryFee = (deliveryOption == "delivery") ? 2.50m : 0m;
                
                // Total cost before discounts
                decimal totalCost = subtotal + deliveryFee;
                decimal originalCost = totalCost; // Store original cost before discounts
                decimal discountAmount = 0;
                string? discountType = null;

                // 3. Create a new DrinkOrder transaction
                DrinkOrder newOrder = null;
                LoyaltyUser user = null;
                int newPoints = 0;
                int pointsEarnedThisOrder = 0;
                decimal moneySpentDecimal = 0.00m;
                bool isBirthday = false;

                if (isLoggedIn)
                {
                    // Fetch the user object for logged-in users
                    user = await _userManager.Users
                                             .FirstOrDefaultAsync(u => u.Id == userId);
                    
                    if (user == null)
                    {
                        return NotFound(new { message = "Loyalty user not found." });
                    }
                    
                    // Check for redemption discount from query parameter first (takes priority)
                    string? redeemSize = Request.Query["redeem"].ToString();
                    bool hasRedemption = !string.IsNullOrEmpty(redeemSize);
                    
                    // Validate size if redemption is active - must match redeemed size
                    if (hasRedemption)
                    {
                        if (!string.IsNullOrEmpty(orderSize) && orderSize != redeemSize.ToLower())
                        {
                            return BadRequest(new { message = $"You can only redeem a {redeemSize} size drink. Please select the correct size." });
                        }
                    }
                    
                    // Check if it's the user's birthday AND they haven't used it today (only if no redemption)
                    isBirthday = !hasRedemption && user.IsBirthdayToday() && !user.HasUsedBirthdayDiscountToday();
                    
                    if (hasRedemption && totalCost > 0)
                    {
                        // Apply redemption discount - make it free
                        discountAmount = totalCost;
                        totalCost = 0;
                        discountType = "redemption";
                    }
                    else if (isBirthday && totalCost > 0)
                    {
                        // Apply birthday discount - make it free
                        discountAmount = totalCost;
                        totalCost = 0;
                        discountType = "birthday";
                        // Mark that birthday discount was used today
                        user.LastBirthdayDiscountUsed = DateTime.Today;
                    }

                    // Create order with user ID
                    newOrder = new DrinkOrder
                    {
                        UserId = user.Id,
                        TotalCost = totalCost,
                        OrderDate = DateTime.Now,
                        DiscountAmount = discountAmount > 0 ? discountAmount : null,
                        DiscountType = discountType,
                        Ingredients = validIngredients.Select(i => new Ingredient 
                        { 
                            Name = i.Name ?? string.Empty, 
                            Type = i.Type ?? string.Empty, 
                            Price = i.Price 
                        }).ToList()
                    };

                    // Calculate points earned from THIS order (before discount)
                    pointsEarnedThisOrder = 0;
                    
                    // Update Loyalty Points and Money Spent (ONLY if no discount applied)
                    // Discounts should NOT count towards points
                    if (discountAmount == 0 && totalCost > 0)
                    {
                        // Calculate points for THIS order: 5 points per $1
                        pointsEarnedThisOrder = (int)Math.Floor(totalCost * 5.0m);
                        
                        // Update money spent
                        decimal newTotalSpent = user.CurrentMoneySpentAsDecimal + totalCost;
                        user.MoneySpent = newTotalSpent.ToString("F2");
                        
                        // Add points to existing total
                        user.LoyaltyPoints += pointsEarnedThisOrder;
                    } 
                    newPoints = user.LoyaltyPoints;
                    decimal.TryParse(user.MoneySpent, out moneySpentDecimal);

                    // Save everything to the database
                    _context.DrinkOrders.Add(newOrder);
                    
                    // Update user through UserManager
                    var updateResult = await _userManager.UpdateAsync(user);
                    
                    if (!updateResult.Succeeded)
                    {
                        return StatusCode(500, new { message = "Failed to update user points." });
                    }
                }
                else
                {
                    // Guest order - create order without user ID
                    newOrder = new DrinkOrder
                    {
                        UserId = null, // Guest orders don't have a user
                        TotalCost = totalCost,
                        OrderDate = DateTime.Now,
                        Ingredients = validIngredients.Select(i => new Ingredient 
                        { 
                            Name = i.Name ?? string.Empty, 
                            Type = i.Type ?? string.Empty, 
                            Price = i.Price 
                        }).ToList()
                    };

                    _context.DrinkOrders.Add(newOrder);
                }

                // Save the order and ingredients
                await _context.SaveChangesAsync();

                // 4. Return success message
                string message = "Order placed and points updated!";
                bool hasDiscount = isLoggedIn && ((isBirthday && newOrder.DiscountType == "birthday") || newOrder.DiscountType == "redemption");
                
                if (hasDiscount && totalCost == 0)
                {
                    if (newOrder.DiscountType == "birthday")
                    {
                        message = "🎉 Happy Birthday! Enjoy your FREE drink! 🎉";
                    }
                    else if (newOrder.DiscountType == "redemption")
                    {
                        message = "🎉 Enjoy your FREE redeemed drink! 🎉";
                    }
                }
                
                if (isLoggedIn)
                {
                    // Get total points after this order (includes points earned)
                    int totalPointsAfterOrder = newPoints;
                    
                    return Ok(new 
                    { 
                        message = message,
                        totalCost = totalCost.ToString("C"), // Cost AFTER discount (what was actually paid)
                        originalCost = originalCost.ToString("C"), // Cost BEFORE discount (for display)
                        newPoints = totalPointsAfterOrder, // Total points after order
                        pointsEarned = pointsEarnedThisOrder, // Points earned from THIS order only
                        moneySpent = moneySpentDecimal.ToString("C"),
                        orderId = newOrder.Id,
                        isLoggedIn = true,
                        isBirthday = isBirthday && newOrder.DiscountType == "birthday",
                        hasDiscount = hasDiscount,
                        discountType = newOrder.DiscountType,
                        discountAmount = discountAmount.ToString("C") // Amount discounted
                    });
                }
                else
                {
                    return Ok(new 
                    { 
                        message = "Order placed! Create an account to start earning loyalty points!",
                        totalCost = totalCost.ToString("C"),
                        newPoints = 0,
                        moneySpent = "$0.00",
                        orderId = newOrder.Id,
                        isLoggedIn = false
                    });
                }
            }
            catch (Exception ex)
            {
                // Log the exception for debugging
                return StatusCode(500, new { message = "An error occurred while processing your order: " + ex.Message });
            }
        }

        // --- API Endpoint: GetCurrentUserPoints ---
        // This is called when the page loads to display current loyalty points
        [HttpGet("currentuser")]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                // User is not logged in
                return Ok(new { isLoggedIn = false, points = 0 });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Ok(new { isLoggedIn = false, points = 0 });
            }

            return Ok(new
            {
                isLoggedIn = true,
                points = user.LoyaltyPoints,
                moneySpent = user.MoneySpent
            });
        }
    }
}
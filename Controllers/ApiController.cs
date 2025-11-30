using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using CoffeeShopSimulation.Data;
using CoffeeShopSimulation.Models;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims; // Needed for accessing the current user's ID

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
        public async Task<IActionResult> SaveOrder([FromBody] List<Ingredient> ingredients)
        {
            try
            {
                // Validate ingredients
                if (ingredients == null || ingredients.Count == 0)
                {
                    return BadRequest(new { message = "No ingredients provided." });
                }

                // 1. Get the current logged-in user (LoyaltyUser)
                // If the user is not logged in, we cannot save the order or track points.
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                {
                    // This returns a 401 Unauthorized error if the user isn't logged in
                    return Unauthorized(new { message = "You must be logged in to place an order." });
                }

                // Fetch the user object including their orders for point calculation
                var user = await _userManager.Users
                                            .Include(u => u.DrinkOrders)
                                            .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return NotFound(new { message = "Loyalty user not found." });
                }

                // 2. Calculate the total cost of the order
                decimal totalCost = ingredients.Sum(i => i.Price);

            // 3. Create a new DrinkOrder transaction
            var newOrder = new DrinkOrder
            {
                UserId = user.Id,
                TotalCost = totalCost,
                OrderDate = DateTime.Now,
                Ingredients = new List<Ingredient>()
            };

            // 4. Add ingredients to the order (Entity Framework will handle foreign keys)
            foreach (var ingredient in ingredients)
            {
                // Create a new ingredient and add it to the order
                var newIngredient = new Ingredient
                {
                    Name = ingredient.Name,
                    Type = ingredient.Type,
                    Price = ingredient.Price,
                    DrinkOrder = newOrder  // Set navigation property
                };
                newOrder.Ingredients.Add(newIngredient);
            }

            // 5. Save the order (this will also save the ingredients due to the relationship)
            _context.DrinkOrders.Add(newOrder);

            // 6. Update Loyalty Points and Money Spent
            user.CalculateNewPoints(totalCost);
            
            // 7. Save all changes to the database
            await _context.SaveChangesAsync();
            
            // 8. Update user after saving order (refresh from DB to get latest points)
            //await _userManager.UpdateAsync(user);

                // 9. Return success message and point data to the front-end
                return Ok(new
                {
                    message = "Order placed and points updated!",
                    totalCost = totalCost.ToString("C"),
                    newPoints = user.LoyaltyPoints,
                    moneySpent = decimal.Parse(user.MoneySpent).ToString("C")
                });
            }
            catch (Exception)
            {
                // Log the exception and return a user-friendly error
                return StatusCode(500, new { message = "An error occurred while processing your order. Please try again." });
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

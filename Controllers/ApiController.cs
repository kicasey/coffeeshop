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
                // Add the ingredients received from the front-end to the order
                Ingredients = ingredients
            };

            // 4. Update Loyalty Points and Money Spent
            user.CalculateNewPoints(totalCost);

            // 5. Save everything to the database
            _context.DrinkOrders.Add(newOrder);

            // Note: We need to update the user record because their points changed
            await _userManager.UpdateAsync(user);
            await _context.SaveChangesAsync();

            // 6. Return success message and point data to the front-end
            return Ok(new
            {
                message = "Order placed and points updated!",
                totalCost = totalCost.ToString("C"),
                newPoints = user.LoyaltyPoints,
                moneySpent = user.MoneySpent.ToString("C")
            });
        }
    }
}

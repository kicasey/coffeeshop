using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoffeeShopSimulation.Models
{
    // Extends IdentityUser to add custom properties for the loyalty program.
    public class LoyaltyUser : IdentityUser
    {
        // --- Private Fields ---
        private int loyaltyPoints;
        private decimal moneySpent;
        private string firstName;
        private string lastName;

        // --- Navigation Property ---
        private List<DrinkOrder> drinkOrders;

        // --- Constructors ---
        public LoyaltyUser()
        {
            // Initialize custom fields
            this.loyaltyPoints = 0;
            this.moneySpent = 0.00m;
            this.firstName = string.Empty;
            this.lastName = string.Empty;
            this.drinkOrders = new List<DrinkOrder>();
        }

        // --- Public Properties (Explicit Getters/Setters) ---

        public int LoyaltyPoints
        {
            get { return loyaltyPoints; }
            set { loyaltyPoints = value; }
        }

        public decimal MoneySpent
        {
            get { return moneySpent; }
            set { moneySpent = value; }
        }

        public string FirstName
        {
            get { return firstName; }
            set { firstName = value; }
        }

        public string LastName
        {
            get { return lastName; }
            set { lastName = value; }
        }

        // Navigation Property: Allows C# code to easily access the user's order history.
        public List<DrinkOrder> DrinkOrders
        {
            get { return drinkOrders; }
            set { drinkOrders = value; }
        }

        // --- Custom Business Logic ---

        /// <summary>
        /// Calculates new loyalty points earned based on the order total.
        /// Rule: 1 point for every $5 spent.
        /// </summary>
        /// <param name="orderTotal">The total cost of the latest order.</param>
        public void CalculateNewPoints(decimal orderTotal)
        {
            // 1. Update the total money spent
            this.MoneySpent += orderTotal;

            // 2. Calculate newly earned points
            int initialPoints = this.LoyaltyPoints;
            
            // Calculate how many times $5 has been spent in total
            int totalPossiblePoints = (int)Math.Floor(this.MoneySpent / 5.00m);
            
            // Determine how many new points were earned
            int pointsEarned = totalPossiblePoints - initialPoints;

            // 3. Update Loyalty Points
            if (pointsEarned > 0)
            {
                this.LoyaltyPoints = totalPossiblePoints;
            }
        }
    }
}
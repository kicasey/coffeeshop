using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoffeeShopSimulation.Models
{
    public class LoyaltyUser : IdentityUser
    {
        // --- Private Fields (moneySpent changed to string) ---
        private int loyaltyPoints;
        private string moneySpent; // <--- CHANGED TO STRING
        private string firstName;
        private string lastName;

        // --- Navigation Property ---
        private List<DrinkOrder> drinkOrders;

        // --- Constructors ---
        public LoyaltyUser()
        {
            this.loyaltyPoints = 0;
            this.moneySpent = "0.00"; // <--- Initialized as a string
            this.firstName = string.Empty;
            this.lastName = string.Empty;
            this.drinkOrders = new List<DrinkOrder>();
        }

        // --- Public Properties ---

        public int LoyaltyPoints
        {
            get { return loyaltyPoints; }
            set { loyaltyPoints = value; }
        }

        // This property remains a string to map correctly to the database's TEXT column
        public string MoneySpent
        {
            get { return moneySpent; }
            set { moneySpent = value; }
        }

        // *** NEW HELPER PROPERTY FOR CALCULATIONS ***
        // Safely converts the stored string to a decimal for arithmetic operations
        public decimal CurrentMoneySpentAsDecimal 
        {
            get 
            { 
                if (decimal.TryParse(this.moneySpent, out decimal result))
                {
                    return result;
                }
                return 0.00m;
            }
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

        public List<DrinkOrder> DrinkOrders
        {
            get { return drinkOrders; }
            set { drinkOrders = value; }
        }

        // --- Custom Business Logic (Updated to use CurrentMoneySpentAsDecimal) ---
        public void CalculateNewPoints(decimal orderTotal)
        {
            // 1. Calculate and update total money spent
            decimal newTotalSpent = CurrentMoneySpentAsDecimal + orderTotal;
            
            // 2. Save the total spent back as a formatted string ("F2" ensures two decimal places)
            this.MoneySpent = newTotalSpent.ToString("F2"); 

            // 3. Calculate newly earned points (Rule: 1 point for every $5 spent cumulatively)
            int totalPossiblePoints = (int)Math.Floor(newTotalSpent / 5.00m);
            
            // 4. Update Loyalty Points
            this.LoyaltyPoints = totalPossiblePoints;
        }
    }
}
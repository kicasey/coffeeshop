using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CoffeeShopSimulation.Models
{
    public class LoyaltyUser : IdentityUser
    {
        // private fields
        private int loyaltyPoints;
        private string moneySpent; // changed to string
        private string firstName;
        private string lastName;
        private DateTime? birthday; // nullable birthday field
        private DateTime? lastBirthdayDiscountUsed; // track when birthday discount was last used make sure cant use again today

        // navigation property
        private List<DrinkOrder> drinkOrders;

        // constructors
        public LoyaltyUser()
        {
            this.loyaltyPoints = 0;
            this.moneySpent = "0.00"; // initialized as a string
            this.firstName = string.Empty;
            this.lastName = string.Empty;
            this.drinkOrders = new List<DrinkOrder>();
        }

        // getters and setters

        public int LoyaltyPoints
        {
            get { return loyaltyPoints; }
            set { loyaltyPoints = value; }
        }

        // this property remains a string to map correctly to the database's TEXT column
        public string MoneySpent
        {
            get { return moneySpent; }
            set { moneySpent = value; }
        }

        // new helper property for calculations
        // safely converts the stored string to a decimal for arithmetic operations
        public decimal CurrentMoneySpentAsDecimal 
        {
            get 
            { 
                if (decimal.TryParse(this.moneySpent, out decimal result)) // try to parse the money spent as a decimal
                {
                    return result;
                }
                return 0.00m;
            }
        }

        // getters and setters
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

        // navigation property
        public List<DrinkOrder> DrinkOrders
        {
            get { return drinkOrders; }
            set { drinkOrders = value; }
        }

        // getters and setters
        public DateTime? Birthday
        {
            get { return birthday; }
            set { birthday = value; }
        }

        public DateTime? LastBirthdayDiscountUsed
        {
            get { return lastBirthdayDiscountUsed; }
            set { lastBirthdayDiscountUsed = value; }
        }

        // check if today is user's birthday
        public bool IsBirthdayToday()
        {
            if (!birthday.HasValue) return false;
            var today = DateTime.Today;
            return birthday.Value.Month == today.Month && birthday.Value.Day == today.Day;
        }

        // check if birthday discount has been used today
        public bool HasUsedBirthdayDiscountToday()
        {
            if (!lastBirthdayDiscountUsed.HasValue) return false;
            var today = DateTime.Today;
            return lastBirthdayDiscountUsed.Value.Date == today;
        }

        // check if user can redeem a reward
        public bool CanRedeemReward(string size)
        {
            int pointsNeeded = size.ToLower() switch
            {
                "small" => 200,
                "medium" => 300,
                "large" => 400,
                _ => int.MaxValue
            };
            return LoyaltyPoints >= pointsNeeded;
        }

        // custom business logic (updated: 5 points per $1)
        public void CalculateNewPoints(decimal orderTotal)
        {
            // 1) calculate and update total money spent
            decimal newTotalSpent = CurrentMoneySpentAsDecimal + orderTotal;
            
            // 2) save the total spent back as a formatted string ("F2" ensures two decimal places!!)
            this.MoneySpent = newTotalSpent.ToString("F2"); 
            
            // 3) calculate newly earned points (Rule: 5 points for every $1 spent cumulatively)
            int totalPossiblePoints = (int)Math.Floor(newTotalSpent * 5.00m);
            
            // 4) update loyalty points
            this.LoyaltyPoints = totalPossiblePoints;
        }
    }
}
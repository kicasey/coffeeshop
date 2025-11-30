using System;
using System.Collections.Generic;

namespace CoffeeShopSimulation.Models
{
    //records a complete transaction
    public class DrinkOrder
    {
        private int id;
        private decimal totalCost;
        private string? userId; // Nullable for guest orders
        private DateTime orderDate;
        private decimal? discountAmount; // Discount applied (e.g., from redemption or birthday)
        private string? discountType; // "birthday", "redemption", etc.
        
        // navigation cause order will have ingredients
        private List<Ingredient> ingredients;

        //constructors
        public DrinkOrder()
        {
            this.orderDate = DateTime.Now;
            this.totalCost = 0.00m;
            this.ingredients = new List<Ingredient>();
        }

        // getters and setters
        public int Id
        {
            get {return id;}
            set {id = value;}
        }

        public decimal TotalCost
        {
            get {return totalCost;}
            set {totalCost = value;}
        }

        //foreign key that links order to customer who placed it 
        public string? UserId
        {
            get {return userId;}
            set {userId = value;}
        }

        public DateTime OrderDate
        {
            get { return orderDate; }
            set { orderDate = value; }
        }

        //navigation property to let you see and retrieve ingredients in property
        public List<Ingredient> Ingredients
        {
            get { return ingredients; }
            set { ingredients = value; }
        }

        public decimal? DiscountAmount
        {
            get { return discountAmount; }
            set { discountAmount = value; }
        }

        public string? DiscountType
        {
            get { return discountType; }
            set { discountType = value; }
        }
    }
}
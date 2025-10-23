using System;
using System.Collections.Generic;

namespace CoffeeShopSimulation.Models
{
    //records a complete transaction
    public class DrinkOrder
    {
        private int id;
        private decimal totalCost;
        private string userId;
        private DateTime orderDate;
        
        // navigation cause order will have ingredients
        private List<Ingredient> ingredients;

        //constructors
        public DrinkOrder()
        {
            this.orderDate = DateTime.Now;
            this.totalCost = 0.00m;
            this.ingredients = new List<Ingredient>();
        }

        // --- Public Properties (Explicit Getters/Setters) ---
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
        public string UserId
        {
            get {return userId;}
            set {userId = value;}
        }

        public DateTime OrderDate
        {
            get { return orderDate; }
            set { orderDate = value; }
        }

        //nav property to let you see retrieve ingredients in property
        public List<Ingredient> Ingredients
        {
            get { return ingredients; }
            set { ingredients = value; }
        }
    }
}
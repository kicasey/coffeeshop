using System;
using System.Collections.Generic;

namespace CoffeeShopSimulation.Models
{
    // blueprint for a single drag-and-drop item 
    public class Ingredient
    {
        // private fields
        private int id;
        private string type;
        private string name;
        private decimal price;
        private int drinkOrderId; // foreign key that links ingredient to order

        public Ingredient()
        {
            // default constructor required by entity framework core
            this.id = 0;
            this.type = string.Empty;
            this.name = string.Empty;
            this.price = 0.00m;
            this.drinkOrderId = 0;
        }

        // constructor for creating a new ingredient with specific details
        public Ingredient(string ingredientName, string ingredientType, decimal ingredientPrice)
        {
            this.name = ingredientName;
            this.type = ingredientType;
            this.price = ingredientPrice;
        }


        // getters and setters

        public int Id
        {
            get { return id; }
            set { id = value; }
        }

        public string Type
        {
            get { return type; }
            set { type = value; }
        }

        public string Name
        {
            get { return name; }
            set { name = value; }
        }

        // The price for this specific ingredient 
        public decimal Price
        {
            get { return price; }
            set { price = value; }
        }

        // foreign key that links ingredient to order
        public int DrinkOrderId
        {
            get { return drinkOrderId; }
            set { drinkOrderId = value; }
        }

        // navigation property to let you see and retrieve the parent order
        public DrinkOrder DrinkOrder { get; set; } = null!;
    }
}
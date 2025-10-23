using System;
using System.Collections.Generic;

namespace CoffeeShopSimulation.Models
{
    // Blueprint for a single drag-and-drop item (e.g., Espresso Shot, Caramel Syrup).
    public class Ingredient
    {
        // --- Private Fields (The actual data storage) ---
        private int id;
        private string type;
        private string name;
        private decimal price;
        private int drinkOrderId; // Foreign Key

        // --- Constructors ---
        public Ingredient()
        {
            // Default constructor required by Entity Framework Core
            this.id = 0;
            this.type = string.Empty;
            this.name = string.Empty;
            this.price = 0.00m;
            this.drinkOrderId = 0;
        }

        // Constructor for creating a new ingredient with specific details
        public Ingredient(string ingredientName, string ingredientType, decimal ingredientPrice)
        {
            this.name = ingredientName;
            this.type = ingredientType;
            this.price = ingredientPrice;
        }


        // --- Public Properties (The controlled doorway to the private fields) ---

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

        // The price for this specific ingredient (e.g., $1.00 for an extra shot)
        public decimal Price
        {
            get { return price; }
            set { price = value; }
        }

        // Foreign Key: ID of the parent DrinkOrder this ingredient belongs to.
        public int DrinkOrderId
        {
            get { return drinkOrderId; }
            set { drinkOrderId = value; }
        }

        // Navigation Property: Allows C# code to easily access the parent Order.
        public DrinkOrder DrinkOrder { get; set; } = null!;
    }
}
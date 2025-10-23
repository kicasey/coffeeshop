using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CoffeeShopSimulation.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace CoffeeShopSimulation.Data
{
    // The DatabaseContext is the bridge to the SQLite file.
    // It inherits from IdentityDbContext to get built-in User/Login tables.
    public class DatabaseContext : IdentityDbContext<LoyaltyUser> 
    {
        // Private fields to hold the DbSet references (the tables)
        private DbSet<DrinkOrder> drinkOrders;
        private DbSet<Ingredient> ingredients;

        // Constructor required by ASP.NET Core
        public DatabaseContext(DbContextOptions<DatabaseContext> options)
            : base(options)
        {
        }

        // --- Database Tables (DbSets) ---
        public DbSet<DrinkOrder> DrinkOrders
        {
            get { return drinkOrders; }
            set { drinkOrders = value; }
        }

        public DbSet<Ingredient> Ingredients
        {
            get { return ingredients; }
            set { ingredients = value; }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // This is required to make the IdentityDbContext work properly.
            base.OnModelCreating(builder);
        }
    }
}
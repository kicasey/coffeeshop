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

            // Configure the relationship between DrinkOrder and Ingredient
            builder.Entity<Ingredient>()
                .HasOne(i => i.DrinkOrder)
                .WithMany(o => o.Ingredients)
                .HasForeignKey(i => i.DrinkOrderId)
                .OnDelete(DeleteBehavior.Cascade); // If order is deleted, ingredients are deleted too

            // Configure the relationship between LoyaltyUser and DrinkOrder
            builder.Entity<DrinkOrder>()
                .HasOne<LoyaltyUser>()
                .WithMany(u => u.DrinkOrders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade); // If user is deleted, orders are deleted too
        }
    }
}
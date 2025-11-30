using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using CoffeeShopSimulation.Models;
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

namespace CoffeeShopSimulation.Data
{
    // database context is the bridge to the sqlite file
    // it is gonna contain the dbSet references
    public class DatabaseContext : IdentityDbContext<LoyaltyUser> 
    {
        // private fields to hold the dbset references (the tables)
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
            // required to make the identityDbContext work properly
            base.OnModelCreating(builder);

            // make the relationship between DrinkOrder and Ingredient
            builder.Entity<Ingredient>()
                .HasOne(i => i.DrinkOrder)
                .WithMany(o => o.Ingredients)
                .HasForeignKey(i => i.DrinkOrderId)
                .OnDelete(DeleteBehavior.Cascade); // if order is deleted, ingredients are deleted too

            // make the relationship between LoyaltyUser and DrinkOrder
            builder.Entity<DrinkOrder>()
                .HasOne<LoyaltyUser>()
                .WithMany(u => u.DrinkOrders)
                .HasForeignKey(o => o.UserId)
                .OnDelete(DeleteBehavior.Cascade); // if user is deleted, orders are deleted too
        }
    }
}
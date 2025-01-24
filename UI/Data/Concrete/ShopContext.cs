using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Data.Configuration;
using UI.Entity;
using UI.Data.Configuration;

namespace UI.Data.Concrete
{
    public class ShopContext : DbContext 
    {
        public ShopContext()
        {
        }
        public ShopContext(DbContextOptions<ShopContext> options) : base(options)
        {

        }
        public DbSet<Quiz> Quiz { get; set; }    
        public DbSet<Blog> Blog { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new QuizConfiguration());
            modelBuilder.ApplyConfiguration(new BlogConfiguration());
            modelBuilder.Seed(); // Fully qualified if needed
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        { 
        }
    }
}
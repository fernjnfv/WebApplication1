using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.Configuration;

namespace UserManagement
{
    public class UserDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }

        static readonly string connectionString = "Server=localhost; User ID=sa; Password=admin; Database=User; TrustServerCertificate=True";

        public UserDbContext()
        {
            Database.EnsureCreated();
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlServer(connectionString);
        }
    }
}

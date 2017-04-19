using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using src.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Data
{
    public class ApplicationDbContext:IdentityDbContext
    {
        public DbSet<Document> Documents { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public ApplicationDbContext(DbContextOptions options): base(options)
        {
        }
    }
}

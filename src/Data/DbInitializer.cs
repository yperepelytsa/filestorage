using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using src.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace src.Data
{
    public class DbInitializer
    {
        private ApplicationDbContext _context;
        private UserManager<IdentityUser> _userManager;
        private IWatsonService _wtservice { get; set; }
        public DbInitializer(ApplicationDbContext context, UserManager<IdentityUser> userManager, IWatsonService wtservice)
        {
            _wtservice = wtservice;
            _context = context;
            _userManager = userManager;
        }
        public void Initialize()
        {
            _context.Database.Migrate();
            createAdmin();
        }
        public void createAdmin()
        {
            var admin = _userManager.FindByNameAsync("Admin").GetAwaiter().GetResult();
            if (admin == null)
            {
                admin = new IdentityUser() { UserName = "Admin", Email = "admin@example.com" };
                _userManager.CreateAsync(admin).GetAwaiter().GetResult();
                _userManager.AddPasswordAsync(admin, "Password_1").GetAwaiter().GetResult();
                _wtservice.CreateCollectionAsync(admin.Id).GetAwaiter().GetResult();
            }

        }
    }
}

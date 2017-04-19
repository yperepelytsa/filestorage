using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using src.Services;
using System.IO;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Security.Claims;

namespace src.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private IWatsonService _wtservice { get; set; }
        public HomeController(IWatsonService wtservice)
        {
            _wtservice = wtservice;
        }


        public IActionResult Index(string search=null)
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;            
            var docs = _wtservice.SearchFiles(userId, search).GetAwaiter().GetResult();
            return View(docs);
        }
        [HttpGet("filetable")]
        public IActionResult FileTable(string search = null)
        {
            var userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var docs = _wtservice.SearchFiles(userId, search).GetAwaiter().GetResult();
            return View(docs);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}

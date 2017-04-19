using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using src.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace src.Controllers
{
    [Authorize]
    public class FileController:Controller
    {
        private IWatsonService _wtservice { get; set; }
        private UserManager<IdentityUser> _userManager { get; set; }
        public FileController(IWatsonService wtservice, UserManager<IdentityUser> userManager)
        {
            _wtservice = wtservice;
            _userManager = userManager;
        }

        [HttpPost("file-upload")]
        public async Task<IActionResult> UploadFile()
        {
            string userId = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //   var files 
            var files = Request.Form.Files;
            var uploads = Path.Combine("../uploads", userId);
            if (!Directory.Exists(uploads))
                Directory.CreateDirectory(uploads);
            foreach (var file in files)
            {
                if (System.IO.File.Exists(Path.Combine(uploads, file.FileName)))
                    return BadRequest("File already exists.");
            }
                foreach (var file in files)
            {
                if (file.Length > 0)
                {
                    using (var fileStream = new FileStream(Path.Combine(uploads, file.FileName), FileMode.Create))
                    {
                        await file.CopyToAsync(fileStream);                       
                    }
                    try { 
                    await _wtservice.AddFileAsync(userId, file.FileName);
                    }
                    catch(Exception)
                    {
                        System.IO.File.Delete(Path.Combine("../uploads/", userId, file.FileName));
                        return BadRequest();
                    }
                }
            }
            return Ok();
        }
        [HttpGet("files/{name}")]
        public IActionResult GetFile(string name)
        {
            if (!System.IO.File.Exists(Path.Combine("../uploads/", HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, name)))
                return NotFound();
            byte[] fileBytes = System.IO.File.ReadAllBytes(Path.Combine("../uploads/", HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, name));
            return File(fileBytes, "application/x-msdownload", name);
        }
        [HttpPost("files/delete/{name}")]
        public IActionResult DeleteFile(string name)
        {
            if (!System.IO.File.Exists(Path.Combine("../uploads/", HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, name)))
                return NoContent();
            _wtservice.DeleteFileAsync(HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, name).GetAwaiter().GetResult();
            System.IO.File.Delete(Path.Combine("../uploads/", HttpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value, name));
            return NoContent();
        }

    }
}

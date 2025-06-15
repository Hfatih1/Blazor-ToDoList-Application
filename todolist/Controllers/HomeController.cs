using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using todolist.Areas.Identity.Data;
using todolist.Data;
using todolist.Models;

namespace todolist.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {

        private readonly ILogger<HomeController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        private todoDbContext context;

        public HomeController(todoDbContext ctx) => context = ctx;


        public IActionResult Index(string id)
        {
            //ViewData["UserID"]=_userManager.GetUserId(this.User);
            var filters = new Filters(id);
            ViewBag.Filters = filters;

            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            ViewBag.Categories = context.Categories.ToList();
            ViewBag.Statuses = context.Statuses.ToList();
            ViewBag.DueFilters = Filters.DueFilterValues;

            IQueryable<ToDo> query = context.ToDos
                .Include(t => t.Category)
                .Include(t => t.Creator)
                .Include(t => t.Status)
                .Where(t => t.CreatorId == userId);

            if (filters.HasCategory)
            {
                query = query.Where(t => t.CategoryId == filters.CategoryId);
            }

            if (filters.HasStatus)
            {
                query = query.Where(t => t.StatusId == filters.StatusId);
            }
            if (filters.HasDue)
            {
                var today = DateTime.Today;
                if (filters.IsPast) {
                    query = query.Where(t => t.DueDate < today);
                }
                else if (filters.IsFuture)
                {
                    query = query.Where(t => t.DueDate > today);
                }
                else if (filters.IsToday)
                {
                    query = query.Where(t => t.DueDate == today);
                }
            }
            var tasks = query.OrderBy(t => t.DueDate).ToList();

            return View(tasks);
        }

        [HttpGet]
        public IActionResult Add()
        {
            ViewBag.Categories = context.Categories.ToList();
            ViewBag.Statuses = context.Statuses.ToList();
            var task = new ToDo { StatusId = "open" };
            return View(task);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public IActionResult Add(ToDo task)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (userId == null)
            {
                return Unauthorized();
            }

            // Check if user exists
            var userExists = context.Users.Any(u => u.Id == userId);
            if (!userExists)
            {
                return Unauthorized(); 
            }

            task.CreatorId = userId;

            if (ModelState.IsValid)
            {
                context.ToDos.Add(task);
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            else
            {
                ViewBag.Categories = context.Categories.ToList();
                ViewBag.Statuses = context.Statuses.ToList();
                return View(task);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult Filter(string[] filter)
        {
            string id = string.Join('-', filter);
            return RedirectToAction("Index", new { ID = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
            public IActionResult MarkComplete([FromRoute] string id, ToDo selected)
            {
                selected = context.ToDos.Find(selected.Id)!;

                if (selected != null)
                {
                    selected.StatusId = "closed";
                    context.SaveChanges();
                }
                return RedirectToAction("index", new { ID = id });
            }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public IActionResult DeleteComplete(string id)
        {
            var toDelete = context.ToDos.Where(t => t.StatusId == "closed").ToList();

            foreach(var task in toDelete)
            {
                context.ToDos.Remove(task);
            }
            context.SaveChanges();
            return RedirectToAction("Index",new { ID = id });  
        }
    }
}

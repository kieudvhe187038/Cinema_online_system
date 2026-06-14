using Cinema_System.Application.Interfaces;
using Cinema_System.Application.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace Cinema_System.Controllers
{
    public class MoviesController : Controller
    {
        private readonly IMovieService _movieService;

        public MoviesController(IMovieService movieService) 
        {
            _movieService = movieService;
        }

        public async Task<IActionResult> Index(string tab = "now", int page = 1)
        {
            var pageSize = 4;
            var moviesPageViewModel = await _movieService.GetMoviesPageAsync(tab, page, pageSize);
            return View(moviesPageViewModel);
        }

        public async Task<IActionResult> Search([FromQuery(Name = "q")] string searchQuery, int page = 1) 
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return RedirectToAction("Index");
            }

            var pageSize = 4;
            var moviesPageViewModel = await _movieService.SearchMoviesAsync(searchQuery, page, pageSize);
            ViewData["SearchKeyword"] = searchQuery;
            return View("Index", moviesPageViewModel);
        }
    }
}

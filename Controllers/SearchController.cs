using Microsoft.AspNetCore.Mvc;
using TestProject.Models;
using TestProject.Services;

namespace TestProject.Controllers {
    /// <summary>
    /// JSON API for searching the configured base directory by name. Like
    /// <see cref="DirectoryController"/>, it only translates HTTP to and from
    /// <see cref="FileSystemService"/>; the matching and traversal live there.
    /// </summary>
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase {
        private readonly FileSystemService _fileSystem;

        public SearchController(FileSystemService fileSystem) {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// GET /api/search?q=cat — finds every file and directory under the base whose name
        /// contains <paramref name="query"/> (case-insensitive), searching all subdirectories,
        /// and returns them as a JSON <see cref="Listing"/> with base-relative paths.
        /// </summary>
        /// <returns>200 with the matches; 400 if <c>q</c> is missing or blank.</returns>
        [HttpGet]
        public ActionResult<Listing> Search([FromQuery(Name = "q")] string query = "") {
            try {
                return Ok(_fileSystem.Search(query));
            } catch (ArgumentException) {
                return BadRequest();
            }
        }
    }
}

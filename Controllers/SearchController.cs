using Microsoft.AspNetCore.Mvc;
using TestProject.Models;
using TestProject.Services;

namespace TestProject.Controllers {
    /// <summary>
    /// JSON API for searching the configured base directory by name. Like
    /// <see cref="DirectoryController"/>, it only translates HTTP to and from
    /// <see cref="IFileSystemService"/>; the matching and traversal live there.
    /// </summary>
    [ApiController]
    [Route("api/search")]
    public class SearchController : ControllerBase {
        private readonly IFileSystemService _fileSystem;

        public SearchController(IFileSystemService fileSystem) {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// GET /api/search?path=photos&amp;q=cat — finds every file and directory under the
        /// directory <c>path</c> whose name contains <paramref name="query"/> (case-insensitive),
        /// searching all subdirectories, and returns them as a JSON array of
        /// <see cref="FileSystemEntry"/> with base-relative paths, directories first. Omitting
        /// <c>path</c> searches the whole base.
        /// </summary>
        /// <returns>
        /// 200 with the matches; 400 if <c>q</c> is missing or blank or the path escapes the base;
        /// 404 if the directory does not exist.
        /// </returns>
        [HttpGet]
        public ActionResult<IReadOnlyList<FileSystemEntry>> Search([FromQuery(Name = "q")] string query = "", [FromQuery] string path = "") {
            try {
                return Ok(_fileSystem.Search(path, query));
            } catch (ArgumentException) {
                return BadRequest();
            } catch (DirectoryNotFoundException) {
                return NotFound();
            }
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using TestProject.Models;
using TestProject.Services;

namespace TestProject.Controllers {
    /// <summary>
    /// JSON API for browsing the configured base directory. The controller only translates
    /// HTTP to and from <see cref="IFileSystemService"/>; all path and listing logic lives there.
    /// </summary>
    [ApiController]
    [Route("api/dir")]
    public class DirectoryController : ControllerBase {
        private readonly IFileSystemService _fileSystem;

        public DirectoryController(IFileSystemService fileSystem) {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// GET /api/dir?path=photos — lists the entries directly inside <paramref name="path"/>
        /// as a JSON array of <see cref="FileSystemEntry"/>, directories first. Omitting <c>path</c>
        /// lists the base itself.
        /// </summary>
        /// <returns>
        /// 200 with the listing; 400 if the path escapes the base; 404 if the directory does not exist.
        /// </returns>
        [HttpGet]
        public ActionResult<IReadOnlyList<FileSystemEntry>> List([FromQuery] string path = "") {
            try {
                return Ok(_fileSystem.ListDirectory(path));
            } catch (ArgumentException) {
                return BadRequest();
            } catch (DirectoryNotFoundException) {
                return NotFound();
            }
        }
    }
}

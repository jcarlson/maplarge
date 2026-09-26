using Microsoft.AspNetCore.Mvc;
using TestProject.Services;

namespace TestProject.Controllers {
    /// <summary>
    /// JSON API for browsing the configured base directory. The controller only translates
    /// HTTP to and from <see cref="DirectoryLister"/>; all path and listing logic lives there.
    /// </summary>
    [ApiController]
    [Route("api/dir")]
    public class DirectoryController : ControllerBase {
        private readonly DirectoryLister _lister;

        public DirectoryController(DirectoryLister lister) {
            _lister = lister;
        }

        /// <summary>
        /// GET /api/dir?path=photos — lists the entries directly inside <paramref name="path"/>
        /// as a JSON array of base-relative paths. Omitting <c>path</c> lists the base itself.
        /// </summary>
        /// <returns>
        /// 200 with the entries; 400 if the path escapes the base; 404 if the directory does not exist.
        /// </returns>
        [HttpGet]
        public ActionResult<IReadOnlyList<string>> List([FromQuery] string path = "") {
            try {
                return Ok(_lister.List(path));
            } catch (ArgumentException) {
                return BadRequest();
            } catch (DirectoryNotFoundException) {
                return NotFound();
            }
        }
    }
}

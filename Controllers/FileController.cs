using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using TestProject.Services;

namespace TestProject.Controllers {
    /// <summary>
    /// Serves individual files from the configured base directory. Like
    /// <see cref="DirectoryController"/>, it only maps HTTP to and from
    /// <see cref="FileSystemService"/>; path resolution and existence checks live there.
    /// </summary>
    [ApiController]
    [Route("api/file")]
    public class FileController : ControllerBase {
        private const string FallbackContentType = "application/octet-stream";
        private static readonly FileExtensionContentTypeProvider ContentTypes = new();

        private readonly FileSystemService _fileSystem;

        public FileController(FileSystemService fileSystem) {
            _fileSystem = fileSystem;
        }

        /// <summary>
        /// GET /api/file?path=photos/cat.jpg — sends the file as an attachment named after the
        /// file, with a MIME type inferred from its extension. Range requests are honoured so
        /// downloads can resume. <c>path</c> is required; omitting it is a 400.
        /// </summary>
        /// <returns>
        /// 200 with the file body; 400 if the path escapes the base; 404 if there is no file at the path.
        /// </returns>
        [HttpGet]
        public IActionResult DownloadFile([FromQuery] string path) {
            FileInfo file;
            try {
                file = _fileSystem.GetFileInfo(path);
            } catch (ArgumentException) {
                return BadRequest();
            } catch (FileNotFoundException) {
                return NotFound();
            }

            if (!ContentTypes.TryGetContentType(file.Name, out var contentType)) {
                contentType = FallbackContentType;
            }

            return PhysicalFile(file.FullName, contentType, file.Name, enableRangeProcessing: true);
        }
    }
}

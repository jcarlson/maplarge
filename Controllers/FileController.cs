using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using TestProject.Models;
using TestProject.Services;

namespace TestProject.Controllers {
    /// <summary>
    /// Serves and accepts individual files in the configured base directory. Like
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

        /// <summary>
        /// POST /api/file?path=photos with a multipart form containing one <c>file</c> part — writes
        /// the uploaded file into the directory <c>path</c>, named as it was on the client. Omitting
        /// <c>path</c> uploads into the base itself; omitting <c>file</c> is a 400. Existing files are
        /// never overwritten.
        /// </summary>
        /// <returns>
        /// 201 with the new <see cref="FileSystemEntry"/> and its download URL in <c>Location</c>; 400 if the path
        /// escapes the base or the file name is not a plain name; 404 if the directory does not exist;
        /// 409 if a file or directory with that name already exists.
        /// </returns>
        [HttpPost]
        public async Task<ActionResult<FileSystemEntry>> UploadFile(IFormFile file, [FromQuery] string path = "", CancellationToken cancellationToken = default) {
            FileSystemEntry entry;
            try {
                using var content = file.OpenReadStream();
                entry = await _fileSystem.WriteFileAsync(path, file.FileName, content, cancellationToken);
            } catch (ArgumentException) {
                return BadRequest();
            } catch (DirectoryNotFoundException) {
                return NotFound();
            } catch (EntryExistsException) {
                return Conflict();
            }

            return Created($"/api/file?path={Uri.EscapeDataString(entry.Path)}", entry);
        }
    }
}

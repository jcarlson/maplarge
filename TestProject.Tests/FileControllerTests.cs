using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TestProject.Controllers;
using TestProject.Models;
using TestProject.Services;

namespace TestProject.Tests
{
    /// <summary>
    /// Calls <see cref="FileController"/> actions directly over a scripted
    /// <see cref="StubFileSystemService"/>, so the mapping of each service outcome to an HTTP
    /// result is tested without a running host or a directory on disk.
    /// </summary>
    public class FileControllerTests
    {
        // Need not exist: the physical-file result opens it only when executed.
        private static readonly string BaseDirectory = Path.Combine(Path.GetTempPath(), "FileControllerTests");

        private readonly StubFileSystemService _fileSystem = new();
        private readonly FileController _controller;

        public FileControllerTests()
        {
            _controller = new FileController(_fileSystem);
        }

        [Fact]
        public void DownloadFile_ReturnsPhysicalFileAsNamedAttachment()
        {
            var info = new FileInfo(Path.Combine(BaseDirectory, "docs", "notes.txt"));
            string? asked = null;
            _fileSystem.OnGetFileInfo = path => { asked = path; return info; };

            var result = _controller.DownloadFile("docs/notes.txt");

            Assert.Equal("docs/notes.txt", asked);
            var file = Assert.IsType<PhysicalFileResult>(result);
            Assert.Equal(info.FullName, file.FileName);
            Assert.Equal("notes.txt", file.FileDownloadName);
            Assert.Equal("text/plain", file.ContentType);
            Assert.True(file.EnableRangeProcessing);
        }

        [Fact]
        public void DownloadFile_UnknownExtension_FallsBackToOctetStream()
        {
            _fileSystem.OnGetFileInfo = _ => new FileInfo(Path.Combine(BaseDirectory, "data.unknownext"));

            var result = _controller.DownloadFile("data.unknownext");

            Assert.Equal("application/octet-stream", Assert.IsType<PhysicalFileResult>(result).ContentType);
        }

        [Fact]
        public void DownloadFile_MissingFile_ReturnsNotFound()
        {
            _fileSystem.OnGetFileInfo = _ => throw new FileNotFoundException();

            var result = _controller.DownloadFile("nope.txt");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void DownloadFile_PathEscapingBase_ReturnsBadRequest()
        {
            _fileSystem.OnGetFileInfo = _ => throw new ArgumentException("escapes");

            var result = _controller.DownloadFile("../secret.txt");

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public void DownloadFile_OtherFailure_Propagates()
        {
            _fileSystem.OnGetFileInfo = _ => throw new IOException("disk on fire");

            Assert.Throws<IOException>(() => _controller.DownloadFile("notes.txt"));
        }

        [Fact]
        public async Task UploadFile_WritesTheFileIntoThePathUnderItsOwnName_ReturnsCreatedEntry()
        {
            var entry = new FileSystemEntry("docs/notes.txt", IsDirectory: false, Size: 5);
            (string directory, string name, string content)? written = null;
            _fileSystem.OnWriteFile = (directory, name, content) =>
            {
                written = (directory, name, new StreamReader(content).ReadToEnd());
                return entry;
            };

            var result = await _controller.UploadFile(Upload("notes.txt", "hello"), "docs");

            Assert.Equal(("docs", "notes.txt", "hello"), written);
            var created = Assert.IsType<CreatedResult>(result.Result);
            Assert.Same(entry, created.Value);
            Assert.Equal("/api/file?path=docs%2Fnotes.txt", created.Location);
        }

        [Fact]
        public async Task UploadFile_NoPath_WritesIntoBase()
        {
            string? directory = null;
            _fileSystem.OnWriteFile = (dir, name, _) => { directory = dir; return new FileSystemEntry(name, false, 5); };

            await _controller.UploadFile(Upload("notes.txt", "hello"));

            Assert.Equal("", directory);
        }

        [Fact]
        public async Task UploadFile_MissingDirectory_ReturnsNotFound()
        {
            _fileSystem.OnWriteFile = (_, _, _) => throw new DirectoryNotFoundException();

            var result = await _controller.UploadFile(Upload("notes.txt", "hello"), "nope");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UploadFile_ExistingEntry_ReturnsConflict()
        {
            _fileSystem.OnWriteFile = (_, _, _) => throw new EntryExistsException("taken");

            var result = await _controller.UploadFile(Upload("notes.txt", "replacement"));

            Assert.IsType<ConflictResult>(result.Result);
        }

        [Fact]
        public async Task UploadFile_EscapingPathOrBadName_ReturnsBadRequest()
        {
            _fileSystem.OnWriteFile = (_, _, _) => throw new ArgumentException("not a plain name");

            var result = await _controller.UploadFile(Upload("../notes.txt", "hello"));

            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UploadFile_OtherFailure_Propagates()
        {
            _fileSystem.OnWriteFile = (_, _, _) => throw new IOException("disk on fire");

            await Assert.ThrowsAsync<IOException>(() => _controller.UploadFile(Upload("notes.txt", "hello")));
        }

        /// <summary>An in-memory stand-in for the <c>file</c> part of a multipart upload.</summary>
        private static IFormFile Upload(string fileName, string content)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
        }
    }
}

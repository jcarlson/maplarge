using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TestProject.Controllers;
using TestProject.Models;
using TestProject.Services;

namespace TestProject.Tests {
    /// <summary>
    /// Calls <see cref="FileController"/> actions directly, backed by a real
    /// <see cref="FileSystemService"/> over a throwaway temp directory, so the HTTP mapping is
    /// tested without a running host.
    /// </summary>
    public class FileControllerTests : IDisposable {
        private readonly string _baseDirectory =
            Path.Combine(Path.GetTempPath(), "FileControllerTests-" + Guid.NewGuid());
        private readonly FileController _controller;

        public FileControllerTests() {
            Directory.CreateDirectory(_baseDirectory);
            _controller = new FileController(new FileSystemService(_baseDirectory));
        }

        public void Dispose() {
            Directory.Delete(_baseDirectory, recursive: true);
        }

        [Fact]
        public void DownloadFile_ExistingFile_ReturnsPhysicalFileAsNamedAttachment() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "docs"));
            File.WriteAllText(Path.Combine(_baseDirectory, "docs", "notes.txt"), "hello");

            var result = _controller.DownloadFile("docs/notes.txt");

            var file = Assert.IsType<PhysicalFileResult>(result);
            Assert.Equal(Path.Combine(_baseDirectory, "docs", "notes.txt"), file.FileName);
            Assert.Equal("notes.txt", file.FileDownloadName);
            Assert.Equal("text/plain", file.ContentType);
            Assert.True(file.EnableRangeProcessing);
        }

        [Fact]
        public void DownloadFile_UnknownExtension_FallsBackToOctetStream() {
            File.WriteAllText(Path.Combine(_baseDirectory, "data.unknownext"), "");

            var result = _controller.DownloadFile("data.unknownext");

            Assert.Equal("application/octet-stream", Assert.IsType<PhysicalFileResult>(result).ContentType);
        }

        [Fact]
        public void DownloadFile_MissingFile_ReturnsNotFound() {
            var result = _controller.DownloadFile("nope.txt");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void DownloadFile_Directory_ReturnsNotFound() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "docs"));

            var result = _controller.DownloadFile("docs");

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public void DownloadFile_PathEscapingBase_ReturnsBadRequest() {
            var result = _controller.DownloadFile("../secret.txt");

            Assert.IsType<BadRequestResult>(result);
        }

        [Fact]
        public async Task UploadFile_NewFile_WritesItAndReturnsCreatedEntry() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "docs"));

            var result = await _controller.UploadFile(Upload("notes.txt", "hello"), "docs");

            var created = Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal(new Entry(Path.Combine("docs", "notes.txt"), IsDirectory: false, Size: 5), created.Value);
            Assert.Equal("/api/file?path=" + Uri.EscapeDataString(Path.Combine("docs", "notes.txt")), created.Location);
            Assert.Equal("hello", File.ReadAllText(Path.Combine(_baseDirectory, "docs", "notes.txt")));
        }

        [Fact]
        public async Task UploadFile_NoPath_WritesIntoBase() {
            var result = await _controller.UploadFile(Upload("notes.txt", "hello"));

            Assert.IsType<CreatedResult>(result.Result);
            Assert.Equal("hello", File.ReadAllText(Path.Combine(_baseDirectory, "notes.txt")));
        }

        [Fact]
        public async Task UploadFile_MissingDirectory_ReturnsNotFound() {
            var result = await _controller.UploadFile(Upload("notes.txt", "hello"), "nope");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task UploadFile_ExistingEntry_ReturnsConflict() {
            File.WriteAllText(Path.Combine(_baseDirectory, "notes.txt"), "original");

            var result = await _controller.UploadFile(Upload("notes.txt", "replacement"));

            Assert.IsType<ConflictResult>(result.Result);
            Assert.Equal("original", File.ReadAllText(Path.Combine(_baseDirectory, "notes.txt")));
        }

        [Fact]
        public async Task UploadFile_PathEscapingBase_ReturnsBadRequest() {
            var result = await _controller.UploadFile(Upload("notes.txt", "hello"), "../");

            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public async Task UploadFile_FileNameWithSeparator_ReturnsBadRequest() {
            var result = await _controller.UploadFile(Upload("../notes.txt", "hello"));

            Assert.IsType<BadRequestResult>(result.Result);
        }

        /// <summary>An in-memory stand-in for the <c>file</c> part of a multipart upload.</summary>
        private static IFormFile Upload(string fileName, string content) {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "file", fileName);
        }
    }
}

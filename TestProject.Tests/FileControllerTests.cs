using Microsoft.AspNetCore.Mvc;
using TestProject.Controllers;
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
    }
}

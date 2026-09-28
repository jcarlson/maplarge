using Microsoft.AspNetCore.Mvc;
using TestProject.Controllers;
using TestProject.Models;
using TestProject.Services;

namespace TestProject.Tests {
    /// <summary>
    /// Calls <see cref="DirectoryController"/> actions directly, backed by a real
    /// <see cref="FileSystemService"/> over a throwaway temp directory, so the HTTP mapping is
    /// tested without a running host.
    /// </summary>
    public class DirectoryControllerTests : IDisposable {
        private readonly string _baseDirectory =
            Path.Combine(Path.GetTempPath(), "DirectoryControllerTests-" + Guid.NewGuid());
        private readonly DirectoryController _controller;

        public DirectoryControllerTests() {
            Directory.CreateDirectory(_baseDirectory);
            _controller = new DirectoryController(new FileSystemService(_baseDirectory));
        }

        public void Dispose() {
            Directory.Delete(_baseDirectory, recursive: true);
        }

        [Fact]
        public void List_DefaultPath_ReturnsOkWithBaseEntries() {
            File.WriteAllText(Path.Combine(_baseDirectory, "notes.txt"), "hello");
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));

            var result = _controller.List();

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var entries = Assert.IsAssignableFrom<IReadOnlyList<FileSystemEntry>>(ok.Value);
            Assert.Equal(
                new[] {
                    new FileSystemEntry("photos", IsDirectory: true, Size: null),
                    new FileSystemEntry("notes.txt", IsDirectory: false, Size: 5),
                },
                entries);
        }

        [Fact]
        public void List_Subdirectory_ReturnsOkWithEntriesRelativeToBase() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cat.jpg"), "");

            var result = _controller.List("photos");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var entries = Assert.IsAssignableFrom<IReadOnlyList<FileSystemEntry>>(ok.Value);
            Assert.Equal(new[] { new FileSystemEntry("photos/cat.jpg", IsDirectory: false, Size: 0) }, entries);
        }

        [Fact]
        public void List_PathEscapingBase_ReturnsBadRequest() {
            var result = _controller.List("../");

            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public void List_MissingDirectory_ReturnsNotFound() {
            var result = _controller.List("nope");

            Assert.IsType<NotFoundResult>(result.Result);
        }
    }
}

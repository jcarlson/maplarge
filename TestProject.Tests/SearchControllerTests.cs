using Microsoft.AspNetCore.Mvc;
using TestProject.Controllers;
using TestProject.Models;
using TestProject.Services;

namespace TestProject.Tests {
    /// <summary>
    /// Calls <see cref="SearchController"/> actions directly, backed by a real
    /// <see cref="FileSystemService"/> over a throwaway temp directory, so the HTTP mapping is
    /// tested without a running host.
    /// </summary>
    public class SearchControllerTests : IDisposable {
        private readonly string _baseDirectory =
            Path.Combine(Path.GetTempPath(), "SearchControllerTests-" + Guid.NewGuid());
        private readonly SearchController _controller;

        public SearchControllerTests() {
            Directory.CreateDirectory(_baseDirectory);
            _controller = new SearchController(new FileSystemService(_baseDirectory));
        }

        public void Dispose() {
            Directory.Delete(_baseDirectory, recursive: true);
        }

        [Fact]
        public void Search_ReturnsOkWithMatchesFromAllDepths() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cat.jpg"), "meow");
            File.WriteAllText(Path.Combine(_baseDirectory, "dog.jpg"), "");

            var result = _controller.Search("cat");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var entries = Assert.IsAssignableFrom<IReadOnlyList<FileSystemEntry>>(ok.Value);
            Assert.Equal(new[] { new FileSystemEntry(Path.Combine("photos", "cat.jpg"), IsDirectory: false, Size: 4) }, entries);
        }

        [Fact]
        public void Search_NoMatches_ReturnsOkWithNoEntries() {
            File.WriteAllText(Path.Combine(_baseDirectory, "dog.jpg"), "");

            var result = _controller.Search("cat");

            var ok = Assert.IsType<OkObjectResult>(result.Result);
            var entries = Assert.IsAssignableFrom<IReadOnlyList<FileSystemEntry>>(ok.Value);
            Assert.Empty(entries);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Search_BlankQuery_ReturnsBadRequest(string query) {
            var result = _controller.Search(query);

            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public void Search_OmittedQuery_ReturnsBadRequest() {
            var result = _controller.Search();

            Assert.IsType<BadRequestResult>(result.Result);
        }
    }
}

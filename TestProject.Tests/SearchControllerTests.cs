using Microsoft.AspNetCore.Mvc;
using TestProject.Controllers;
using TestProject.Models;

namespace TestProject.Tests
{
    /// <summary>
    /// Calls <see cref="SearchController"/> actions directly over a scripted
    /// <see cref="StubFileSystemService"/>, so the mapping of each service outcome to an HTTP
    /// result is tested without a running host or a directory on disk.
    /// </summary>
    public class SearchControllerTests
    {
        private static readonly FileSystemEntry[] Matches = {
            new("photos/cat.jpg", IsDirectory: false, Size: 4),
        };

        private readonly StubFileSystemService _fileSystem = new();
        private readonly SearchController _controller;

        public SearchControllerTests()
        {
            _controller = new SearchController(_fileSystem);
        }

        [Fact]
        public void Search_ReturnsOkWithTheMatchesForThePathAndQuery()
        {
            (string path, string query)? searched = null;
            _fileSystem.OnSearch = (path, query) => { searched = (path, query); return Matches; };

            var result = _controller.Search("cat", "photos");

            Assert.Equal(("photos", "cat"), searched);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(Matches, ok.Value);
        }

        [Fact]
        public void Search_DefaultPath_SearchesTheBase()
        {
            (string path, string query)? searched = null;
            _fileSystem.OnSearch = (path, query) => { searched = (path, query); return Matches; };

            _controller.Search("cat");

            Assert.Equal(("", "cat"), searched);
        }

        [Fact]
        public void Search_OmittedQuery_PassesAnEmptyQueryForTheServiceToReject()
        {
            (string path, string query)? searched = null;
            _fileSystem.OnSearch = (path, query) => { searched = (path, query); return Matches; };

            _controller.Search();

            Assert.Equal(("", ""), searched);
        }

        [Fact]
        public void Search_BlankQueryOrEscapingPath_ReturnsBadRequest()
        {
            _fileSystem.OnSearch = (_, _) => throw new ArgumentException("blank");

            var result = _controller.Search("   ");

            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public void Search_MissingDirectory_ReturnsNotFound()
        {
            _fileSystem.OnSearch = (_, _) => throw new DirectoryNotFoundException();

            var result = _controller.Search("cat", "nope");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void Search_OtherFailure_Propagates()
        {
            _fileSystem.OnSearch = (_, _) => throw new IOException("disk on fire");

            Assert.Throws<IOException>(() => _controller.Search("cat"));
        }
    }
}

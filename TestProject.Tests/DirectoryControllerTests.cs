using Microsoft.AspNetCore.Mvc;
using TestProject.Controllers;
using TestProject.Models;

namespace TestProject.Tests
{
    /// <summary>
    /// Calls <see cref="DirectoryController"/> actions directly over a scripted
    /// <see cref="StubFileSystemService"/>, so the mapping of each service outcome to an HTTP
    /// result is tested without a running host or a directory on disk.
    /// </summary>
    public class DirectoryControllerTests
    {
        private static readonly FileSystemEntry[] Entries = {
            new("photos", IsDirectory: true, Size: null),
            new("notes.txt", IsDirectory: false, Size: 5),
        };

        private readonly StubFileSystemService _fileSystem = new();
        private readonly DirectoryController _controller;

        public DirectoryControllerTests()
        {
            _controller = new DirectoryController(_fileSystem);
        }

        [Fact]
        public void List_ReturnsOkWithTheEntriesForThePath()
        {
            string? listed = null;
            _fileSystem.OnListDirectory = path => { listed = path; return Entries; };

            var result = _controller.List("photos");

            Assert.Equal("photos", listed);
            var ok = Assert.IsType<OkObjectResult>(result.Result);
            Assert.Same(Entries, ok.Value);
        }

        [Fact]
        public void List_DefaultPath_ListsTheBase()
        {
            string? listed = null;
            _fileSystem.OnListDirectory = path => { listed = path; return Entries; };

            _controller.List();

            Assert.Equal("", listed);
        }

        [Fact]
        public void List_PathEscapingBase_ReturnsBadRequest()
        {
            _fileSystem.OnListDirectory = _ => throw new ArgumentException("escapes");

            var result = _controller.List("../");

            Assert.IsType<BadRequestResult>(result.Result);
        }

        [Fact]
        public void List_MissingDirectory_ReturnsNotFound()
        {
            _fileSystem.OnListDirectory = _ => throw new DirectoryNotFoundException();

            var result = _controller.List("nope");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void List_OtherFailure_Propagates()
        {
            _fileSystem.OnListDirectory = _ => throw new IOException("disk on fire");

            Assert.Throws<IOException>(() => _controller.List("photos"));
        }
    }
}

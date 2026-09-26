using TestProject.Services;

namespace TestProject.Tests {
    /// <summary>
    /// Exercises <see cref="DirectoryLister"/> against a throwaway directory under the
    /// system temp folder, created fresh for each test and removed afterwards.
    /// </summary>
    public class DirectoryListerTests : IDisposable {
        private readonly string _baseDirectory =
            Path.Combine(Path.GetTempPath(), "DirectoryListerTests-" + Guid.NewGuid());

        public DirectoryListerTests() {
            Directory.CreateDirectory(_baseDirectory);
        }

        public void Dispose() {
            Directory.Delete(_baseDirectory, recursive: true);
        }

        [Fact]
        public void List_ReturnsFilesAndDirectoriesRelativeToBase() {
            File.WriteAllText(Path.Combine(_baseDirectory, "notes.txt"), "hello");
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "nested.jpg"), "");

            var entries = new DirectoryLister(_baseDirectory).List();

            Assert.Equal(new[] { "notes.txt", "photos" }, entries);
        }

        [Fact]
        public void List_ReturnsEntriesInOrdinalOrder() {
            // Names chosen so ordinal order differs from natural/numeric order.
            foreach (var name in new[] { "b.txt", "a.txt", "10.txt", "2.txt" }) {
                File.WriteAllText(Path.Combine(_baseDirectory, name), "");
            }

            var entries = new DirectoryLister(_baseDirectory).List();

            Assert.Equal(new[] { "10.txt", "2.txt", "a.txt", "b.txt" }, entries);
        }

        [Theory]
        [InlineData("photos")]
        [InlineData("/photos")]
        [InlineData("photos/")]
        public void List_Subdirectory_ReturnsEntriesRelativeToBase(string path) {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos", "2024"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cat.jpg"), "");
            File.WriteAllText(Path.Combine(_baseDirectory, "unrelated.txt"), "");

            var entries = new DirectoryLister(_baseDirectory).List(path);

            Assert.Equal(new[] { Path.Combine("photos", "2024"), Path.Combine("photos", "cat.jpg") }, entries);
        }

        [Fact]
        public void List_MissingSubdirectory_Throws() {
            var lister = new DirectoryLister(_baseDirectory);

            Assert.Throws<DirectoryNotFoundException>(() => lister.List("nope"));
        }

        [Fact]
        public void List_PathEscapingBase_Throws() {
            var lister = new DirectoryLister(_baseDirectory);

            Assert.Throws<ArgumentException>(() => lister.List("../"));
        }

        [Fact]
        public void List_EmptyDirectory_ReturnsEmptyList() {
            var entries = new DirectoryLister(_baseDirectory).List();

            Assert.Empty(entries);
        }

        [Fact]
        public void Constructor_NormalizesBaseDirectoryPath() {
            File.WriteAllText(Path.Combine(_baseDirectory, "file.txt"), "");
            var unnormalized = _baseDirectory + Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar;

            var entries = new DirectoryLister(unnormalized).List();

            Assert.Equal(new[] { "file.txt" }, entries);
        }

        [Fact]
        public void Constructor_MissingBaseDirectory_Throws() {
            var missing = Path.Combine(_baseDirectory, "does-not-exist");

            Assert.Throws<DirectoryNotFoundException>(() => new DirectoryLister(missing));
        }
    }
}

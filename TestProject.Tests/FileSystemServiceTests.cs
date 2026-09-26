using TestProject.Services;

namespace TestProject.Tests {
    /// <summary>
    /// Exercises <see cref="FileSystemService"/> against a throwaway directory under the
    /// system temp folder, created fresh for each test and removed afterwards.
    /// </summary>
    public class FileSystemServiceTests : IDisposable {
        private readonly string _baseDirectory =
            Path.Combine(Path.GetTempPath(), "FileSystemServiceTests-" + Guid.NewGuid());

        public FileSystemServiceTests() {
            Directory.CreateDirectory(_baseDirectory);
        }

        public void Dispose() {
            Directory.Delete(_baseDirectory, recursive: true);
        }

        [Fact]
        public void ListDirectory_ReturnsFilesAndDirectoriesRelativeToBase() {
            File.WriteAllText(Path.Combine(_baseDirectory, "notes.txt"), "hello");
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "nested.jpg"), "");

            var entries = new FileSystemService(_baseDirectory).ListDirectory();

            Assert.Equal(new[] { "notes.txt", "photos" }, entries);
        }

        [Fact]
        public void ListDirectory_ReturnsEntriesInOrdinalOrder() {
            // Names chosen so ordinal order differs from natural/numeric order.
            foreach (var name in new[] { "b.txt", "a.txt", "10.txt", "2.txt" }) {
                File.WriteAllText(Path.Combine(_baseDirectory, name), "");
            }

            var entries = new FileSystemService(_baseDirectory).ListDirectory();

            Assert.Equal(new[] { "10.txt", "2.txt", "a.txt", "b.txt" }, entries);
        }

        [Theory]
        [InlineData("photos")]
        [InlineData("/photos")]
        [InlineData("photos/")]
        public void ListDirectory_Subdirectory_ReturnsEntriesRelativeToBase(string path) {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos", "2024"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cat.jpg"), "");
            File.WriteAllText(Path.Combine(_baseDirectory, "unrelated.txt"), "");

            var entries = new FileSystemService(_baseDirectory).ListDirectory(path);

            Assert.Equal(new[] { Path.Combine("photos", "2024"), Path.Combine("photos", "cat.jpg") }, entries);
        }

        [Fact]
        public void ListDirectory_MissingSubdirectory_Throws() {
            var service = new FileSystemService(_baseDirectory);

            Assert.Throws<DirectoryNotFoundException>(() => service.ListDirectory("nope"));
        }

        [Fact]
        public void ListDirectory_PathEscapingBase_Throws() {
            var service = new FileSystemService(_baseDirectory);

            Assert.Throws<ArgumentException>(() => service.ListDirectory("../"));
        }

        [Fact]
        public void ListDirectory_EmptyDirectory_ReturnsEmptyList() {
            var entries = new FileSystemService(_baseDirectory).ListDirectory();

            Assert.Empty(entries);
        }

        [Fact]
        public void GetFileInfo_ReturnsInfoForExistingFile() {
            File.WriteAllText(Path.Combine(_baseDirectory, "notes.txt"), "hello");

            var info = new FileSystemService(_baseDirectory).GetFileInfo("notes.txt");

            Assert.Equal("notes.txt", info.Name);
            Assert.Equal(Path.Combine(_baseDirectory, "notes.txt"), info.FullName);
            Assert.Equal(5, info.Length);
        }

        [Theory]
        [InlineData("photos/cat.jpg")]
        [InlineData("/photos/cat.jpg")]
        public void GetFileInfo_NestedFile_ResolvesRelativeToBase(string path) {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cat.jpg"), "meow");

            var info = new FileSystemService(_baseDirectory).GetFileInfo(path);

            Assert.Equal(Path.Combine(_baseDirectory, "photos", "cat.jpg"), info.FullName);
        }

        [Fact]
        public void GetFileInfo_MissingFile_Throws() {
            var service = new FileSystemService(_baseDirectory);

            Assert.Throws<FileNotFoundException>(() => service.GetFileInfo("nope.txt"));
        }

        [Theory]
        [InlineData("")]
        [InlineData("photos")]
        public void GetFileInfo_DirectoryAtPath_ThrowsFileNotFound(string path) {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            var service = new FileSystemService(_baseDirectory);

            Assert.Throws<FileNotFoundException>(() => service.GetFileInfo(path));
        }

        [Fact]
        public void GetFileInfo_PathEscapingBase_Throws() {
            var service = new FileSystemService(_baseDirectory);

            Assert.Throws<ArgumentException>(() => service.GetFileInfo("../secret.txt"));
        }

        [Fact]
        public void Constructor_NormalizesBaseDirectoryPath() {
            File.WriteAllText(Path.Combine(_baseDirectory, "file.txt"), "");
            var unnormalized = _baseDirectory + Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar;

            var entries = new FileSystemService(unnormalized).ListDirectory();

            Assert.Equal(new[] { "file.txt" }, entries);
        }

        [Fact]
        public void Constructor_MissingBaseDirectory_Throws() {
            var missing = Path.Combine(_baseDirectory, "does-not-exist");

            Assert.Throws<DirectoryNotFoundException>(() => new FileSystemService(missing));
        }
    }
}

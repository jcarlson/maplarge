using TestProject.Models;
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

            Assert.Equal(
                new[] {
                    new FileSystemEntry("photos", IsDirectory: true, Size: null),
                    new FileSystemEntry("notes.txt", IsDirectory: false, Size: 5),
                },
                entries);
        }

        [Theory]
        [InlineData("photos")]
        [InlineData("/photos")]
        [InlineData("photos/")]
        public void ListDirectory_Subdirectory_ReturnsEntriesRelativeToBase(string path) {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos", "2024"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cat.jpg"), "meow");
            File.WriteAllText(Path.Combine(_baseDirectory, "unrelated.txt"), "");

            var entries = new FileSystemService(_baseDirectory).ListDirectory(path);

            Assert.Equal(
                new[] {
                    new FileSystemEntry("photos/2024", IsDirectory: true, Size: null),
                    new FileSystemEntry("photos/cat.jpg", IsDirectory: false, Size: 4),
                },
                entries);
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
        public void ListDirectory_EmptyDirectory_ReturnsNoEntries() {
            var entries = new FileSystemService(_baseDirectory).ListDirectory();

            Assert.Empty(entries);
        }

        [Fact]
        public void ListDirectory_OrdersDirectoriesBeforeFiles_EachInOrdinalOrder() {
            // Names chosen so ordinal order differs from natural/numeric order and so a file
            // would sort before a directory if the two kinds were not separated.
            foreach (var name in new[] { "z", "10", "2" }) {
                Directory.CreateDirectory(Path.Combine(_baseDirectory, name));
            }
            foreach (var name in new[] { "b.txt", "a.txt", "10.txt", "2.txt" }) {
                File.WriteAllText(Path.Combine(_baseDirectory, name), "");
            }

            var entries = new FileSystemService(_baseDirectory).ListDirectory();

            Assert.Equal(new[] { "10", "2", "z", "10.txt", "2.txt", "a.txt", "b.txt" }, entries.Select(e => e.Path));
        }

        [Fact]
        public void Search_FindsMatchesAtEveryDepthOrderedLikeADirectoryListing() {
            File.WriteAllText(Path.Combine(_baseDirectory, "cat.txt"), "12");
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos", "cats"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cats", "tabby-cat.jpg"), "123");
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "dog.jpg"), "");

            var entries = new FileSystemService(_baseDirectory).Search("", "cat");

            Assert.Equal(
                new[] {
                    new FileSystemEntry("photos/cats", IsDirectory: true, Size: null),
                    new FileSystemEntry("cat.txt", IsDirectory: false, Size: 2),
                    new FileSystemEntry("photos/cats/tabby-cat.jpg", IsDirectory: false, Size: 3),
                },
                entries);
        }

        [Fact]
        public void Search_IgnoresCase() {
            File.WriteAllText(Path.Combine(_baseDirectory, "README.md"), "");

            var entries = new FileSystemService(_baseDirectory).Search("", "readme");

            Assert.Equal(new[] { new FileSystemEntry("README.md", IsDirectory: false, Size: 0) }, entries);
        }

        [Fact]
        public void Search_MatchesTheEntryNameOnlyNotItsParentDirectories() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cat.jpg"), "");

            var entries = new FileSystemService(_baseDirectory).Search("", "photos");

            Assert.Equal(new[] { new FileSystemEntry("photos", IsDirectory: true, Size: null) }, entries);
        }

        [Fact]
        public void Search_NoMatches_ReturnsNoEntries() {
            File.WriteAllText(Path.Combine(_baseDirectory, "dog.jpg"), "");

            var entries = new FileSystemService(_baseDirectory).Search("", "cat");

            Assert.Empty(entries);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Search_BlankQuery_Throws(string query) {
            var service = new FileSystemService(_baseDirectory);

            Assert.Throws<ArgumentException>(() => service.Search("", query));
        }

        [Theory]
        [InlineData("photos")]
        [InlineData("/photos")]
        [InlineData("photos/")]
        public void Search_Subdirectory_FindsOnlyEntriesBelowIt_WithPathsRelativeToBase(string path) {
            File.WriteAllText(Path.Combine(_baseDirectory, "cat.txt"), "");
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos", "cats"));
            File.WriteAllText(Path.Combine(_baseDirectory, "photos", "cats", "tabby-cat.jpg"), "123");
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "docs"));
            File.WriteAllText(Path.Combine(_baseDirectory, "docs", "cat-care.md"), "");

            var entries = new FileSystemService(_baseDirectory).Search(path, "cat");

            Assert.Equal(
                new[] {
                    new FileSystemEntry("photos/cats", IsDirectory: true, Size: null),
                    new FileSystemEntry("photos/cats/tabby-cat.jpg", IsDirectory: false, Size: 3),
                },
                entries);
        }

        [Fact]
        public void Search_MissingSubdirectory_Throws() {
            var service = new FileSystemService(_baseDirectory);

            Assert.Throws<DirectoryNotFoundException>(() => service.Search("nope", "cat"));
        }

        [Fact]
        public void Search_PathEscapingBase_Throws() {
            var service = new FileSystemService(_baseDirectory);

            Assert.Throws<ArgumentException>(() => service.Search("../", "cat"));
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
        public async Task WriteFileAsync_WritesContentIntoBase_ReturnsFileEntry() {
            using var content = new MemoryStream("hello"u8.ToArray());

            var entry = await new FileSystemService(_baseDirectory).WriteFileAsync("", "notes.txt", content);

            Assert.Equal(new FileSystemEntry("notes.txt", IsDirectory: false, Size: 5), entry);
            Assert.Equal("hello", File.ReadAllText(Path.Combine(_baseDirectory, "notes.txt")));
        }

        [Theory]
        [InlineData("photos")]
        [InlineData("/photos")]
        [InlineData("photos/")]
        public async Task WriteFileAsync_Subdirectory_WritesRelativeToBase(string directoryPath) {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            using var content = new MemoryStream("meow"u8.ToArray());

            var entry = await new FileSystemService(_baseDirectory).WriteFileAsync(directoryPath, "cat.jpg", content);

            Assert.Equal(new FileSystemEntry("photos/cat.jpg", IsDirectory: false, Size: 4), entry);
            Assert.Equal("meow", File.ReadAllText(Path.Combine(_baseDirectory, "photos", "cat.jpg")));
        }

        [Fact]
        public async Task WriteFileAsync_MissingDirectory_Throws() {
            var service = new FileSystemService(_baseDirectory);

            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => service.WriteFileAsync("nope", "notes.txt", Stream.Null));
        }

        [Fact]
        public async Task WriteFileAsync_DirectoryPathIsAFile_Throws() {
            File.WriteAllText(Path.Combine(_baseDirectory, "notes.txt"), "");
            var service = new FileSystemService(_baseDirectory);

            await Assert.ThrowsAsync<DirectoryNotFoundException>(
                () => service.WriteFileAsync("notes.txt", "inner.txt", Stream.Null));
        }

        [Fact]
        public async Task WriteFileAsync_PathEscapingBase_Throws() {
            var service = new FileSystemService(_baseDirectory);

            await Assert.ThrowsAsync<ArgumentException>(
                () => service.WriteFileAsync("../", "notes.txt", Stream.Null));
        }

        [Fact]
        public async Task WriteFileAsync_ExistingFile_ThrowsAndLeavesFileUntouched() {
            File.WriteAllText(Path.Combine(_baseDirectory, "notes.txt"), "original");
            using var content = new MemoryStream("replacement"u8.ToArray());
            var service = new FileSystemService(_baseDirectory);

            await Assert.ThrowsAsync<EntryExistsException>(() => service.WriteFileAsync("", "notes.txt", content));

            Assert.Equal("original", File.ReadAllText(Path.Combine(_baseDirectory, "notes.txt")));
        }

        [Fact]
        public async Task WriteFileAsync_ExistingDirectoryWithSameName_Throws() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "notes"));
            var service = new FileSystemService(_baseDirectory);

            await Assert.ThrowsAsync<EntryExistsException>(() => service.WriteFileAsync("", "notes", Stream.Null));
        }

        [Theory]
        [InlineData("")]
        [InlineData(".")]
        [InlineData("..")]
        [InlineData("sub/notes.txt")]
        [InlineData("../notes.txt")]
        public async Task WriteFileAsync_FileNameNotASingleSegment_ThrowsAndWritesNothing(string fileName) {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "sub"));
            var service = new FileSystemService(_baseDirectory);

            await Assert.ThrowsAsync<ArgumentException>(() => service.WriteFileAsync("sub", fileName, Stream.Null));

            Assert.Equal(new[] { Path.Combine(_baseDirectory, "sub") }, Directory.GetFileSystemEntries(_baseDirectory));
            Assert.Empty(Directory.GetFileSystemEntries(Path.Combine(_baseDirectory, "sub")));
        }

        [Fact]
        public void Constructor_NormalizesBaseDirectoryPath() {
            File.WriteAllText(Path.Combine(_baseDirectory, "file.txt"), "");
            var unnormalized = _baseDirectory + Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar;

            var entries = new FileSystemService(unnormalized).ListDirectory();

            Assert.Equal(new[] { new FileSystemEntry("file.txt", IsDirectory: false, Size: 0) }, entries);
        }

        [Fact]
        public void Constructor_MissingBaseDirectory_Throws() {
            var missing = Path.Combine(_baseDirectory, "does-not-exist");

            Assert.Throws<DirectoryNotFoundException>(() => new FileSystemService(missing));
        }
    }
}

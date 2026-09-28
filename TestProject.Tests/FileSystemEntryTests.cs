using TestProject.Models;

namespace TestProject.Tests {
    /// <summary>
    /// Exercises <see cref="FileSystemEntry.From"/> against a throwaway directory under the system temp
    /// folder. Real files are needed because a file's size is read from the disk.
    /// </summary>
    public class FileSystemEntryTests : IDisposable {
        private readonly string _baseDirectory =
            Path.Combine(Path.GetTempPath(), "FileSystemEntryTests-" + Guid.NewGuid());

        public FileSystemEntryTests() {
            Directory.CreateDirectory(_baseDirectory);
        }

        public void Dispose() {
            Directory.Delete(_baseDirectory, recursive: true);
        }

        [Fact]
        public void From_File_HasRelativePathWithForwardSlashesAndSize() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            var file = Path.Combine(_baseDirectory, "photos", "cat.jpg");
            File.WriteAllText(file, "meow");

            var entry = FileSystemEntry.From(_baseDirectory, new FileInfo(file));

            Assert.Equal(new FileSystemEntry("photos/cat.jpg", IsDirectory: false, Size: 4), entry);
        }

        [Fact]
        public void From_EmptyFile_HasSizeZero() {
            var file = Path.Combine(_baseDirectory, "empty.txt");
            File.WriteAllText(file, "");

            var entry = FileSystemEntry.From(_baseDirectory, new FileInfo(file));

            Assert.Equal(0, entry.Size);
        }

        [Fact]
        public void From_Directory_HasRelativePathAndNoSize() {
            var directory = Path.Combine(_baseDirectory, "photos");
            Directory.CreateDirectory(directory);

            var entry = FileSystemEntry.From(_baseDirectory, new DirectoryInfo(directory));

            Assert.Equal(new FileSystemEntry("photos", IsDirectory: true, Size: null), entry);
        }
    }
}

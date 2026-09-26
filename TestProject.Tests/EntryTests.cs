using TestProject.Models;

namespace TestProject.Tests {
    /// <summary>
    /// Exercises <see cref="Entry.From"/> against a throwaway directory under the system temp
    /// folder. Real files are needed because a file's size is read from the disk.
    /// </summary>
    public class EntryTests : IDisposable {
        private readonly string _baseDirectory =
            Path.Combine(Path.GetTempPath(), "EntryTests-" + Guid.NewGuid());

        public EntryTests() {
            Directory.CreateDirectory(_baseDirectory);
        }

        public void Dispose() {
            Directory.Delete(_baseDirectory, recursive: true);
        }

        [Fact]
        public void From_File_HasRelativePathAndSize() {
            Directory.CreateDirectory(Path.Combine(_baseDirectory, "photos"));
            var file = Path.Combine(_baseDirectory, "photos", "cat.jpg");
            File.WriteAllText(file, "meow");

            var entry = Entry.From(_baseDirectory, new FileInfo(file));

            Assert.Equal(new Entry(Path.Combine("photos", "cat.jpg"), IsDirectory: false, Size: 4), entry);
        }

        [Fact]
        public void From_EmptyFile_HasSizeZero() {
            var file = Path.Combine(_baseDirectory, "empty.txt");
            File.WriteAllText(file, "");

            var entry = Entry.From(_baseDirectory, new FileInfo(file));

            Assert.Equal(0, entry.Size);
        }

        [Fact]
        public void From_Directory_HasRelativePathAndNoSize() {
            var directory = Path.Combine(_baseDirectory, "photos");
            Directory.CreateDirectory(directory);

            var entry = Entry.From(_baseDirectory, new DirectoryInfo(directory));

            Assert.Equal(new Entry("photos", IsDirectory: true, Size: null), entry);
        }
    }
}

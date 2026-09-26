using TestProject.Models;

namespace TestProject.Tests {
    /// <summary>
    /// Exercises <see cref="Listing"/> with <see cref="FileInfo"/> and <see cref="DirectoryInfo"/>
    /// objects built for paths that do not exist. The listing only reads names and types from
    /// them, so no directory has to be created.
    /// </summary>
    public class ListingTests {
        private static readonly string Base = Path.GetFullPath(Path.Combine(Path.GetTempPath(), "ListingTests"));

        private static FileInfo File(params string[] segments) => new(Path.Combine(Base, Path.Combine(segments)));
        private static DirectoryInfo Dir(params string[] segments) => new(Path.Combine(Base, Path.Combine(segments)));

        [Fact]
        public void Entries_HavePathsRelativeToBase() {
            var listing = new Listing(Base, new FileSystemInfo[] { File("photos", "cat.jpg") });

            Assert.Equal(Path.Combine("photos", "cat.jpg"), Assert.Single(listing.Entries).Path);
        }

        [Fact]
        public void Entries_MarkDirectoriesAndFiles() {
            var listing = new Listing(Base, new FileSystemInfo[] { File("notes.txt"), Dir("photos") });

            Assert.Equal(
                new[] { new Entry("photos", IsDirectory: true), new Entry("notes.txt", IsDirectory: false) },
                listing.Entries);
        }

        [Fact]
        public void Entries_ListDirectoriesBeforeFiles_EachInOrdinalOrder() {
            // Names chosen so ordinal order differs from natural/numeric order and so a file
            // would sort before a directory if the two kinds were not separated.
            var listing = new Listing(Base, new FileSystemInfo[] {
                File("b.txt"), Dir("z"), File("a.txt"), Dir("10"), File("10.txt"), Dir("2"), File("2.txt"),
            });

            Assert.Equal(new[] { "10", "2", "z", "10.txt", "2.txt", "a.txt", "b.txt" }, listing.Entries.Select(e => e.Path));
        }

        [Fact]
        public void Entries_EmptyInput_IsEmpty() {
            var listing = new Listing(Base, Array.Empty<FileSystemInfo>());

            Assert.Empty(listing.Entries);
        }
    }
}

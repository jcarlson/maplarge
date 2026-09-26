using TestProject.Models;

namespace TestProject.Tests {
    /// <summary>
    /// Exercises <see cref="Listing"/> with hand-built <see cref="Entry"/> values. The listing
    /// only orders and totals what it is given, so no directory has to be created.
    /// </summary>
    public class ListingTests {
        private static Entry File(string path, long size = 0) => new(path, IsDirectory: false, Size: size);
        private static Entry Dir(string path) => new(path, IsDirectory: true, Size: null);

        [Fact]
        public void Entries_ListDirectoriesBeforeFiles_EachInOrdinalOrder() {
            // Names chosen so ordinal order differs from natural/numeric order and so a file
            // would sort before a directory if the two kinds were not separated.
            var listing = new Listing(new[] {
                File("b.txt"), Dir("z"), File("a.txt"), Dir("10"), File("10.txt"), Dir("2"), File("2.txt"),
            });

            Assert.Equal(new[] { "10", "2", "z", "10.txt", "2.txt", "a.txt", "b.txt" }, listing.Entries.Select(e => e.Path));
        }

        [Fact]
        public void Entries_EmptyInput_IsEmpty() {
            var listing = new Listing(Array.Empty<Entry>());

            Assert.Empty(listing.Entries);
        }

        [Fact]
        public void TotalSize_SumsFileSizes() {
            var listing = new Listing(new[] { File("a.txt", 5), File("b.txt", 7) });

            Assert.Equal(12, listing.TotalSize);
        }

        [Fact]
        public void TotalSize_IgnoresDirectories() {
            var listing = new Listing(new[] { Dir("photos"), File("notes.txt", 5) });

            Assert.Equal(5, listing.TotalSize);
        }

        [Fact]
        public void TotalSize_EmptyInput_IsZero() {
            var listing = new Listing(Array.Empty<Entry>());

            Assert.Equal(0, listing.TotalSize);
        }

        [Fact]
        public void TotalSize_ExceedsInt32Range() {
            var listing = new Listing(new[] { File("a.bin", 3L * int.MaxValue), File("b.bin", 3L * int.MaxValue) });

            Assert.Equal(6L * int.MaxValue, listing.TotalSize);
        }
    }
}

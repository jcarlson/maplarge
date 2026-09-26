namespace TestProject.Models {
    /// <summary>
    /// A set of file-system entries as the API presents it: the shape every listing-style
    /// response (a directory's contents, and later search results) serialises to. Orders the
    /// entries and totals their sizes; it never touches the disk itself, so it is unit-tested
    /// without one. Reading entries off the disk is <see cref="Entry.From"/>'s job.
    /// </summary>
    public class Listing {
        /// <param name="entries">The entries to present, in any order.</param>
        public Listing(IEnumerable<Entry> entries) {
            Entries = entries
                .OrderByDescending(entry => entry.IsDirectory)
                .ThenBy(entry => entry.Path, StringComparer.Ordinal)
                .ToList();
            TotalSize = Entries.Sum(entry => entry.Size ?? 0);
        }

        /// <summary>
        /// Directories first, then files, each group in ordinal path order so the client can
        /// render the list as given and the order is the same on every platform.
        /// </summary>
        public IReadOnlyList<Entry> Entries { get; }

        /// <summary>
        /// Sum of the sizes of the files in <see cref="Entries"/>, in bytes. Directories have no
        /// size and contribute nothing; an empty listing totals zero.
        /// </summary>
        public long TotalSize { get; }
    }

    /// <summary>
    /// One file or directory, addressed by its path relative to the base directory.
    /// <paramref name="Size"/> is the file's length in bytes, or <c>null</c> for a directory.
    /// </summary>
    public record Entry(string Path, bool IsDirectory, long? Size) {
        /// <summary>
        /// Builds the entry for a file or directory on disk. Only the info's name, type and
        /// (for a file) length are read, so nothing about the server-side location of
        /// <paramref name="baseDirectory"/> can reach a response.
        /// </summary>
        /// <param name="baseDirectory">
        /// Absolute path of the browsable root; the entry's path is expressed relative to it.
        /// </param>
        /// <param name="info">
        /// The file or directory, as <see cref="DirectoryInfo.EnumerateFileSystemInfos()"/> yields it.
        /// </param>
        public static Entry From(string baseDirectory, FileSystemInfo info) =>
            new(System.IO.Path.GetRelativePath(baseDirectory, info.FullName),
                IsDirectory: info is DirectoryInfo,
                Size: (info as FileInfo)?.Length);
    }
}

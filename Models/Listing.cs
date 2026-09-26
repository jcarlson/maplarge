namespace TestProject.Models {
    /// <summary>
    /// A set of file-system entries as the API presents it: the shape every listing-style
    /// response (a directory's contents, and later search results) serialises to. Built from the
    /// framework's <see cref="FileSystemInfo"/> objects but keeps none of them, so nothing about
    /// the server-side location of the base directory can reach a response.
    /// </summary>
    public class Listing {
        /// <param name="baseDirectory">
        /// Absolute path of the browsable root; entry paths are expressed relative to it.
        /// </param>
        /// <param name="entries">The entries to present, in any order.</param>
        public Listing(string baseDirectory, IEnumerable<FileSystemInfo> entries) {
            Entries = entries
                .Select(info => new Entry(Path.GetRelativePath(baseDirectory, info.FullName), info is DirectoryInfo))
                .OrderByDescending(entry => entry.IsDirectory)
                .ThenBy(entry => entry.Path, StringComparer.Ordinal)
                .ToList();
        }

        /// <summary>
        /// Directories first, then files, each group in ordinal path order so the client can
        /// render the list as given and the order is the same on every platform.
        /// </summary>
        public IReadOnlyList<Entry> Entries { get; }
    }

    /// <summary>One file or directory, addressed by its path relative to the base directory.</summary>
    public record Entry(string Path, bool IsDirectory);
}

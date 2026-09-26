namespace TestProject.Services {
    /// <summary>
    /// Lists the contents of a fixed base directory. Every entry is reported as a path
    /// relative to that base, so callers never see where on disk the base lives.
    /// </summary>
    public class DirectoryLister {
        private readonly string _baseDirectory;

        public DirectoryLister(string baseDirectory) {
            _baseDirectory = Path.GetFullPath(baseDirectory);
            if (!Directory.Exists(_baseDirectory)) {
                throw new DirectoryNotFoundException($"Base directory does not exist: {_baseDirectory}");
            }
        }

        /// <summary>
        /// Returns the files and directories directly inside the base directory, as paths
        /// relative to it, in ordinal order so results are stable across platforms.
        /// </summary>
        public IReadOnlyList<string> List() {
            return Directory.EnumerateFileSystemEntries(_baseDirectory)
                .Select(entry => Path.GetRelativePath(_baseDirectory, entry))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
    }
}

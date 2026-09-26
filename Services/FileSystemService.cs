namespace TestProject.Services {
    /// <summary>
    /// Operations on the files and directories under a fixed base directory. Every path taken
    /// or returned is relative to that base, so callers never see where on disk the base lives.
    /// </summary>
    public class FileSystemService {
        private readonly PathResolver _paths;

        public FileSystemService(string baseDirectory) {
            _paths = new PathResolver(baseDirectory);
            if (!Directory.Exists(_paths.BaseDirectory)) {
                throw new DirectoryNotFoundException($"Base directory does not exist: {_paths.BaseDirectory}");
            }
        }

        /// <summary>
        /// Returns the files and directories directly inside <paramref name="path"/>, a directory
        /// given relative to the base, as paths relative to the base in ordinal order so results
        /// are stable across platforms. An empty path or "/" lists the base itself.
        /// </summary>
        /// <exception cref="ArgumentException">The path would escape the base directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory does not exist under the base.</exception>
        public IReadOnlyList<string> ListDirectory(string path = "") {
            var directory = _paths.Resolve(path);
            return Directory.EnumerateFileSystemEntries(directory)
                .Select(entry => Path.GetRelativePath(_paths.BaseDirectory, entry))
                .Order(StringComparer.Ordinal)
                .ToList();
        }
    }
}

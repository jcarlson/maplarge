namespace TestProject.Services {
    /// <summary>
    /// Maps client-supplied relative paths to absolute paths inside a fixed base directory.
    /// A leading separator is treated as the base itself, so "/a/b" and "a/b" are the same
    /// location. The path is fully normalised ("." and ".." segments collapsed) and then
    /// rejected if the result lies outside the base.
    /// </summary>
    public class PathResolver {
        public PathResolver(string baseDirectory) {
            BaseDirectory = Normalize(baseDirectory);
        }

        public string BaseDirectory { get; }

        /// <summary>
        /// Returns the absolute path for <paramref name="relativePath"/> within the base directory.
        /// </summary>
        /// <exception cref="ArgumentException">The path would escape the base directory.</exception>
        public string Resolve(string relativePath) {
            // Path.Join, unlike Path.Combine, does not let a rooted second argument replace the base.
            var resolved = Normalize(Path.Join(BaseDirectory, relativePath));
            if (resolved != BaseDirectory && !resolved.StartsWith(BaseDirectory + Path.DirectorySeparatorChar, StringComparison.Ordinal)) {
                throw new ArgumentException($"Path escapes the base directory: {relativePath}", nameof(relativePath));
            }

            return resolved;
        }

        /// <summary>
        /// Absolute form with dot segments and repeated separators collapsed and no trailing
        /// separator, so two spellings of one location compare equal as strings.
        /// </summary>
        private static string Normalize(string path) {
            return Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));
        }
    }
}

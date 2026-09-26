using TestProject.Models;

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
        /// Lists the files and directories directly inside <paramref name="path"/>, a directory
        /// given relative to the base. An empty path or "/" lists the base itself.
        /// </summary>
        /// <exception cref="ArgumentException">The path would escape the base directory.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory does not exist under the base.</exception>
        public Listing ListDirectory(string path = "") {
            var directory = new DirectoryInfo(_paths.Resolve(path));
            return new Listing(_paths.BaseDirectory, directory.EnumerateFileSystemInfos());
        }

        /// <summary>
        /// Describes the file at <paramref name="path"/>, given relative to the base. No handle is
        /// opened; the caller decides how to read the file, e.g. by handing
        /// <see cref="FileInfo.FullName"/> to the framework so it owns the open and close.
        /// </summary>
        /// <exception cref="ArgumentException">The path would escape the base directory.</exception>
        /// <exception cref="FileNotFoundException">
        /// No file exists at the path under the base. A directory at the path counts as no file.
        /// </exception>
        public FileInfo GetFileInfo(string path) {
            var file = new FileInfo(_paths.Resolve(path));
            if (!file.Exists) {
                throw new FileNotFoundException($"File does not exist under the base directory: {path}", path);
            }

            return file;
        }
    }
}

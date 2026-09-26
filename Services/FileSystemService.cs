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
            return new Listing(directory.EnumerateFileSystemInfos().Select(info => Entry.From(_paths.BaseDirectory, info)));
        }

        /// <summary>
        /// Finds every file and directory anywhere under the base whose name contains
        /// <paramref name="query"/>, ignoring case. Only the entry's own name is matched, never
        /// the directories above it, so a query of "photos" finds the "photos" directory but not
        /// the files inside it. Subdirectories that cannot be read are skipped rather than
        /// failing the whole search.
        /// </summary>
        /// <exception cref="ArgumentException">The query is empty or whitespace.</exception>
        public Listing Search(string query) {
            if (string.IsNullOrWhiteSpace(query)) {
                throw new ArgumentException("Search query must not be empty.", nameof(query));
            }

            var options = new EnumerationOptions {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                // Show the same entries a listing would: the defaults skip hidden and system files.
                AttributesToSkip = 0,
            };
            var matches = new DirectoryInfo(_paths.BaseDirectory)
                .EnumerateFileSystemInfos("*", options)
                .Where(info => info.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            return new Listing(matches.Select(info => Entry.From(_paths.BaseDirectory, info)));
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

        /// <summary>
        /// Writes <paramref name="content"/> to a new file named <paramref name="fileName"/> inside
        /// <paramref name="directoryPath"/>, a directory given relative to the base. The file must not
        /// already exist; nothing is ever overwritten.
        /// </summary>
        /// <returns>The new file as the <see cref="Entry"/> a listing of its directory would show.</returns>
        /// <exception cref="ArgumentException">
        /// The directory path would escape the base, or the file name is empty or not a single path segment.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">The directory does not exist under the base.</exception>
        /// <exception cref="EntryExistsException">A file or directory with that name already exists there.</exception>
        public async Task<Entry> WriteFileAsync(string directoryPath, string fileName, Stream content, CancellationToken cancellationToken = default) {
            var directory = new DirectoryInfo(_paths.Resolve(directoryPath));
            if (!directory.Exists) {
                throw new DirectoryNotFoundException($"Directory does not exist under the base directory: {directoryPath}");
            }

            if (fileName.Length == 0 || fileName is "." or ".." || Path.GetFileName(fileName) != fileName) {
                throw new ArgumentException($"File name must be a single path segment: {fileName}", nameof(fileName));
            }

            var target = Path.Join(directory.FullName, fileName);
            if (Path.Exists(target)) {
                throw new EntryExistsException($"An entry already exists at: {Path.Join(directoryPath, fileName)}");
            }

            // CreateNew also refuses an existing file, so two racing writes cannot clobber each other.
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Options = FileOptions.Asynchronous };
            await using (var file = new FileStream(target, options)) {
                await content.CopyToAsync(file, cancellationToken);
            }

            return Entry.From(_paths.BaseDirectory, new FileInfo(target));
        }
    }
}

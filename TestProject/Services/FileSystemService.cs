using TestProject.Models;

namespace TestProject.Services
{
    /// <summary>
    /// The <see cref="IFileSystemService"/> over a real directory on disk. Every path taken or
    /// returned is relative to the base given to the constructor.
    /// </summary>
    public class FileSystemService : IFileSystemService
    {
        private readonly PathResolver _paths;

        public FileSystemService(string baseDirectory)
        {
            _paths = new PathResolver(baseDirectory);
            if (!Directory.Exists(_paths.BaseDirectory))
            {
                throw new DirectoryNotFoundException($"Base directory does not exist: {_paths.BaseDirectory}");
            }
        }

        /// <inheritdoc/>
        public IReadOnlyList<FileSystemEntry> ListDirectory(string path = "")
        {
            var directory = ResolveDirectory(path);
            return ToOrderedEntries(directory.EnumerateFileSystemInfos());
        }

        /// <inheritdoc/>
        public IReadOnlyList<FileSystemEntry> Search(string path, string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                throw new ArgumentException("Search query must not be empty.", nameof(query));
            }
            var directory = ResolveDirectory(path);

            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                // A subdirectory that cannot be read is skipped rather than failing the whole search.
                IgnoreInaccessible = true,
                // Show the same entries a listing would: the defaults skip hidden and system files.
                AttributesToSkip = 0,
            };
            var matches = directory
                .EnumerateFileSystemInfos("*", options)
                .Where(info => info.Name.Contains(query, StringComparison.OrdinalIgnoreCase));
            return ToOrderedEntries(matches);
        }

        /// <summary>
        /// The existing directory at <paramref name="path"/>, given relative to the base. Checked
        /// explicitly rather than left to enumeration, whose exception for a path that is a file
        /// differs by platform (a <see cref="DirectoryNotFoundException"/> on Unix, a plain
        /// <see cref="IOException"/> on Windows).
        /// </summary>
        /// <exception cref="ArgumentException">The path would escape the base directory.</exception>
        /// <exception cref="DirectoryNotFoundException">There is no directory at the path under the base.</exception>
        private DirectoryInfo ResolveDirectory(string path)
        {
            var directory = new DirectoryInfo(_paths.Resolve(path));
            if (!directory.Exists)
            {
                throw new DirectoryNotFoundException($"Directory does not exist under the base directory: {path}");
            }

            return directory;
        }

        /// <summary>
        /// The entries for a set of infos: directories first, then files, each group in ordinal
        /// path order, so the client can render the list as given and the order is the same on
        /// every platform.
        /// </summary>
        private IReadOnlyList<FileSystemEntry> ToOrderedEntries(IEnumerable<FileSystemInfo> infos) =>
            infos.Select(info => FileSystemEntry.From(_paths.BaseDirectory, info))
                .OrderByDescending(entry => entry.IsDirectory)
                .ThenBy(entry => entry.Path, StringComparer.Ordinal)
                .ToList();

        /// <inheritdoc/>
        public FileInfo GetFileInfo(string path)
        {
            var file = new FileInfo(_paths.Resolve(path));
            if (!file.Exists)
            {
                throw new FileNotFoundException($"File does not exist under the base directory: {path}", path);
            }

            return file;
        }

        /// <inheritdoc/>
        public async Task<FileSystemEntry> WriteFileAsync(string directoryPath, string fileName, Stream content, CancellationToken cancellationToken = default)
        {
            var directory = ResolveDirectory(directoryPath);

            if (fileName.Length == 0 || fileName is "." or ".." || Path.GetFileName(fileName) != fileName)
            {
                throw new ArgumentException($"File name must be a single path segment: {fileName}", nameof(fileName));
            }

            var target = Path.Join(directory.FullName, fileName);
            if (Path.Exists(target))
            {
                throw new EntryExistsException($"An entry already exists at: {Path.Join(directoryPath, fileName)}");
            }

            // CreateNew also refuses an existing file, so two racing writes cannot clobber each other.
            var options = new FileStreamOptions { Mode = FileMode.CreateNew, Access = FileAccess.Write, Options = FileOptions.Asynchronous };
            var file = new FileStream(target, options);
            try
            {
                await using (file)
                {
                    await content.CopyToAsync(file, cancellationToken);
                }
            }
            catch
            {
                // The copy stopped part way (the client went away, or the request was cancelled):
                // a half-written file must not be left looking like a real one. The file is ours to
                // delete because CreateNew succeeded, so nothing else could have created it.
                File.Delete(target);
                throw;
            }

            return FileSystemEntry.From(_paths.BaseDirectory, new FileInfo(target));
        }
    }
}

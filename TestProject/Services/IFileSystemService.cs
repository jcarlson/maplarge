using TestProject.Models;

namespace TestProject.Services {
    /// <summary>
    /// Operations on the files and directories under a fixed base directory. Every path taken
    /// or returned is relative to that base, so callers never see where on disk the base lives.
    /// The controllers depend on this rather than on <see cref="FileSystemService"/>, so their
    /// mapping of each documented outcome to an HTTP status can be tested against a stub that
    /// produces the outcome on demand, with no directory on disk.
    /// </summary>
    public interface IFileSystemService {
        /// <summary>
        /// Lists the files and directories directly inside <paramref name="path"/>, a directory
        /// given relative to the base. An empty path or "/" lists the base itself. Entries come
        /// directories first, then files, each group in ordinal path order.
        /// </summary>
        /// <exception cref="ArgumentException">The path would escape the base directory.</exception>
        /// <exception cref="DirectoryNotFoundException">
        /// The directory does not exist under the base. A file at the path counts as no directory.
        /// </exception>
        IReadOnlyList<FileSystemEntry> ListDirectory(string path = "");

        /// <summary>
        /// Finds every file and directory anywhere under <paramref name="path"/>, a directory given
        /// relative to the base (empty or "/" for the base itself), whose name contains
        /// <paramref name="query"/>, ignoring case. Only the entry's own name is matched, never
        /// the directories above it, so a query of "photos" finds the "photos" directory but not
        /// the files inside it. Paths in the result are relative to the base, not to
        /// <paramref name="path"/>, and are ordered as a listing is.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// The path would escape the base directory, or the query is empty or whitespace.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// The directory does not exist under the base. A file at the path counts as no directory.
        /// </exception>
        IReadOnlyList<FileSystemEntry> Search(string path, string query);

        /// <summary>
        /// Describes the file at <paramref name="path"/>, given relative to the base. No handle is
        /// opened; the caller decides how to read the file, e.g. by handing
        /// <see cref="FileInfo.FullName"/> to the framework so it owns the open and close.
        /// </summary>
        /// <exception cref="ArgumentException">The path would escape the base directory.</exception>
        /// <exception cref="FileNotFoundException">
        /// No file exists at the path under the base. A directory at the path counts as no file.
        /// </exception>
        FileInfo GetFileInfo(string path);

        /// <summary>
        /// Writes <paramref name="content"/> to a new file named <paramref name="fileName"/> inside
        /// <paramref name="directoryPath"/>, a directory given relative to the base. The file must not
        /// already exist; nothing is ever overwritten. If the copy fails or is cancelled part way, the
        /// partial file is deleted before the exception propagates, so a failed upload leaves no trace.
        /// </summary>
        /// <returns>The new file as the <see cref="FileSystemEntry"/> a listing of its directory would show.</returns>
        /// <exception cref="ArgumentException">
        /// The directory path would escape the base, or the file name is empty or not a single path segment.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">The directory does not exist under the base.</exception>
        /// <exception cref="EntryExistsException">A file or directory with that name already exists there.</exception>
        Task<FileSystemEntry> WriteFileAsync(string directoryPath, string fileName, Stream content, CancellationToken cancellationToken = default);
    }
}

namespace TestProject.Models
{
    /// <summary>
    /// One file or directory, addressed by its path relative to the base directory: the shape
    /// every API response is made of. <paramref name="Path"/> uses <c>/</c> as its separator on
    /// every platform, so the client can split it without knowing the server's OS.
    /// <paramref name="Size"/> is the file's length in bytes, or <c>null</c> for a directory.
    /// </summary>
    public record FileSystemEntry(string Path, bool IsDirectory, long? Size)
    {
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
        public static FileSystemEntry From(string baseDirectory, FileSystemInfo info) =>
            new(System.IO.Path.GetRelativePath(baseDirectory, info.FullName).Replace(System.IO.Path.DirectorySeparatorChar, '/'),
                IsDirectory: info is DirectoryInfo,
                Size: (info as FileInfo)?.Length);
    }
}

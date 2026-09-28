using TestProject.Models;
using TestProject.Services;

namespace TestProject.Tests {
    /// <summary>
    /// An <see cref="IFileSystemService"/> a test scripts by assigning a delegate per method, so a
    /// controller can be handed each outcome the real service documents (a result, or one of its
    /// exceptions) without a directory on disk. A method the test did not script fails the test.
    /// </summary>
    internal sealed class StubFileSystemService : IFileSystemService {
        public Func<string, IReadOnlyList<FileSystemEntry>> OnListDirectory { get; set; } = _ => throw Unscripted();
        public Func<string, string, IReadOnlyList<FileSystemEntry>> OnSearch { get; set; } = (_, _) => throw Unscripted();
        public Func<string, FileInfo> OnGetFileInfo { get; set; } = _ => throw Unscripted();
        public Func<string, string, Stream, FileSystemEntry> OnWriteFile { get; set; } = (_, _, _) => throw Unscripted();

        public IReadOnlyList<FileSystemEntry> ListDirectory(string path = "") => OnListDirectory(path);

        public IReadOnlyList<FileSystemEntry> Search(string path, string query) => OnSearch(path, query);

        public FileInfo GetFileInfo(string path) => OnGetFileInfo(path);

        public Task<FileSystemEntry> WriteFileAsync(string directoryPath, string fileName, Stream content, CancellationToken cancellationToken = default) =>
            Task.FromResult(OnWriteFile(directoryPath, fileName, content));

        private static InvalidOperationException Unscripted() => new("The test did not script this call.");
    }
}

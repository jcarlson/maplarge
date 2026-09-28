namespace TestProject.Services
{
    /// <summary>
    /// Thrown when an operation would create an entry at a path where a file or directory already
    /// exists. Distinct from a plain <see cref="IOException"/> so callers can tell a name clash (the
    /// client's problem) from a genuine I/O failure (the server's).
    /// </summary>
    public class EntryExistsException : IOException
    {
        public EntryExistsException(string message) : base(message)
        {
        }
    }
}

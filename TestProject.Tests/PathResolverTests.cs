using TestProject.Services;

namespace TestProject.Tests {
    /// <summary>
    /// <see cref="PathResolver"/> is pure string logic, so these tests use a base directory
    /// that need not exist on disk.
    /// </summary>
    public class PathResolverTests {
        private static readonly string Base = Path.Combine(Path.GetTempPath(), "PathResolverTests", "root");
        private readonly PathResolver _resolver = new(Base);

        [Theory]
        [InlineData("")]
        [InlineData("/")]
        [InlineData("//")]
        [InlineData(".")]
        [InlineData("foo/..")]
        public void Resolve_PathsMeaningTheBase_ReturnBase(string path) {
            Assert.Equal(Base, _resolver.Resolve(path));
        }

        [Theory]
        [InlineData("foo/bar/baz")]
        [InlineData("/foo/bar/baz")]
        [InlineData("foo/bar/baz/")]
        [InlineData("foo//bar///baz")]
        [InlineData("./foo/bar/baz")]
        [InlineData("foo/./bar/baz")]
        [InlineData("foo/qux/../bar/baz")]
        public void Resolve_PathsInsideBase_AreNormalizedAndRootedAtBase(string path) {
            Assert.Equal(Path.Combine(Base, "foo", "bar", "baz"), _resolver.Resolve(path));
        }

        [Theory]
        [InlineData("..")]
        [InlineData("../")]
        [InlineData("../etc")]
        [InlineData("/../etc")]
        [InlineData("foo/../../etc")]
        [InlineData("../rootling")]
        public void Resolve_PathsEscapingBase_Throw(string path) {
            Assert.Throws<ArgumentException>(() => _resolver.Resolve(path));
        }

        [Fact]
        public void Resolve_PathThatLeavesAndReentersBase_IsAccepted() {
            Assert.Equal(Path.Combine(Base, "foo"), _resolver.Resolve("../root/foo"));
        }

        [Fact]
        public void Resolve_SegmentsThatMerelyStartWithDots_AreAllowed() {
            Assert.Equal(Path.Combine(Base, ".hidden", "...three"), _resolver.Resolve(".hidden/...three"));
        }

        [Fact]
        public void BaseDirectory_IsNormalizedToFullPath() {
            var unnormalized = Base + Path.DirectorySeparatorChar + "." + Path.DirectorySeparatorChar;

            Assert.Equal(Base, new PathResolver(unnormalized).BaseDirectory);
        }
    }
}

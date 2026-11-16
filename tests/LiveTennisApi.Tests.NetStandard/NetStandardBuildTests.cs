using System.Linq;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text.Json;
using Xunit;

namespace LiveTennisApi.Tests
{
    /// <summary>
    /// Guards the point of this project. If either check fails, the shared tests
    /// here ran against the net8.0 build or the in-box serializer instead.
    /// </summary>
    public class NetStandardBuildTests
    {
        [Fact]
        public void LibraryUnderTestIsTheNetStandardBuild()
        {
            var framework = typeof(LiveTennisApiClient).Assembly
                .GetCustomAttributes<TargetFrameworkAttribute>()
                .Single()
                .FrameworkName;

            Assert.StartsWith(".NETStandard,Version=v2.0", framework);
        }

        [Fact]
        public void SerializerIsThePackageNotTheInBoxCopy()
        {
            // net8.0 ships System.Text.Json 8.0. The package the library
            // references for netstandard2.0 is newer, so it must win here.
            var version = typeof(JsonSerializer).Assembly.GetName().Version!;

            Assert.True(version.Major > 8, "Loaded System.Text.Json " + version + ", expected the package.");
        }
    }
}

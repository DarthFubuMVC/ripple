using NUnit.Framework;
using NuGet;
using ripple.Model;
using ripple.Nuget;
using FubuTestingSupport;

namespace ripple.Testing.Nuget
{
    [TestFixture, Explicit]
    public class NugetFeedTester
    {
        private NugetFeed theFeed;

        [TestFixtureSetUp]
        public void SetUp()
        {
            theFeed = new NugetFeed(Feed.NuGetV2.Url, NugetStability.ReleasedOnly);
        }

        [Test]
        public void finds_the_latest_for_a_special_version()
        {
            var stableVersionInThePast = new SemanticVersion("4.9.1");
            theFeed.FindLatest(new Dependency("Newtonsoft.Json") {NugetStability = NugetStability.ReleasedOnly}).Version.ShouldBeGreaterThan(stableVersionInThePast);
        }

        [Test]
        public void find_the_async_package()
        {
            var asyncTargetPack = new Dependency("Microsoft.CompilerServices.AsyncTargetingPack");
            var solution = new Solution();
            solution.AddDependency(asyncTargetPack);

            var task = NugetSearch.Find(solution, asyncTargetPack);
            task.Wait();

            task.Result.Nuget.Version.ShouldEqual(new SemanticVersion("1.0.1"));
        }

        [Test]
        public void restore_solution_dependencies()
        {
            var notExistingVersion = new SemanticVersion("1.0.0");
            var fixedFeed = new NugetFeed(Feed.NuGetV2.Url, NugetStability.ReleasedOnly);
            fixedFeed.Find(new Dependency("Newtonsoft.Json", notExistingVersion, UpdateMode.Float)
            {
                NugetStability = NugetStability.ReleasedOnly,
                Mode = UpdateMode.Float
            }).ShouldNotBeNull();
        }
    }
}
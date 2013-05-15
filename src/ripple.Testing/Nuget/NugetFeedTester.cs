using NUnit.Framework;
using NuGet;
using ripple.Model;
using ripple.Nuget;
using FubuTestingSupport;

namespace ripple.Testing.Nuget
{
    [TestFixture]
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
    }
}
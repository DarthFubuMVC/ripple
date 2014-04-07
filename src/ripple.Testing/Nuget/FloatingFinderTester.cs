using System.Linq;
using FubuTestingSupport;
using NUnit.Framework;
using NuGet;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Testing.Nuget
{
    [TestFixture]
    public class FloatingFinderTester
    {
        private FloatingFinder theFinder;
        private Solution theSolution;

        [SetUp]
        public void SetUp()
        {
            theFinder = new FloatingFinder();

            FeedScenario.Create(scenario =>
            {
                scenario.For(Feed.Fubu)
                        .Add("Test", "1.1.0.0")
                        .Add("Test", "1.2.0.0")
                        .Add("Test", "1.2.0.12");
            });

            theSolution = Solution.Empty();
            theSolution.AddFeed(Feed.Fubu);
        }

        [Test]
        public void matches_floated_dependencies()
        {
            theFinder.Matches(new Dependency("Test")).ShouldBeTrue();
        }

        [Test]
        public void does_not_match_fixed_dependencies()
        {
            theFinder.Matches(new Dependency("Test", "1.1.0.0", UpdateMode.Fixed)).ShouldBeFalse();
        }

        [Test]
        public void finds_the_latest_version()
        {
            var result = theFinder.Find(theSolution, new Dependency("Test", "1.1.0.0"));

            result.Found.ShouldBeTrue();
            result.Nuget.Version.ShouldEqual(new SemanticVersion("1.2.0.12"));
        }

        [Test]
        public void find_the_latest_for_a_fixed_dependency_should_respect_the_upper_bounds()
        {
            var result = theFinder.Find(theSolution, new Dependency("Test", "1.0.0.0", UpdateMode.Fixed));;

            result.Found.ShouldBeFalse();
        }

        [Test]
        [Explicit]
        public void load_all_packages_from_feed_with_100plus_packages()
        {
            var feedUrl = Feed.NuGetV2.Url;

            var feed = new FloatingFeed(feedUrl, NugetStability.Anything);

            Assert.IsTrue(feed.GetLatest().Count() > 100, feedUrl + " has more than 100 packages");
        }


        [Test]
        [Explicit]
        public void find_packages_by_name_from_feed_with_100plus_packages()
        {
            var feedUrl = Feed.NuGetV2.Url;

            var feed = new FloatingFeed(feedUrl, NugetStability.Anything);

            Assert.NotNull(feed.FindLatestByName("MiniProfiler"), "Package should be on nuget feed " + feedUrl);
        }
    }
}
using System;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Testing.Model
{
    [TestFixture]
    public class FeedProviderTester
    {
        private FeedProvider theProvider;
        private Func<string> originalBranchHelper;

        [SetUp]
        public void SetUp()
        {
            theProvider = new FeedProvider();
            originalBranchHelper = BranchDetector.ProvideBranchName;
            BranchDetector.ProvideBranchName = () => "testBranch";
        }

        [TearDown]
        public void TearDown()
        {
            BranchDetector.ProvideBranchName = originalBranchHelper;
        }

        [Test]
        public void fixed_filesystem_feed()
        {
            theProvider.For(new Feed("file://C:/code/nugets", UpdateMode.Fixed))
                       .As<FileSystemNugetFeed>()
                       .Directory
                       .ShouldEqual("C:/code/nugets");
        }

        [Test]
        public void floated_filesystem_feed()
        {
            theProvider.For(new Feed("file://C:/code/nugets", UpdateMode.Float))
                       .As<FloatingFileSystemNugetFeed>()
                       .Directory
                       .ShouldEqual("C:/code/nugets");
        }

        [Test]
        public void filesystem_feed_with_branch()
        {
            theProvider.For(new Feed("file://C:/code/nugets/{branch}", UpdateMode.Float))
                       .As<FloatingFileSystemNugetFeed>()
                       .Directory
                       .ShouldEqual("C:/code/nugets/testBranch");
        }

        [Test]
        public void fixed_feed()
        {
            theProvider.For(Feed.NuGetV2)
                       .As<NugetFeed>()
                       .Url
                       .ShouldEqual(Feed.NuGetV2.Url);
        }

        [Test]
        public void floated_feed()
        {
            theProvider.For(Feed.Fubu)
                       .As<FloatingFeed>()
                       .Url
                       .ShouldEqual(Feed.Fubu.Url);
        }
    }
}
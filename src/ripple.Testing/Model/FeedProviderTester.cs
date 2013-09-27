using System;
using System.IO;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Testing.Model
{
    [TestFixture]
    public class FeedProviderTester
    {
        private FeedProvider theProvider;

        [SetUp]
        public void SetUp()
        {
            theProvider = new FeedProvider();
            BranchDetector.Stub(() => "testBranch");
        }

        [TearDown]
        public void TearDown()
        {
            BranchDetector.Live();
        }

        [Test]
        public void fixed_filesystem_feed()
        {
			theProvider.For(new Feed(string.Format("file://{0}/nugets", Path.GetTempPath()), UpdateMode.Fixed))
                       .As<FileSystemNugetFeed>()
                       .Directory
                       .ShouldEqual(Path.Combine(Path.GetTempPath(), "nugets"));
        }

        [Test]
        public void floated_filesystem_feed()
        {
			theProvider.For(new Feed(string.Format("file://{0}/code/nugets", Path.GetTempPath()), UpdateMode.Float))
                       .As<FloatingFileSystemNugetFeed>()
                       .Directory
                       .ShouldEqual(Path.Combine(Path.GetTempPath(), "code", "nugets"));
        }

        [Test]
        public void filesystem_feed_with_branch()
        {
			var tmpPath = string.Format(@"file://{0}/{{branch}}", Path.GetTempPath());

			theProvider.For(new Feed(tmpPath, UpdateMode.Float))
                       .As<FloatingFileSystemNugetFeed>()
                       .Directory
					.ShouldEqual(Path.Combine(Path.GetTempPath(), "testBranch"));
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

        [Test]
        public void single_dot_int_file_system_feed_paths_should_be_expanded_relative_to_cwd()
        {
            asCurrentDirectory(cwd => theProvider.For(new Feed("file://./code/nugets", UpdateMode.Fixed))
                       .As<FileSystemNugetFeed>()
                       .Directory
                       .ShouldEqual(Path.Combine(cwd, "code", "nugets")));
        }
        
        [Test]
        public void double_dot_int_file_system_feed_paths_should_be_expanded_relative_to_cwd()
        {
        	asCurrentDirectory(cwd =>
        	{
        		var parentDir = Directory.GetParent(cwd).ToString();
        
        		theProvider.For(new Feed("file://../code/nugets", UpdateMode.Float))
        				   .As<FloatingFileSystemNugetFeed>()
        				   .Directory
        				   .ShouldEqual(Path.Combine(parentDir, "code", "nugets"));
        	});
        }
        
        private static void asCurrentDirectory(Action<string> cwdAction)
        {
        	var cwd = Directory.GetCurrentDirectory();
        	try
        	{
        		var newCwd = Path.GetTempPath().AppendRandomPath();
        		Directory.CreateDirectory(newCwd);
        		Directory.SetCurrentDirectory(newCwd);
        
        		cwdAction(newCwd);
        	}
        	finally
        	{
        		Directory.SetCurrentDirectory(cwd);
        	}
        }

    }
}
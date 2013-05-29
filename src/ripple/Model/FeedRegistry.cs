using System.IO;
using FubuCore;
using FubuCore.Util;
using ripple.Nuget;

namespace ripple.Model
{
	public interface IFeedProvider
	{
		INugetFeed For(Feed feed);
	}

	public class FeedProvider : IFeedProvider
	{
		private readonly Cache<Feed, INugetFeed> _feeds;
        
		public FeedProvider()
		{
			_feeds = new Cache<Feed, INugetFeed>(buildFeed);
		}

		public INugetFeed For(Feed feed)
		{
			return _feeds[feed];
		}

		private INugetFeed buildFeed(Feed feed)
		{
            if(feed.Url.StartsWith("file://"))
            {
                return buildFileSystemFeed(feed);
            }

			if (feed.Mode == UpdateMode.Fixed)
			{
                return new NugetFeed(feed.Url, feed.Stability);
			}

            return new FloatingFeed(feed.Url, feed.Stability);
		}

        private const string BranchPlaceholder = "{branch}";

        private INugetFeed buildFileSystemFeed(Feed feed)
        {
            var directory = feed.Url.Replace("file://", "");

            if (directory.Contains(BranchPlaceholder))
            {
                var branchName = BranchDetector.Current();
                directory = directory.Replace(BranchPlaceholder, branchName);

                RippleLog.Debug("Detected branch feed: {0}. Current branch is {1}. Setting directory to {2}".ToFormat(feed, branchName, directory));
            }

            directory = directory.ToFullPath();

            if (feed.Mode == UpdateMode.Fixed)
            {
                return new FileSystemNugetFeed(directory, feed.Stability);
            }

            return new FloatingFileSystemNugetFeed(directory, feed.Stability);
        }
	}

    public class FeedRegistry
	{
		private static IFeedProvider _provider;

		static FeedRegistry()
		{
			Reset();
		}

		public static void Stub(IFeedProvider provider)
		{
			_provider = provider;
		}

		public static void Reset()
		{
			Stub(new FeedProvider());
		}

	 	public static INugetFeed FeedFor(Feed feed)
	 	{
	 		return _provider.For(feed);
	 	}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using ripple.Nuget;

namespace ripple.Model
{
    public interface IFeedConnectivity
    {
        // TODO -- Kill this
        void IfOnline(INugetFeed feed, Action<INugetFeed> continuation);
        bool AllOffline(IEnumerable<INugetFeed> feeds);

        IEnumerable<INugetFeed> FloatedFeedsFor(Solution solution);
        IEnumerable<INugetFeed> FeedsFor(Solution solution);
    }

    // Covered with integration tests
    public class FeedConnectivity : IFeedConnectivity
    {
        private readonly IList<INugetFeed> _checked = new List<INugetFeed>(); 
        private readonly IList<INugetFeed> _offline = new List<INugetFeed>();

        public void IfOnline(INugetFeed feed, Action<INugetFeed> continuation)
        {
            try
            {
                if (isOffline(feed))
                {
                    RippleLog.Debug("Feed offline. Ignoring " + feed);
                    return;
                }

                continuation(feed);
            }
            catch (Exception exc)
            {
                MarkOffline(feed);
                RippleLog.Info("Feed unavailable: " + feed);
                RippleLog.Debug(exc.ToString());
            }
        }

        public IEnumerable<INugetFeed> FloatedFeedsFor(Solution solution)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<INugetFeed> FeedsFor(Solution solution)
        {
            yield return solution.Cache.ToFeed().GetNugetFeed();

            var solutionFeeds = solution.Feeds.Select(x => x.GetNugetFeed()).ToArray();

            if (!AllOffline(solutionFeeds))
            {
                foreach (var feed in solutionFeeds)
                {
                    yield return feed;
                }
            }
        }

        public bool AllOffline(IEnumerable<INugetFeed> feeds)
        {
            return feeds.All(isOffline);
        }

        public void MarkOffline(INugetFeed feed)
        {
            _offline.Fill(feed);
        }

        private bool isOffline(INugetFeed feed)
        {
            if (!_checked.Contains(feed))
            {
                if (!feed.IsOnline())
                {
                    _offline.Add(feed);
                }

                _checked.Add(feed);
            }

            return _offline.Contains(feed);
        }
    }
}
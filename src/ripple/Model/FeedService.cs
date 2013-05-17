using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;
using ripple.Nuget;

namespace ripple.Model
{
    public class FeedService : IFeedService
    {
        private readonly IList<INugetFeed> _offline = new List<INugetFeed>();
        private readonly Dictionary<Dependency, IRemoteNuget> _nugetForCache = new Dictionary<Dependency, IRemoteNuget>();
        private readonly Dictionary<Dependency, IEnumerable<Dependency>> _dependenciesForFixedCache = new Dictionary<Dependency, IEnumerable<Dependency>>();
        private readonly Dictionary<Dependency, IEnumerable<Dependency>> _dependenciesForFloatCache = new Dictionary<Dependency, IEnumerable<Dependency>>();

        private void markOffline(INugetFeed feed)
        {
            _offline.Fill(feed);
        }

        private bool isOffline(INugetFeed feed)
        {
            return _offline.Contains(feed);
        }

        private bool allOffline(IEnumerable<INugetFeed> feeds)
        {
            return feeds.All(isOffline);
        }

        private void tryFeed(INugetFeed feed, Action<INugetFeed> action)
        {
            try
            {
                if (isOffline(feed))
                {
                    RippleLog.Debug("Feed offline. Ignoring.");
                    return;
                }

                action(feed);
            }
            catch (Exception exc)
            {
                markOffline(feed);
                RippleLog.Debug("Feed unavailable: " + feed);
                RippleLog.Debug(exc.ToString());
            }
        }

        public virtual IRemoteNuget NugetFor(Solution solution, Dependency dependency)
        {
            IRemoteNuget feed;
            if (_nugetForCache.TryGetValue(dependency, out feed) == false)
            {
                feed = nugetFor(solution, dependency);
                _nugetForCache[dependency] = feed;
            }

            return feed;
        }

        private IRemoteNuget nugetFor(Solution solution, Dependency dependency, bool retrying = false)
        {
            IRemoteNuget nuget = null;
            var feeds = feedsFor(solution);
            foreach (var feed in feeds)
            {
                tryFeed(feed, x => nuget = getLatestFromFloatingFeed(x, dependency));
                if (nuget != null) break;

                if (dependency.IsFloat() || dependency.Version.IsEmpty())
                {
                    tryFeed(feed, x => nuget = x.FindLatest(dependency));
                    if (nuget != null) break;
                }

                tryFeed(feed, x => nuget = x.Find(dependency));
                if (nuget != null) break;
            }

            if (nuget == null)
            {
                if (allOffline(feeds) && !retrying)
                {
                    return nugetFor(solution, dependency, true);
                }

                feeds.OfType<FloatingFileSystemNugetFeed>()
                    .Each(files => files.DumpLatest());

                RippleAssert.Fail("Could not find " + dependency);
            }

            return remoteOrCached(solution, nuget);
        }

        private IRemoteNuget remoteOrCached(Solution solution, IRemoteNuget nuget)
        {
            if (nuget == null) return null;
            return solution.Cache.Retrieve(nuget);
        }

        private IRemoteNuget getLatestFromFloatingFeed(INugetFeed feed, Dependency dependency)
        {
            var floatingFeed = feed as IFloatingFeed;
            if (floatingFeed == null) return null;

            var floatedResult = floatingFeed.GetLatest().SingleOrDefault(x => dependency.MatchesName(x.Name));
            if (floatedResult != null && dependency.Mode == UpdateMode.Fixed && floatedResult.IsUpdateFor(dependency))
            {
                return null;
            }

            return floatedResult;
        }

        // Almost entirely covered by integration tests
        public IEnumerable<Dependency> DependenciesFor(Solution solution, Dependency dependency, UpdateMode mode)
        {
            var cache = mode == UpdateMode.Fixed ? _dependenciesForFixedCache : _dependenciesForFloatCache;

            IEnumerable<Dependency> dependencies;
            if (cache.TryGetValue(dependency, out dependencies) == false)
            {
                dependencies = findDependenciesFor(solution, dependency, mode);
                cache[dependency] = dependencies;
            }

            // ReSharper disable PossibleMultipleEnumeration
            return dependencies.ToArray(); // clone on return
            // ReSharper restore PossibleMultipleEnumeration
        }

        private IEnumerable<Dependency> findDependenciesFor(Solution solution, Dependency dependency, UpdateMode mode, int depth = 0)
        {
            var nuget = NugetFor(solution, dependency);
            var dependencies = new List<Dependency>();

            if (depth != 0)
            {
                var dep = dependency;
                if (dep.IsFloat() && mode == UpdateMode.Fixed)
                {
                    dep = new Dependency(nuget.Name, nuget.Version, mode);
                }

                dependencies.Add(dep);
            }

            nuget
                .Dependencies()
                .Each(x => dependencies.AddRange(findDependenciesFor(solution, x, mode, depth + 1)));

            return dependencies.OrderBy(x => x.Name);
        }

        public IRemoteNuget LatestFor(Solution solution, Dependency dependency, bool forced = false)
        {
            if (dependency.Mode == UpdateMode.Fixed && !forced)
            {
                return null;
            }

            IRemoteNuget latest = null;
            var feeds = feedsFor(solution);

            foreach (var feed in feeds)
            {
                try
                {
                    IRemoteNuget nuget = null;
                    tryFeed(feed, x => nuget = feed.FindLatest(dependency));

                    if (latest == null)
                    {
                        latest = nuget;
                    }

                    if (latest != null && nuget != null && latest.Version < nuget.Version)
                    {
                        latest = nuget;
                    }
                }
                catch (Exception)
                {
                    RippleLog.Debug("Error while finding latest " + dependency);
                }
            }

            if (isUpdate(latest, solution, dependency))
            {
                return remoteOrCached(solution, latest);
            }

            return null;
        }

        private bool isUpdate(IRemoteNuget latest, Solution solution, Dependency dependency)
        {
            if (latest == null) return false;

            var localDependencies = solution.LocalDependencies();
            if (localDependencies.Has(dependency))
            {
                var local = localDependencies.Get(dependency);
                return latest.IsUpdateFor(local);
            }

            if (dependency.IsFloat())
            {
                return true;
            }

            return latest.IsUpdateFor(dependency);
        }

        private IEnumerable<INugetFeed> feedsFor(Solution solution)
        {
            var feeds = solution.Feeds.Select(x => x.GetNugetFeed());
            if (!RippleConnection.Connected() || allOffline(feeds))
            {
                var cache = solution.Cache.ToFeed();
                return new[] { cache.GetNugetFeed() };
            }


            return feeds;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore;
using FubuCore.Util;
using NuGet;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Testing.Model
{
    public class StubFeedProvider : IFeedProvider
    {
        private readonly Cache<Feed, StubFeed> _feeds;

        public StubFeedProvider()
        {
            _feeds = new Cache<Feed, StubFeed>(feed => new StubFeed(feed));
        }

        public INugetFeed For(Feed feed)
        {
            return _feeds[feed];
        }
    }

    public class StubFeed : NugetFeedBase
    {
        private readonly IList<IRemoteNuget> _nugets = new List<IRemoteNuget>();
        private readonly IList<DependencyError> _explicitErrors = new List<DependencyError>();
        private readonly Feed _feed;
        private IPackageRepository _repository;

        public StubFeed(Feed feed)
        {
            _feed = feed;

            UseRepository(new StubPackageRepository());
        }

        public StubFeed Add(string name, string version)
        {
            return Add(new Dependency(name, version));
        }

        public StubFeed Add(Dependency dependency)
        {
            _nugets.Add(new StubNuget(dependency, () => Repository.As<StubPackageRepository>().GetPackageByDependency(dependency)));
            return this;
        }

        protected override IRemoteNuget FindImpl(Dependency query)
        {
            throwIfNeeded(query);

            if (query.IsFloat() || query.Version.IsEmpty())
            {
                return _nugets.FirstOrDefault(x => query.MatchesName(x.Name));
            }

            var version = SemanticVersion.Parse(query.Version);
            var matching = _nugets.Where(x => query.MatchesName(x.Name));

            if (query.DetermineStability(_feed.Stability) == NugetStability.ReleasedOnly)
            {
                return matching.FirstOrDefault(x => x.Version.SpecialVersion.IsEmpty() && x.Version.Equals(version));
            }

            return matching.FirstOrDefault(x => x.Version.Version.Equals(version.Version));
        }

        protected override IRemoteNuget FindLatestImpl(Dependency query)
        {
            Console.WriteLine("FindLatest in {0} for {1}", _repository.GetHashCode(), query);
            throwIfNeeded(query);

            return _nugets.Where(x => x.Name == query.Name)
                          .OrderByDescending(x => x.Version)
                          .FirstOrDefault();
        }

        private void throwIfNeeded(Dependency dependency)
        {
            var error = _explicitErrors.FirstOrDefault(x => x.Matches(dependency));
            if (error != null)
            {
                throw error.Exception;
            }
        }

        public StubFeed ThrowWhenSearchingFor(string name, string version, Exception exception)
        {
            return ThrowWhenSearchingFor(new Dependency(name, version), exception);
        }

        public StubFeed ThrowWhenSearchingFor(Dependency dependency, Exception exception)
        {
            _explicitErrors.Add(new DependencyError(dependency, exception));
            return this;
        }

        public void ConfigureRepository(Action<StubPackageRepository> configure)
        {
            configure((StubPackageRepository)Repository);
        }

        public void UseRepository(StubPackageRepository repository)
        {
            _repository = repository;
        }

        public override IPackageRepository Repository
        {
            get { return _repository; }
        }

        public class DependencyError
        {
            public DependencyError(Dependency dependency, Exception exception)
            {
                Dependency = dependency;
                Exception = exception;
            }

            public Dependency Dependency { get; private set; }
            public Exception Exception { get; private set; }

            public bool Matches(Dependency dependency)
            {
                return Dependency.Equals(dependency);
            }
        }
    }
}
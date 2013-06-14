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
            _feeds = new Cache<Feed, StubFeed>(feed =>
	        {
		        if (feed.Mode == UpdateMode.Fixed)
		        {
			        return new StubFeed(feed);
		        }

				return new FloatingStubFeed(feed);
	        });
        }

        public INugetFeed For(Feed feed)
        {
            return _feeds[feed];
        }
    }

    public class StubFeed : NugetFeedBase
    {
        protected readonly IList<IRemoteNuget> Nugets = new List<IRemoteNuget>();
        private readonly IList<DependencyError> _explicitErrors = new List<DependencyError>();
        private readonly Feed _feed;
        private IPackageRepository _repository;
        private bool _online;

        public StubFeed(Feed feed)
        {
            _feed = feed;
            _online = true;

            UseRepository(new StubPackageRepository(feed.Url));
        }

        public StubFeed Add(string name, string version)
        {
            return Add(new Dependency(name, version));
        }

        public StubFeed Add(Dependency dependency)
        {
            Nugets.Add(new StubNuget(dependency, () => Repository.As<StubPackageRepository>().GetPackageByDependency(dependency)));
            return this;
        }

        public override bool IsOnline()
        {
            return _online;
        }

        public void MarkOffline()
        {
            _online = false;
        }

        protected override IRemoteNuget find(Dependency query)
        {
            throwIfNeeded(query);

            if (query.IsFloat() || query.Version.IsEmpty())
            {
                return Nugets.FirstOrDefault(x => query.MatchesName(x.Name));
            }

            var version = SemanticVersion.Parse(query.Version);
            var matching = Nugets.Where(x => query.MatchesName(x.Name));

            if (query.DetermineStability(_feed.Stability) == NugetStability.ReleasedOnly)
            {
                return matching.FirstOrDefault(x => x.Version.SpecialVersion.IsEmpty() && x.Version.Equals(version));
            }

            return matching.FirstOrDefault(x => x.Version.Version.Equals(version.Version));
        }

        public override IEnumerable<IRemoteNuget> FindLatestByName(string idPart)
        {
            return Nugets.Where(nuget => nuget.Name.Contains(idPart));
        }

        protected override IRemoteNuget findLatest(Dependency query)
        {
            Console.WriteLine("FindLatest in {0} for {1}", _repository.GetHashCode(), query);
            throwIfNeeded(query);

            return Nugets.Where(x => x.Name == query.Name)
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

	public class FloatingStubFeed : StubFeed, IFloatingFeed
	{
		public FloatingStubFeed(Feed feed) 
			: base(feed)
		{}

		public IEnumerable<IRemoteNuget> GetLatest()
		{
			var nugets = new List<IRemoteNuget>();

			var distinct = from nuget in Nugets
			               let name = nuget.Name.ToLower()
			               group nuget by name;

			distinct.Each(group =>
			{
				var latest = group.OrderByDescending(n => n.Version).First();
				nugets.Add(latest);
			});

			return nugets.OrderBy(x => x.Name);
		}
	}
}
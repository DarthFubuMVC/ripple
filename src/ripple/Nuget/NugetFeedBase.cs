using System.Collections.Generic;
using FubuCore.Util;
using NuGet;
using ripple.Model;

namespace ripple.Nuget
{
    public abstract class NugetFeedBase : INugetFeed
    {
        private static readonly NullRemoteNuget Null = new NullRemoteNuget();
        private readonly Cache<CacheKey<Dependency>, IRemoteNuget> _findCache = new Cache<CacheKey<Dependency>, IRemoteNuget>();
        private readonly Cache<CacheKey<string>, IRemoteNuget> _findLatest = new Cache<CacheKey<string>, IRemoteNuget>();

        public abstract bool IsOnline();

        public IRemoteNuget Find(Dependency query)
        {
            var key = new CacheKey<Dependency>(query.IncludesPrelease(), query);

            if (_findCache.Has(key) == false)
            {
                _findCache[key] = find(query) ?? Null;
            }

            return Return(_findCache[key]);
        }

        protected abstract IRemoteNuget find(Dependency query);

        public IRemoteNuget FindLatest(Dependency query)
        {
            var stability = query.IncludesPrelease();
            var key = new CacheKey<string>(stability, query.Name);

            if (_findLatest.Has(key) == false)
            {
                var nuget = findLatest(query);
                _findLatest[key] = nuget ?? Null;

                if (nuget != null && nuget.Version != null)
                {
                    var findKey = new CacheKey<Dependency>(stability, new Dependency(nuget.Name, nuget.Version.ToString()));
                    if (_findCache.Has(findKey) == false)
                    {
                        _findCache[findKey] = nuget;
                    }
                }
            }

            return Return(_findLatest[key]);
        }

        public abstract IEnumerable<IRemoteNuget> FindLatestByName(string idPrefix);
        
        protected abstract IRemoteNuget findLatest(Dependency query);

        private static IRemoteNuget Return(object nuget)
        {
            if (ReferenceEquals(Null, nuget))
                return null;

            return (IRemoteNuget)nuget;
        }

        public abstract IPackageRepository Repository { get; }

        private class CacheKey<TKeyPart>
        {
            private readonly bool _anything;
            private readonly TKeyPart _part;

            public CacheKey(bool anything, TKeyPart part)
            {
                _anything = anything;
                _part = part;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                var other = (CacheKey<TKeyPart>)obj;
                return _anything.Equals(other._anything) && _part.Equals(other._part);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (_anything.GetHashCode() * 397) ^ EqualityComparer<TKeyPart>.Default.GetHashCode(_part);
                }
            }
        }

        private class NullRemoteNuget : IRemoteNuget
        {
            public string Name { get; private set; }
            public SemanticVersion Version { get; private set; }
            public INugetFile DownloadTo(Solution solution, string directory)
            {
                throw new System.NotImplementedException();
            }

            public string Filename { get; private set; }
            public IEnumerable<Dependency> Dependencies()
            {
                throw new System.NotImplementedException();
            }
        }
    }
}
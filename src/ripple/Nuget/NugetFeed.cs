using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FubuCore;
using NuGet;
using ripple.Model;

namespace ripple.Nuget
{
    public class NugetFeed : NugetFeedBase
    {
        private readonly IPackageRepository _repository;
        private readonly string _url;
        protected NugetStability _stability;
        private Lazy<bool> _online;

        public NugetFeed(string url, NugetStability stability)
        {
            _url = url;
            _stability = stability;
            _repository = new PackageRepositoryFactory().CreateRepository(_url);

            _online = new Lazy<bool>(isOnline);
        }

        public string Url
        {
            get { return _url; }
        }

        public override bool IsOnline()
        {
            return _online.Value;
        }

        public override void MarkOffline()
        {
            _online = new Lazy<bool>(() => false);
        }

        private bool isOnline()
        {
            try
            {
                using (var client = new WebClient())
                {
                    using (var stream = client.OpenRead(_url))
                    {
                        return true;
                    }
                }
            }
            catch (Exception exc)
            {
                RippleLog.Debug("Feed unvailable: {0}".ToFormat(_url));
                RippleLog.Debug(exc.Message);
                return false;
            }
        }

        protected override IRemoteNuget find(Dependency query)
        {
            SemanticVersion version;
            if (!SemanticVersion.TryParse(query.Version, out version))
            {
                RippleLog.Debug("Could not find exact for " + query);
                return null;
            }

            var versionSpec = new VersionSpec(version);
            var package = _repository.FindPackages(query.Name, versionSpec, query.DetermineStability(_stability) == NugetStability.Anything, true).SingleOrDefault();

            if (package == null)
            {
                return null;
            }

            return new RemoteNuget(package);
        }

        public override IRemoteNuget FindLatestByName(string name)
        {
            return _repository
                .GetPackages()
                .Where(x => x.Id.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .LatestNuget();
        }

        public override IEnumerable<IRemoteNuget> FindAllLatestByName(string idPart)
        {
            return _repository.GetPackages()
                .Where(package => package.Id.Contains(idPart) && package.IsLatestVersion)
                .ToArray()
                .Select(package => new RemoteNuget(package));
        }

        protected override IRemoteNuget findLatest(Dependency query)
        {
            RippleLog.Debug("Searching for {0} from {1}".ToFormat(query, _url));
            var candidates = _repository.Search(query.Name, query.DetermineStability(_stability) == NugetStability.Anything)
                                        .Where(x => query.Name == x.Id).OrderBy(x => x.Id).ToList();

            return candidates.LatestNuget(query.VersionSpec);
        }

        public override IPackageRepository Repository { get { return _repository; } }

        public override string ToString()
        {
            return _url;
        }
    }
}
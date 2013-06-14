using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore;
using FubuCore.Descriptions;
using FubuCore.Logging;
using NuGet;
using ripple.Model;

namespace ripple.Nuget
{
    public class FileSystemNugetFeed : NugetFeedBase
    {
        private readonly string _directory;
        private readonly FubuCore.IFileSystem _fileSystem;
        private readonly NugetStability _stability;
        private IPackageRepository _repository;

        public FileSystemNugetFeed(string directory, NugetStability stability)
        {
            _directory = directory.ToCanonicalPath();

            Path.GetInvalidPathChars().Each(x =>
            {
                if (_directory.Contains(x))
                {
                    throw new InvalidOperationException("Invalid character in path: {0} ({1})".ToFormat(x, (int)x));
                }
            });

            _stability = stability;
            _fileSystem = new FileSystem();
        }

        public string Directory { get { return _directory; } }

        protected IEnumerable<INugetFile> files
        {
            get
            {
                var nupkgSet = new FileSet
                {
                    Include = "*.nupkg",
                    DeepSearch = false
                };

                return _fileSystem.FindFiles(_directory, nupkgSet).Select(x => new NugetFile(x, SolutionMode.Ripple));
            }
        }

        private IRemoteNuget findMatching(Func<INugetFile, bool> predicate)
        {
            var file = files.FirstOrDefault(predicate);
            if (file == null)
            {
                return null;
            }

            return new FileSystemNuget(file); 
        }

        public override bool IsOnline()
        {
            // TODO -- Make this smarter
            return true;
        }

        protected override IRemoteNuget find(Dependency query)
        {
            RippleLog.Debug("Searching for {0} in {1}".ToFormat(query, _directory));

            SemanticVersion version;
			if (!SemanticVersion.TryParse(query.Version, out version))
			{
				RippleLog.Debug("Could not find exact for " + query);
				return null;
			}

            return findMatching(nuget => query.MatchesName(nuget.Name) && nuget.Version == version);
        }

        public override IEnumerable<IRemoteNuget> FindLatestByName(string idPart)
        {
            // TODO: reconsided whether querying over files system should be enabled
            return Enumerable.Empty<IRemoteNuget>();
        }

        protected override IRemoteNuget findLatest(Dependency query)
        {
            RippleLog.Debug("Searching for latest of {0} in {1}".ToFormat(query, _directory));

            var nugets = files
                .Where(x => query.MatchesName(x.Name) && (!x.IsPreRelease || (x.IsPreRelease && query.DetermineStability(_stability) == NugetStability.Anything)))
                .ToList();
                
            var nuget = nugets
                .OrderByDescending(x => x.Version)
                .FirstOrDefault();

            if (nuget == null)
            {
                return null;
            }

            return new FileSystemNuget(nuget);
        }

        public override IPackageRepository Repository
        {
            get { return _repository; }
        }

        public override string ToString()
        {
            return _directory;
        }
    }

    public class FloatingFileSystemNugetFeed : FileSystemNugetFeed, IFloatingFeed
    {
        private readonly Lazy<IEnumerable<IRemoteNuget>> _nugets; 

        public FloatingFileSystemNugetFeed(string directory, NugetStability stability) 
            : base(directory, stability)
        {
            _nugets = new Lazy<IEnumerable<IRemoteNuget>>(findLatest);
        }

        private IEnumerable<IRemoteNuget> findLatest()
        {
            var nugets = new List<INugetFile>();

            RippleLog.Debug("Retrieving all latest from " + Directory);

			var distinct = from nuget in files
						   let name = nuget.Name.ToLower()
						   group nuget by name;

            distinct
                .Each(x =>
                {
                    var latest = x.OrderByDescending(n => n.Version).First();
                    nugets.Add(latest);
                });

            return nugets
                .Select(x => new FileSystemNuget(x))
                .OrderBy(x => x.Name);
        }

        public IEnumerable<IRemoteNuget> GetLatest()
        {
            return _nugets.Value;
        }

        public void DumpLatest()
        {
            var latest = GetLatest();
            var topic = new LatestFileNugets(latest, Directory);

            RippleLog.DebugMessage(topic);
        }

        public class LatestFileNugets : LogTopic, DescribesItself
        {
            private readonly IEnumerable<IRemoteNuget> _nugets;
            private readonly string _directory;

            public LatestFileNugets(IEnumerable<IRemoteNuget> nugets, string directory)
            {
                _nugets = nugets;
                _directory = directory;
            }

            public void Describe(Description description)
            {
                description.ShortDescription = "Files found in " + _directory;
                var list = description.AddList("Files", _nugets);
                list.Label = "Files";
            }
        }
    }
}
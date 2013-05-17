using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using FubuCore;
using FubuCore.Util;
using NuGet;
using ripple.Model;
using IFileSystem = FubuCore.IFileSystem;

namespace ripple.Nuget
{
    public class NugetFolderCache : INugetCache
    {
        private static bool _validatePackages = true;

	    private readonly Solution _solution;
	    private readonly string _folder;
	    private IFileSystem _fileSystem;

	    private Lazy<IEnumerable<INugetFile>> _allFiles;

        public NugetFolderCache(Solution solution, string folder)
        {
	        _solution = solution;
	        _folder = folder;
			_fileSystem = new FileSystem();

	        reset();
        }

		private void reset()
		{
			_allFiles = new Lazy<IEnumerable<INugetFile>>(findNugetFiles);
		}

		private IEnumerable<INugetFile> findNugetFiles()
		{
			var nugetSpec = new FileSet {Include = "*.nupkg"};
			_fileSystem
				.FindFiles(_folder, nugetSpec)
				.Each(file =>
				{
					try
					{
						if (new FileInfo(file).Length == 0)
						{
							_fileSystem.DeleteFile(file);
						}

					    if (_validatePackages)
					    {
					        using (new ZipPackage(file).GetStream())
					        {
					        }
					    }
					}
					catch
					{
						_fileSystem.DeleteFile(file);
					}
				});


			return _fileSystem
				.FindFiles(_folder, nugetSpec)
				.Select(file => new NugetFile(file, _solution.Mode))
				.ToList();
		}

	    private IEnumerable<INugetFile> Dependencies
	    {
			get { return _allFiles.Value; }
	    }

		public void UseFileSystem(IFileSystem fileSystem)
		{
			_fileSystem = fileSystem;
		}

	    public void Update(IRemoteNuget nuget)
	    {
		    UpdateAll(new[] { nuget });
	    }

	    public void UpdateAll(IEnumerable<IRemoteNuget> nugets)
        {
            var latest = new Cache<string, Version>(name => new Version("0.0.0.0"));
            Dependencies.GroupBy(x => x.Name).Each(x => {
                var version = x.OrderByDescending(file => file.Version).First().Version.Version;
                latest[x.Key] = version;
            });

            nugets.Each(nuget => {
                var localVersion = latest[nuget.Name];
                var remoteVersion = nuget.Version.Version;

                if (remoteVersion > localVersion)
                {
                    Console.WriteLine("Downloading {0} to {1}", nuget.Filename, _folder);
                    nuget.DownloadTo(_solution, _folder);
                }
            });

		    reset();
        }

		public INugetFile Latest(Dependency query)
        {
            IEnumerable<INugetFile> files = Dependencies.Where(x => x.Name == query.Name).ToList();
            if (query.IsReleasedOnly())
            {
                files = files.Where(x => x.Version.SpecialVersion.IsEmpty());
            }

            return files.OrderByDescending(x => x.Version).FirstOrDefault();
        }

        public void Flush()
        {
            _fileSystem.CleanDirectory(_folder);
        }

        public IEnumerable<INugetFile> AllFiles()
        {
	        return Dependencies;
        }

		public virtual INugetFile Find(Dependency query)
        {
            return Dependencies.FirstOrDefault(x => query.MatchesName(x.Name) && x.Version.Equals(query.SemanticVersion()));
        }

	    public IRemoteNuget Retrieve(IRemoteNuget nuget)
	    {
		    if (nuget is FileSystemNuget)
		    {
			    return nuget;
		    }

		    var dependency = new Dependency(nuget.Name, nuget.Version.Version.ToString());
		    var file = Find(dependency);

			if (file == null)
			{
				return new CacheableNuget(nuget, _folder);
			}

		    return new FileSystemNuget(file);
	    }

        public Feed ToFeed()
        {
            return Feed.FromPath(_folder);
        }

        public string LocalPath { get { return _folder; } }

		public static NugetFolderCache DefaultFor(Solution solution)
		{
		    var ripple = solution.NugetCacheDirectory;
		    if (string.IsNullOrEmpty(ripple))
		    {
		        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		        ripple = Path.Combine(appData, "ripple");
		    }

		    return For(ripple, solution);
		}

        public static NugetFolderCache For(string directory, Solution solution)
        {
            new FileSystem().CreateDirectory(directory);
            return new NugetFolderCache(solution, directory);
        }

        public static void DisableValidation()
        {
            _validatePackages = false;
        }

        public static void Reset()
        {
            _validatePackages = true;
        }
    }
}
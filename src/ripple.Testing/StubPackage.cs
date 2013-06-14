using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using FubuCore;
using FubuCore.Util;
using NuGet;
using ripple.Model;

namespace ripple.Testing
{
	public class StubPackage : IPackage
	{
		private readonly IList<PackageDependency> _dependencies = new List<PackageDependency>();

		public StubPackage(string id, string version)
		{
			Id = id;
			Version = SemanticVersion.Parse(version);
		}

		#region IPackage Stuff
		public string Id { get; private set; }
		public SemanticVersion Version { get; private set; }
		public string Title { get; private set; }
		public IEnumerable<string> Authors { get; private set; }
		public IEnumerable<string> Owners { get; private set; }
		public Uri IconUrl { get; private set; }
		public Uri LicenseUrl { get; private set; }
		public Uri ProjectUrl { get; private set; }
		public bool RequireLicenseAcceptance { get; private set; }
		public string Description { get; private set; }
		public string Summary { get; private set; }
		public string ReleaseNotes { get; private set; }
		public string Language { get; private set; }
		public string Tags { get; private set; }
		public string Copyright { get; private set; }
		public IEnumerable<FrameworkAssemblyReference> FrameworkAssemblies { get; private set; }
	    public ICollection<PackageReferenceSet> PackageAssemblyReferences { get; private set; }

	    public IEnumerable<PackageDependencySet> DependencySets 
		{
 			get
 			{
 				yield return new PackageDependencySet(null, _dependencies);
 			}
		}

	    public Version MinClientVersion { get; private set; }

	    public Uri ReportAbuseUrl { get; private set; }
		public int DownloadCount { get; private set; }

		public IEnumerable<IPackageFile> GetFiles()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<FrameworkName> GetSupportedFrameworks()
		{
			throw new NotImplementedException();
		}

		public Stream GetStream()
		{
			throw new NotImplementedException();
		}

		public bool IsAbsoluteLatestVersion { get; private set; }
		public bool IsLatestVersion { get; private set; }
		public bool Listed { get; private set; }
		public DateTimeOffset? Published { get; private set; }
		public IEnumerable<IPackageAssemblyReference> AssemblyReferences { get; private set; }
		#endregion

        public DependencyExpression DependsOn(PackageDependency dependency)
		{
			_dependencies.Add(dependency);
            return new DependencyExpression(dependency, _dependencies);
		}

        public DependencyExpression DependsOn(string id, string version)
		{
			return DependsOn(new PackageDependency(id, new VersionSpec(SemanticVersion.Parse(version))));
		}

        public DependencyExpression DependsOn(string id)
		{
			return DependsOn(new PackageDependency(id));
		}

        // This is painfully stupid
        public class DependencyExpression
        {
            private PackageDependency _dependency;
            private readonly IList<PackageDependency> _dependencies;

            public DependencyExpression(PackageDependency dependency, IList<PackageDependency> dependencies)
            {
                _dependency = dependency;
                _dependencies = dependencies;
            }

            public DependencyExpression Min(string version)
            {
                var spec = _dependency.VersionSpec as VersionSpec;
                if (spec != null)
                {
                    spec.MinVersion = new SemanticVersion(version);
                    spec.MaxVersion = null;
                    return this;
                }

                spec = new VersionSpec(new SemanticVersion(version)) { MaxVersion = null };
                _dependencies.Remove(_dependency);

                var dependency = new PackageDependency(_dependency.Id, spec);
                _dependencies.Fill(dependency);

                return new DependencyExpression(dependency, _dependencies);
            }

            public DependencyExpression Max(string version)
            {
                var spec = _dependency.VersionSpec as VersionSpec;
                if (spec != null)
                {
                    spec.MaxVersion = new SemanticVersion(version);
                    return this;
                }

                spec = new VersionSpec(new SemanticVersion(version));
                _dependencies.Remove(_dependency);

                var dependency = new PackageDependency(_dependency.Id, spec);
                _dependencies.Fill(dependency);

                return new DependencyExpression(dependency, _dependencies);
            }
        }
	}

	public class StubPackageRepository : IPackageRepository
	{
        private readonly Cache<Dependency, IPackage> _packages = new Cache<Dependency, IPackage>(x => new StubPackage(x.Name, x.Version));

	    public StubPackageRepository(string url)
	    {
	        Source = url;
	    }

	    public IQueryable<IPackage> GetPackages()
		{
			return _packages.AsQueryable();
		}

		public void AddPackage(IPackage package)
		{
            _packages.Fill(new Dependency(package.Id, package.Version.ToString()), package);
		}

		public void RemovePackage(IPackage package)
		{
			_packages.Remove(new Dependency(package.Id, package.Version.ToString()));
		}

        public IPackage GetPackageByDependency(Dependency dependency)
        {
            return _packages[dependency];
        }

		public void ConfigurePackage(string id, string version, Action<StubPackage> configure)
		{
		    var package = _packages[new Dependency(id, version)].As<StubPackage>();
			configure(package);
		}

		public string Source { get; private set; }
		public bool SupportsPrereleasePackages { get; private set; }
	}
}
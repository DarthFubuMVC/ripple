using System;
using FubuCore;
using NuGet;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Testing
{
	public class StubNugetFile : INugetFile
	{
		public StubNugetFile(Dependency dependency)
		{
			Name = dependency.Name;
			Version = SemanticVersion.Parse(dependency.Version);
		}

		public string FileName { get; set; }
		public string Name { get; private set; }
		public SemanticVersion Version { get; private set; }
	    public bool IsPreRelease { get; private set; }

	    public IPackage ExplodeTo(string directory)
		{
			var explodedDirectory = directory.AppendPath(Name).ToFullPath();
			RippleLog.Info("Exploding to " + explodedDirectory);

			var fileSystem = new FileSystem();
			fileSystem.CreateDirectory(explodedDirectory);
			fileSystem.CleanDirectory(explodedDirectory);

			fileSystem.DeleteFile(FileName);

			fileSystem.WriteStringToFile(explodedDirectory.AppendPath(FileName), "");
			
			return new StubPackage(Name, Version.ToString());
		}

		public INugetFile CopyTo(string directory)
		{
			return this;
		}

		public string NugetFolder(Solution solution)
		{
			return solution.PackagesDirectory().AppendPath(Name);
		}

	    public void ExplodeSourcesTo(string directory)
	    {
	        throw new NotImplementedException();
	    }

	    public bool IsSourceOnlyPackage()
	    {
	        return false;
	    }
	}
}
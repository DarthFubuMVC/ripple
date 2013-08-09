using System.Collections.Generic;
using System.Linq;
using FubuCore;
using ripple.Model;

namespace ripple.Classic
{
	public class NuGetSolutionLoader : ISolutionLoader
	{
		// Hate this
		public SolutionConfig Config { get; set; }

		public Solution LoadFrom(IFileSystem fileSystem, string filePath)
		{
			Config = fileSystem.LoadFromFile<SolutionConfig>(filePath);
			var solution = new Solution { Mode = SolutionMode.Classic };

			solution.Name = Config.Name;
			solution.SourceFolder = Config.SourceFolder;
			solution.FastBuildCommand = Config.FastBuildCommand;
			solution.BuildCommand = Config.BuildCommand;
			solution.NugetSpecFolder = Config.NugetSpecFolder;
            solution.AddFeeds(Config.Feeds);

			return solution;
		}

		public void SolutionLoaded(Solution solution)
		{
			MarkFloatingDependencies(Config, solution);
			ExtractSolutionLevelConfiguration(Config, solution);
		}

		public void MarkFloatingDependencies(SolutionConfig config, Solution solution)
		{
			config.Floats.Each(x => solution.Projects.Each(project =>
			{
				var dependency = project.Dependencies.Find(x);
				if (dependency != null)
				{
					dependency.Float();
				}
			}));
		}

		public void ExtractSolutionLevelConfiguration(SolutionConfig config, Solution solution)
		{
			var specificDependencies = new List<Dependency>();
			solution.Projects.Each(project => specificDependencies.AddRange(project.Dependencies.Where(x => !x.IsFloat())));

			specificDependencies.Each(dependency =>
			{
				solution.AddDependency(new Dependency(dependency.Name, dependency.Version, UpdateMode.Fixed));

				dependency.Float();
			});
		}
	}
}
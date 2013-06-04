using System;
using System.Collections.Generic;
using System.IO;
using FubuCore;
using FubuCore.Util;
using ripple.Commands;
using ripple.Local;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Testing
{
    public class ProjectGenerator
    {
        private static readonly IFileSystem FileSystem = new FileSystem();

        public static Project Create(string projectDir, string name)
        {
            FileSystem.CreateDirectory(projectDir);

            var stream = typeof(SolutionGraphScenario)
                .Assembly
                .GetManifestResourceStream("{0}.ProjectTemplate.txt".ToFormat(typeof(SolutionGraphScenario).Namespace));

            var projectFile = Path.Combine(projectDir, RippleDependencyStrategy.RippleDependenciesConfig);
            FileSystem.WriteStringToFile(projectFile, "");

            var csProjectFile = Path.Combine(projectDir, "{0}.csproj".ToFormat(name));
            FileSystem.WriteStreamToFile(csProjectFile, stream);

            return new Project(csProjectFile);
        }
    }

	public class SolutionGraphScenario
	{
		private readonly string _directory;
	    private readonly string _cacheDirectory;
	    private readonly IFileSystem _fileSystem;
		private readonly Lazy<SolutionGraph> _graph;

		public SolutionGraphScenario(string directory, string cacheDirectory)
		{
			_directory = directory;
		    _cacheDirectory = cacheDirectory;
		    _fileSystem = new FileSystem();

			var builder = new SolutionGraphBuilder(_fileSystem);
			_graph = new Lazy<SolutionGraph>(() => builder.ReadFrom(_directory));

            RippleFileSystem.StubCurrentDirectory(_directory);
		}

		public Solution Find(string name)
		{
			var solution =  Graph[name];
            solution.UseCache(NugetFolderCache.For(_cacheDirectory, solution));

		    return solution;
		}

		public SolutionGraph Graph { get { return _graph.Value; } }

		public string Directory { get { return _directory; } }

		public void Cleanup()
		{
            _fileSystem.ForceClean(_directory);
            _fileSystem.DeleteDirectory(_directory);

		    RippleFileSystem.Live();
		}

		public string DirectoryForSolution(string solutionName)
		{
			return _directory.AppendPath(solutionName);
		}

		public static SolutionGraphScenario Create(Action<SolutionGraphScenarioDefinition> configure)
		{
			var definition = new SolutionGraphScenarioDefinition();
			configure(definition);

			return definition.As<ISolutionGraphScenarioBuilder>().Build();
		}

		public interface ISolutionGraphScenarioBuilder
		{
			string Directory { get; }
			void AddSolution(Solution solution);
			void AddModification(SolutionModification modification);
			SolutionGraphScenario Build();
		}

		public class SolutionGraphScenarioDefinition : ISolutionGraphScenarioBuilder
	 	{
			private readonly IList<Solution> _solutions = new List<Solution>();
			private readonly IList<SolutionModification> _modifications = new List<SolutionModification>();
			private readonly string _directory;
		    private readonly IFileSystem _fileSystem = new FileSystem();

			public SolutionGraphScenarioDefinition()
			{
				_directory = Path.GetTempPath().AppendRandomPath();

                _fileSystem.CleanDirectory(_directory);
				_fileSystem.DeleteDirectory(_directory);
                _fileSystem.DeleteDirectory(_directory);
				_fileSystem.CreateDirectory(_directory);

                _fileSystem.CreateDirectory(cacheDirectory);
			}

            private string cacheDirectory { get { return _directory.AppendPath("cache"); } }

            public void AddCachedNuget(string id, string version)
            {
                var fileName = "{0}.{1}.nupkg".ToFormat(id, version);
                _fileSystem.WriteStringToFile(Path.Combine(cacheDirectory, fileName), fileName);
            }

			public void Solution(string name)
			{
				Solution(name, x => { });
			}

			public void Solution(string name, Action<SolutionExpression> configure)
			{
				var expression = new SolutionExpression(this, name);
				configure(expression);
			}

			string ISolutionGraphScenarioBuilder.Directory { get { return _directory; } }

			void ISolutionGraphScenarioBuilder.AddSolution(Solution solution)
			{
				_solutions.Add(solution);
			}

			void ISolutionGraphScenarioBuilder.AddModification(SolutionModification modification)
			{
				_modifications.Add(modification);
			}

			SolutionGraphScenario ISolutionGraphScenarioBuilder.Build()
	 		{
	 			_modifications.Each(x => x.Execute());
	 			_solutions.Each(solution => solution.Save(true));
	 			return new SolutionGraphScenario(this.As<ISolutionGraphScenarioBuilder>().Directory, cacheDirectory);
	 		}
	 	}

		public class SolutionModification
		{
			private readonly Solution _solution;
			private readonly Action<Solution> _modify;

			public SolutionModification(Solution solution, Action<Solution> modify)
			{
				_solution = solution;
				_modify = modify;
			}

			public void Execute()
			{
				_modify(_solution);
			}
		}

		public class SolutionExpression
		{
			private readonly Solution _solution;
			private readonly IFileSystem _fileSystem;
			private readonly Cache<string, Project> _projects;
			private readonly ISolutionGraphScenarioBuilder _builder;

			public SolutionExpression(ISolutionGraphScenarioBuilder builder, string name)
			{
				_fileSystem = new FileSystem();

				var solutionDir = Path.Combine(builder.Directory, name);
				_fileSystem.CreateDirectory(solutionDir);

				var solutionFile = Path.Combine(solutionDir, SolutionFiles.ConfigFile);
				_fileSystem.WriteStringToFile(solutionFile, "");

				_solution = new Solution
				{
					Name = name,
					Path = solutionFile
				};

				_solution.Directory = solutionDir;
				_solution.SourceFolder = Path.Combine(solutionDir, "src");
				_solution.NugetSpecFolder = Path.Combine(solutionDir, "packaging", "nuget");

				_fileSystem.CreateDirectory(_solution.SourceFolder);

				builder.AddSolution(_solution);

				_builder = builder;

				_projects = new Cache<string, Project>(createAndAddProject);

				addDefaultProject();
			}

			private void addDefaultProject()
			{
				_projects.FillDefault(_solution.Name);
			}

			private Project createAndAddProject(string name)
			{
				var projectDir = Path.Combine(_solution.SourceFolder, name);
			    var project = ProjectGenerator.Create(projectDir, name);

				var debugDir = Path.Combine(projectDir, "bin", "Debug");
				_fileSystem.CreateDirectory(debugDir);
				_fileSystem.WriteStringToFile(Path.Combine(debugDir, "{0}.dll".ToFormat(name)), "");

				_solution.AddProject(project);

				return project;
			}

            public void SolutionDependency(string id, string version, UpdateMode mode)
            {
                Modify(sln => sln.AddDependency(new Dependency(id, version, mode)));
            }

			public void Modify(Action<Solution> modify)
			{
				_builder.AddModification(new SolutionModification(_solution, modify));
			}

			public void Mode(SolutionMode mode)
			{
				Modify(x => x.ConvertTo(mode));
			}

			public void Publishes(string name)
			{
				Publishes(name, null);
			}

			public void Publishes(string name, Action<PublishesExpression> configure)
			{
				var packagingDir = _solution.NugetSpecFolder;
				var specFile = Path.Combine(packagingDir, "{0}.nuspec".ToFormat(name));

				var nuspec = GetType()
					.Assembly
					.GetManifestResourceStream(GetType(), "NuspecTemplate.txt")
                    .ReadAllText();

				_fileSystem.WriteStringToFile(specFile, nuspec.Replace("${Name}", name));

				var document = new NuspecDocument(specFile);
				document.Name = name;

				var expression = new PublishesExpression(document);

				if (configure == null)
				{
					expression.Assembly("{0}.dll".ToFormat(name), "lib");
				}
				else
				{
					configure(expression);
				}

				document.SaveChanges();
			}

            public void LocalDependency(string id, string version)
            {
                var fileName = "{0}.{1}.nupkg".ToFormat(id, version);
                var nugetDir = Path.Combine(_solution.PackagesDirectory(), id);
                _fileSystem.CreateDirectory(nugetDir);
                var filename = Path.Combine(_solution.PackagesDirectory(), id, fileName);
                _fileSystem.WriteStringToFile(filename, "");
            }

			public void ProjectDependency(string project, string id)
			{
				_projects[project].AddDependency(new Dependency(id));
			}

		    public void GroupDependencies(params string[] dependencies)
		    {
		        Modify(solution =>
		        {
                    var group = new DependencyGroup();
		            dependencies.Each(x => group.Dependencies.Add(new GroupedDependency(x)));

                    solution.Groups.Add(group);
		        });
		    }
		}

		public class PublishesExpression
		{
			private readonly NuspecDocument _spec;

			public PublishesExpression(NuspecDocument spec)
			{
				_spec = spec;
			}

			public PublishesExpression DependsOn(string name)
			{
				_spec.AddDependency(new NuspecDependency(name));
				return this;
			}

			public PublishesExpression Assembly(string assembly, string target)
			{
				var name = assembly.Replace(".dll", "");
				// ..\..\src\Bottles\bin\Debug\Bottles.dll
				var relativePath = "..{0}..{0}src{0}{1}{0}bin{0}Debug{0}{2}".ToFormat(Path.DirectorySeparatorChar, name, assembly); 

				_spec.AddPublishedAssembly(relativePath);
				return this;
			}
		}
	}
}
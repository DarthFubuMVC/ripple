using System.Collections.Generic;
using System.Linq;
using FubuCore;
using ripple.Model;

namespace ripple.Nuget
{
    public interface INugetStorage
    {
        void Clean(Solution solution, CleanMode mode);

        void Write(Solution solution);
        void Write(Project project);

        void Reset(Solution solution);

        LocalDependencies Dependencies(Solution solution);

        IEnumerable<Dependency> MissingFiles(Solution solution);
    }

    public class NugetStorage : INugetStorage
    {
        private readonly IFileSystem _fileSystem;
        private readonly IDependencyStrategy _strategy;

        public NugetStorage(IFileSystem fileSystem, IDependencyStrategy strategy)
        {
            _fileSystem = fileSystem;
            _strategy = strategy;
        }

        public IDependencyStrategy Strategy { get { return _strategy; } }

        public void Clean(Solution solution, CleanMode mode)
        {
            if (mode == CleanMode.all || mode == CleanMode.packages)
            {
                var packagesFolder = solution.PackagesDirectory();
                _fileSystem.CleanWithTracing(packagesFolder);
            }

            if (mode == CleanMode.all || mode == CleanMode.projects)
            {
                solution.Projects.Each(p => p.Clean(_fileSystem));
            }

        }

        public void Write(Solution solution)
        {
            _fileSystem.PersistToFile(solution, solution.Path);
        }

        public void Write(Project project)
        {
            _strategy.Write(project);
            project.Proj.Write();
        }

        public void Reset(Solution solution)
        {
            solution.Projects.Each(x => _strategy.RemoveDependencyConfigurations(x));
            Clean(solution, CleanMode.packages);
        }

        public LocalDependencies Dependencies(Solution solution)
        {
            var nupkgSet = new FileSet
            {
                DeepSearch = true,
                Include = "*.nupkg"
            };

            var files = _fileSystem
              .FindFiles(solution.PackagesDirectory(), nupkgSet)
              .Select(x => _strategy.FileFor(x)).ToList();

            return new LocalDependencies(files);
        }

        public IEnumerable<Dependency> MissingFiles(Solution solution)
        {
            var dependencies = Dependencies(solution);
            var restore = solution.RestoreSettings;

            return solution
              .Dependencies
              .Where(dependency => dependencies.ShouldRestore(dependency, restore.ShouldForce(dependency)));
        }

        public static NugetStorage Basic()
        {
            return new NugetStorage(new FileSystem(), new RippleDependencyStrategy());
        }

        public static NugetStorage Classic()
        {
            return new NugetStorage(new FileSystem(), new NuGetDependencyStrategy());
        }

        public static NugetStorage For(SolutionMode mode)
        {
            return mode == SolutionMode.Classic ? Classic() : Basic();
        }
    }
}
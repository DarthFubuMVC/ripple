using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore;
using ripple.Classic;
using ripple.Model.Xml;

namespace ripple.Model
{
    public class SolutionFiles : ISolutionFiles
    {
        public const string ConfigFile = "ripple.config";

        private static readonly IList<ISolutionLoader> Loaders = new List<ISolutionLoader>();
 
        static SolutionFiles()
        {
            Reset();
        }

        public static void Reset()
        {
            Loaders.Clear();
            AddLoader(new XmlSolutionLoader());
        }

        public static void AddLoader(ISolutionLoader loader)
        {
            Loaders.Add(loader);
        }

        private readonly IFileSystem _fileSystem;
        private readonly ISolutionLoader _loader;

        public SolutionFiles(IFileSystem fileSystem, ISolutionLoader loader)
        {
            if (RippleFileSystem.IsSolutionDirectory())
            {
                resetDirectories(RippleFileSystem.FindSolutionDirectory());
            }

            _fileSystem = fileSystem;
            _loader = loader;
        }

        private void resetDirectories(string root)
        {
            RootDir = root;
        }

        public string RootDir { get; set; }

        public SolutionMode Mode
        {
            get
            {
                if (_loader is SolutionLoader) return SolutionMode.Ripple;
                return SolutionMode.Classic;
            }
        }

        public void ForProjects(Solution solution, Action<string> action)
        {
            var projSet = new FileSet { Include = "*.csproj;*.vbproj;*.fsproj" };
            var targetDir = Path.Combine(solution.Directory, solution.SourceFolder);

            _fileSystem.FindFiles(targetDir, projSet).Each(action);
        }

        public void ForNuspecs(Solution solution, Action<string> action)
        {
            var nuspecSet = new FileSet { Include = "*.nuspec" };
            var targetDir = Path.Combine(solution.Directory, solution.NugetSpecFolder);

            _fileSystem.FindFiles(targetDir, nuspecSet).Each(action);
        }

        public Solution LoadSolution()
        {
            var file = Path.Combine(RootDir, ConfigFile);

            var solution = _loader.LoadFrom(_fileSystem, file);
            solution.Path = file;

            return solution;
        }

        public void FinalizeSolution(Solution solution)
        {
            _loader.SolutionLoaded(solution);
        }

        public static SolutionFiles Basic()
        {
            return new SolutionFiles(new FileSystem(), new SolutionLoader());
        }

        public static SolutionFiles FromDirectory(string directory)
        {
            var rippleConfigs = new FileSet
            {
                Include = RippleDependencyStrategy.RippleDependenciesConfig,
                DeepSearch = true
            };

            var isClassicMode = false;
            var configFiles = new FileSystem().FindFiles(directory, rippleConfigs);

            if (!configFiles.Any())
            {
                isClassicMode = true;
                RippleLog.Info("Classic Mode Detected");
            }

            var files = isClassicMode ? Classic() : Basic();
            files.resetDirectories(directory);

            return files;
        }

        public static SolutionFiles FromDirectory(string directory, ISolutionLoader loader)
        {
            var files = new SolutionFiles(new FileSystem(), loader);
            files.resetDirectories(directory);

            return files;
        }

        public static SolutionFiles Classic()
        {
            return new SolutionFiles(new FileSystem(), new ClassicRippleSolutionLoader());
        }

        public static SolutionFiles For(SolutionMode mode)
        {
            return mode == SolutionMode.Classic ? Classic() : Basic();
        }
    }
}
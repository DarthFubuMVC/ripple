using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using FubuCore;
using FubuCore.Descriptions;
using NuGet;
using ripple.Model;

namespace ripple.Nuget
{
    public class NugetName
    {
        public string Name { get; private set; }
        public SemanticVersion Version { get; private set; }

        private static readonly Regex PackageVersionPattern = new Regex(@"(.*?)\.((?<!\w)[\d.]+(?:.*)).nupkg",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static NugetName Parse(string input)
        {
            var matches = PackageVersionPattern.Match(input);
            if (!matches.Success) throw new InvalidOperationException("Invalid package name");

            return new NugetName
            {
                Name = matches.Groups[1].Value,
                Version = SemanticVersion.Parse(matches.Groups[2].Value)
            };
        }
    }

    public class NugetFile : INugetFile, DescribesItself
    {
        private readonly string _path;
        private readonly SolutionMode _mode;

        public NugetFile(string path, SolutionMode mode)
        {
            _path = path;
            _mode = mode;

            var fileName = Path.GetFileName(_path);

            var result = NugetName.Parse(fileName);

            Name = result.Name;
            Version = result.Version;
            IsPreRelease = Version.SpecialVersion.IsNotEmpty();
        }

        public string FileName { get { return _path; } }
        public string Name { get; private set; }
        public SemanticVersion Version { get; private set; }
        public bool IsPreRelease { get; private set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, Version: {1}, IsPreRelease: {2}", Name, Version, IsPreRelease);
        }

        public virtual string ExplodedDirectory(string directory)
        {
            if (_mode == SolutionMode.Classic)
                return directory.AppendPath(Name + "." + Version);

            return directory.AppendPath(Name).ToFullPath();
        }

        public string NugetFolder(Solution solution)
        {
            return ExplodedDirectory(solution.PackagesDirectory());
        }

        public void ExplodeSourcesTo(string directory)
        {
            RippleLog.Info("Exploding source only package {0} to {1}".ToFormat(Name,directory));
            
            var fileSystem = new FileSystem();
            fileSystem.CreateDirectory(directory);
            fileSystem.ForceClean(directory);

            var package = new ZipPackage(_path);

            package.GetFiles().Each(file =>
            {
                if (!file.Path.Contains("content\\"))
                {
                    RippleLog.Info("File {0} is not in the content folder and will be skipped".ToFormat(file.Path));
                    return;
                }

                var relativePath = file.Path.Replace("content\\", "");


                var target = directory.AppendPath(relativePath);
                fileSystem.CreateDirectory(target.ParentDirectory());

                using (var stream = new FileStream(target, FileMode.Create, FileAccess.Write))
                {
                    file.GetStream().CopyTo(stream);
                }
            });
        }

        public bool IsSourceOnlyPackage()
        {
            var name = Name.ToLower();

            return name.EndsWith(".sources") || name.EndsWith("-codeonly");
        }

        public IPackage ExplodeTo(string directory)
        {
            var explodedDirectory = ExplodedDirectory(directory);

            RippleLog.Info("Exploding to " + explodedDirectory);

            var fileSystem = new FileSystem();
            fileSystem.CreateDirectory(explodedDirectory);
            fileSystem.ForceClean(explodedDirectory);

            var package = new ZipPackage(_path);

            package.GetFiles().Each(file =>
            {
                var target = explodedDirectory.AppendPath(file.Path);
                fileSystem.CreateDirectory(target.ParentDirectory());

                using (var stream = new FileStream(target, FileMode.Create, FileAccess.Write))
                {
                    file.GetStream().CopyTo(stream);
                }
            });

            fileSystem.CopyToDirectory(_path, explodedDirectory);

            fileSystem.DeleteFile(_path);

            var newFile = Path.Combine(explodedDirectory, Path.GetFileName(_path));
            return new ZipPackage(newFile);
        }

        public INugetFile CopyTo(string directory)
        {
            var target = Path.Combine(directory, Path.GetFileName(_path));
            new FileSystem().Copy(_path, target);

            return new NugetFile(target, _mode);
        }


        public void Describe(Description description)
        {
            description.Title = Name;
            description.ShortDescription = "Version: {0}, IsPreRelease: {1}".ToFormat(Version, IsPreRelease);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore;
using FubuCore.Util;
using FubuCsProjFile;
using NuGet;
using ripple.Model;
using Solution = ripple.Model.Solution;

namespace ripple.MSBuild
{
    public class ProjFile
    {
        private readonly string _filename;
        private readonly Solution _solution;
        private readonly CsProjFile _project;

        public ProjFile(string filename, Solution solution)
        {
            _filename = filename;
            _solution = solution;

            if (File.Exists(_filename))
            {
                _project = CsProjFile.LoadFrom(_filename);
            }
            else
            {
                _project = CsProjFile.CreateAtLocation(_filename, solution.Name);
            }
        }

        public CsProjFile Project { get { return _project; } }

        public IEnumerable<AssemblyReference> References { get { return _project.All<AssemblyReference>(); } } 
        public IEnumerable<ProjectReference> ProjectReferences { get { return _project.All<ProjectReference>(); }}

        public bool UsesPackagesConfig()
        {
            return FindPackagesConfigItem() != null;
        }

        public None FindPackagesConfigItem()
        {
            return _project
                .All<None>()
                .FirstOrDefault(x => x.Include == "packages.config");
        }

        public void ConvertToRippleDependenciesConfig()
        {
            var item = FindPackagesConfigItem();
            if (item == null)
            {
                _project.Add(new None("ripple.dependencies.config"));
                return;
            }

            _project.Remove(item);

            item.Include = "ripple.dependencies.config";
            _project.Add(item);
        }

        public ReferenceStatus AddReference(string name, string hintPath)
        {
            if (hintPath.IsNotEmpty())
            {
                hintPath = hintPath.Trim();
            }

            var reference = FindReference(name);
            if (reference == null)
            {
                reference = new AssemblyReference(name, hintPath);
                _project.Add(reference);
            }

            var original = reference.HintPath;
            reference.HintPath = hintPath;

            if (original.IsNotEmpty())
            {
                original = original.Trim();
            }

            var status = string.Equals(original, hintPath, StringComparison.OrdinalIgnoreCase) ? ReferenceStatus.Unchanged : ReferenceStatus.Changed;
            if (status == ReferenceStatus.Changed)
            {
                RippleLog.Debug("HintPath changed: " + original + " to " + hintPath);
                _project.Remove(reference);
                _project.Add(reference);
            }

            return status;
        }

        public AssemblyReference FindReference(string name)
        {
            return _project.All<AssemblyReference>().FirstOrDefault(x => x.Include == name);
        }

        public bool RemoveReference(string name)
        {
            AssemblyReference reference = FindReference(name);
            if (reference == null) return false;

            _project.Remove(reference);

            return true;
        }

        public bool RemoveReferences(IEnumerable<string> references)
        {
            var removed = references.All(RemoveReference);
            Write();

            return removed;
        }

        public void Write()
        {
            _project.Save();
        }

        public void AddAssemblies(Dependency dep, IEnumerable<PackageReferenceSet> sets, IEnumerable<IPackageAssemblyReference> assemblies)
        {
            var explicitRefs = findCompatibleItems(sets);
            var references = assemblies.Where(x => explicitRefs.Any(r => r.EqualsIgnoreCase(x.Name))).ToArray();

            AddAssemblies(dep, references);
        }

        public void AddAssemblies(Dependency dep, IEnumerable<IPackageAssemblyReference> assemblies)
        {
            bool needsSaved = false;

            assemblies = findCompatibleItems(assemblies).ToList();

            assemblies.Each(assem =>
            {
                var assemblyName = Path.GetFileNameWithoutExtension(assem.Name);

                if (assemblyName == "_._" || assemblyName == "_") return;

                if (!_solution.ShouldAddReference(dep, assemblyName)) return;

                var nugetDir = _solution.NugetFolderFor(dep.Name);
                var assemblyPath = nugetDir.AppendPath(assem.Path);
                var hintPath = assemblyPath.PathRelativeTo(Path.GetFullPath(_filename.ParentDirectory()));

                if (AddReference(assemblyName, hintPath) == ReferenceStatus.Changed)
                {
                    RippleLog.Debug("Updated reference for {0} to {1}".ToFormat(_filename, hintPath));
                    needsSaved = true;
                }
            });

            if (needsSaved)
            {
                RippleLog.Debug("Writing changes to " + _filename);
                Write();
            }
        }

        public void RemoveDuplicateReferences()
        {
            var allReferences = _project.All<AssemblyReference>().ToArray();
            var references = allReferences.Select(x =>
            {
                var name = x.Include;
                if (name.Contains(","))
                {
                    name = name.Split(',').First();
                }

                return name;
            });

            var counts = new Cache<string, List<AssemblyReference>>(x => new List<AssemblyReference>());
            references.Each(dependency =>
            {
                var duplicates = allReferences.Where(x =>
                {
                    if (x.Include.EqualsIgnoreCase(dependency)) return true;

                    var guessedName = x.Include.Split(',').First().Trim();

                    return dependency.EqualsIgnoreCase(guessedName);
                });

                counts[dependency].Fill(duplicates);
            });

            var removals = new List<string>();
            counts
                .Where(x => x.Count > 1)
                .Each(duplicates => duplicates.Where(x => x.Include.Contains(",")).Each(x => removals.Add(x.Include)));

            RemoveReferences(removals);
        }

        // Manually tested for now. Sucks, but it is what it is.
        private IEnumerable<string> findCompatibleItems(IEnumerable<PackageReferenceSet> sets)
        {
            IEnumerable<PackageReferenceSet> compatibleItems;

            if (VersionUtility.TryGetCompatibleItems(_project.FrameworkName, sets, out compatibleItems))
            {
                var set = compatibleItems.FirstOrDefault();
                return set == null ? Enumerable.Empty<string>() : set.References;
            }

            return Enumerable.Empty<string>();
        }

        // Manually tested for now. Sucks, but it is what it is.
        private IEnumerable<T> findCompatibleItems<T>(IEnumerable<T> items) where T : IFrameworkTargetable
        {
            IEnumerable<T> compatibleItems;

            if (VersionUtility.TryGetCompatibleItems(_project.FrameworkName, items, out compatibleItems))
            {
                return compatibleItems;
            }

            return Enumerable.Empty<T>();
        }

        public override string ToString()
        {
            return string.Format("Filename: {0}", _filename);
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore;
using FubuCore.Descriptions;
using FubuCore.Logging;
using ripple.Model;

namespace ripple.Nuget
{
    public class LocalDependencies : DescribesItself, LogTopic
    {
        private readonly IEnumerable<INugetFile> _dependencies;

        public LocalDependencies(IEnumerable<INugetFile> dependencies)
        {
            _dependencies = dependencies;
        }

        public bool Any()
        {
            return _dependencies.Any();
        }

        public INugetFile Get(Dependency dependency)
        {
            return Get(dependency.Name);
        }

        public INugetFile Get(string name)
        {
            try
            {
                var nuget = _dependencies.SingleOrDefault(x => x.Name.EqualsIgnoreCase(name));
                if (nuget == null)
                {
                    RippleLog.DebugMessage(this);
                    throw new ArgumentOutOfRangeException("name", "Could not find " + name);
                }

                return nuget;
            }
            catch (InvalidOperationException)
            {
                var dependencies = _dependencies.Where(x => x.Name.EqualsIgnoreCase(name)).Select(x => x.FileName).Join("\n");
                RippleLog.Info("Found multiple copies of {0}: {1}".ToFormat(name, dependencies));

                throw;
            }
        }

        public bool ShouldRestore(Dependency dependency, bool force = false)
        {
            if (!Has(dependency))
            {
                return true;
            }

            var local = Get(dependency);
            if (dependency.Version.IsNotEmpty())
            {
                return local.Version < dependency.SemanticVersion();
            }

            if (!force) return false;
            if (dependency.IsFloat()) return true;

            return local.Version != dependency.SemanticVersion();
        }

        public bool Has(Dependency dependency)
        {
            return Has(dependency.Name);
        }

        public bool Has(string name)
        {
            return _dependencies.Any(x => x.Name.EqualsIgnoreCase(name));
        }

        public IEnumerable<INugetFile> All()
        {
            return _dependencies;
        }

        public bool HasLockedFiles(Solution solution)
        {
            return _dependencies.Any(dependency =>
            {
                var folder = dependency.NugetFolder(solution);
                var assemblySet = new FileSet { Include = "*.dll" };

                var files = new FileSystem().FindFiles(folder, assemblySet);
                return files.Any(file =>
                {
                    try
                    {
                        using (var read = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.None))
                        {
                        }
                        return false;
                    }
                    catch
                    {

                        return true;
                    }
                });
            });
        }

        public void Describe(Description description)
        {
            description.AddList("Items", _dependencies);
        }
    }
}
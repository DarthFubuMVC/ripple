using System;
using System.Collections.Generic;
using System.Linq;
using FubuCore.Util;
using NuGet;
using ripple.Model;

namespace ripple.MSBuild
{
    public class ReferenceAttacher
    {
        private readonly Solution _solution;
        private readonly LocalPackageRepository _repository;
        private readonly Cache<string, IPackage> _packages;
        private readonly Cache<string, string> _toolsVersionMatch = new Cache<string, string>();

        public ReferenceAttacher(Solution solution)
        {
            _solution = solution;
            _repository = new LocalPackageRepository(_solution.PackagesDirectory());

            _packages = new Cache<string, IPackage>(name =>
            {
                try
                {
                    return _repository.FindPackage(name);
                }
                catch (Exception)
                {
                    return null;
                }
            });

            _toolsVersionMatch["4.0"] = "net40";
        }

        public void Attach()
        {
            _solution.Projects.Each(fixProject);
        }

        private void fixProject(Project project)
        {
            var assembliesDico = new Dictionary<string, IPackageAssemblyReference>();
            project.Dependencies.Each(dep =>
            {
                var package = _packages[dep.Name];
                if (package == null)
                {
                    RippleLog.Debug("Could not find the IPackage for " + dep.Name);
                    return;
                }

                var assemblies = package.AssemblyReferences;
                if (assemblies == null) return;

                var explicitRefs = package.PackageAssemblyReferences;
                if (explicitRefs.Any())
                {
                    project.Proj.AddAssemblies(dep, explicitRefs, assemblies);
                    return;
                }
                project.Proj.AddAssemblies(dep, assemblies.Where(c => !assembliesDico.ContainsKey(c.Name)));
                assemblies.Each(a =>
                    {
                        if (!assembliesDico.ContainsKey(a.Name))
                            assembliesDico.Add(a.Name, a);
                    });
            });
        }
    }
}
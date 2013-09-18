using System.Collections.Generic;
using System.Linq;
using NuGet;
using ripple.Commands;
using ripple.Local;
using ripple.Model;

namespace ripple.Steps
{
    public class UpdateNuspecs : RippleStep<CreatePackagesInput>
    {
        protected override void execute(CreatePackagesInput input, IRippleStepRunner runner)
        {
            if (!input.UpdateDependenciesFlag)
            {
                return;
            }

            var groups = input.SpecsFor(Solution).ToArray();
            groups.Each(x => updateSpecification(input, x, groups));
        }

        private void updateSpecification(CreatePackagesInput input, SpecGroup group, IEnumerable<SpecGroup> groups)
        {
            var spec = group.Spec;
            var local = Solution.LocalDependencies();
            var nuspec = new NuspecDocument(spec.Filename);
            
            if(input.OverrideDependenciesFlag)
                nuspec.ClearAllDependencies();

            nuspec.ClearAllDependencies();

            group
                .DetermineDependencies()
                .Each(dependency =>
                {
                    var localDependency = local.Get(dependency);
                    var constraint = Solution.ConstraintFor(dependency);
                    var version = constraint.SpecFor(localDependency.Version);

                    var nuspecDep = new NuspecDependency(dependency.Name, version);

					RippleLog.Info("Adding dependency: " + nuspecDep);

                    nuspec.AddDependency(nuspecDep);
                });

            group
                .Projects
                .SelectMany(project => project.References)
                .Each(projectRef =>
                {
                    var target = groups.FirstOrDefault(x => x.Projects.Contains(projectRef));
                    if (target == null) return;

                    // TODO -- Do we need another setting for project references?
                    var constraint = Solution.NuspecSettings.Float;
                    var version = constraint.SpecFor(new SemanticVersion(input.VersionFlag));

                    var nuspecDep = new NuspecDependency(target.Spec.Name, version);

					RippleLog.Info("Adding dependency: " + nuspecDep);

                    nuspec.AddDependency(nuspecDep);
                });



            nuspec.SaveChanges();
        }
    }
}
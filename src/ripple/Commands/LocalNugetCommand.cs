using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FubuCore;
using FubuCore.CommandLine;
using ripple.Local;
using ripple.Model;
using ripple.Steps;

namespace ripple.Commands
{
    public class CreatePackagesInput : SolutionInput
    {
        public CreatePackagesInput()
        {
            VersionFlag = "0.0.0.0";
            DestinationFlag = RippleFileSystem.LocalNugetDirectory();
        }

        [Description("Override the version of the nuget file")]
        public string VersionFlag { get; set; }

        [Description("Specify where the nuget file should be written, otherwise it just goes to the nuget default")]
        public string DestinationFlag { get; set; }

        [Description("Modify the nuspec files to match the current dependency version constraints")]
        [FlagAlias("update-dependencies", 'u')]
        public bool UpdateDependenciesFlag { get; set; }

        [Description("Create also symbols packages")]
        [FlagAlias("symbols")]
        public bool CreateSymbolsFlag { get; set; }

        public IEnumerable<SpecGroup> SpecsFor(Solution solution)
        {
            if (!UpdateDependenciesFlag)
            {
                return new SpecGroup[0];
            }

            var specs = new List<ProjectNuspec>();
            solution.EachProject(project =>
            {
                solution.Specifications.Each(spec =>
                {
                    if (spec.Name.EqualsIgnoreCase(project.Name))
                    {
                        specs.Add(new ProjectNuspec(project, spec));
                    }
                });
            });

            specs.AddRange(solution.Nuspecs.Select(x => x.ToSpec(solution)));

            return specs
                .GroupBy(x => x.Spec)
                .Select(x => new SpecGroup(x.Key, x.Select(y => y.Project)));
        }
    }

    [CommandDescription("Creates the nuget files locally", Name = "local-nuget")]
    public class LocalNugetCommand : FubuCommand<CreatePackagesInput>
    {
        public override bool Execute(CreatePackagesInput input)
        {
            // TODO -- Kill off this command completely and transition to the new alias
            return new CreatePackagesCommand().Execute(input);
        }
    }

    [CommandDescription("Creates the nuget files locally", Name = "create-packages")]
    public class CreatePackagesCommand : FubuCommand<CreatePackagesInput>
    {
        public override bool Execute(CreatePackagesInput input)
        {
            return RippleOperation
                .For(input)
                .Step<UpdateNuspecs>()
                .Step<CreatePackages>()
                .Execute();
        }
    }
}
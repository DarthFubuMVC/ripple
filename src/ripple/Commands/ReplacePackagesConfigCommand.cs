using FubuCore.CommandLine;

namespace ripple.Commands
{
    [CommandDescription("Replaces all references to packages.config with ripple.dependencies.config", Name = "replace-packages-config")]
    public class ReplacePackagesConfigCommand : FubuCommand<SolutionInput>
    {
        public override bool Execute(SolutionInput input)
        {
            input.EachSolution(solution =>
            {
                var save = false;

                solution.EachProject(project =>
                {
                    if (project.Proj.UsesPackagesConfig())
                    {
                        project.Proj.ConvertToRippleDependenciesConfig();
                        save = true;
                    }
                });

                if (save)
                {
                    solution.Save(true);
                }
            });

            return true;
        }
    }
}
using FubuCore;
using FubuCore.CommandLine;
using ripple.Model;

namespace ripple.Commands
{
    [CommandDescription("Initialize a new ripple solution")]
    public class InitCommand : FubuCommand<InitInput>
    {
        public const string ExistingSolution = "Cannot initialize an existing ripplized solution. I found a ripple.config at: {0}";

        public override bool Execute(InitInput input)
        {
	        var rippleConfigDirectory = RippleFileSystem.FindSolutionDirectory(false);
	        if (rippleConfigDirectory.IsNotEmpty())
            {
                RippleAssert.Fail(ExistingSolution.ToFormat(rippleConfigDirectory));
                return false;
            }

            var builder = NewSolutionBuilder(input.ToSolution());
            var solution = builder.Build();
            solution.Save(true);

            return true;
        }

        public static ISolutionBuilder NewSolutionBuilder(Solution solution)
        {
            var loader = new InMemorySolutionLoader(solution);
            var files = SolutionFiles.FromDirectory(RippleFileSystem.CurrentDirectory(), loader);

            return new SolutionBuilder(files, ProjectReader.Basic());
        }
    }
}
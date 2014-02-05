using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;
using FubuCore;
using FubuCore.CommandLine;
using ripple.Model;
using ripple.Steps;

namespace ripple.Commands
{
    public class RestoreInput : RippleInput, IOverrideFeeds
    {
        [Description("Additional NuGet feed urls separated by '#'")]
        [FlagAlias("feeds", 'F')]
        public string FeedsFlag { get; set; }

        [Description("Forces the restoration to correct any version mismatches")]
        [FlagAlias("force", 'f')]
        public bool ForceFlag { get; set; }

        [Description("Forces project references to be updated to match the defined dependencies.")]
        [FlagAlias("fix-references", 'r')]
        public bool FixReferencesFlag { get; set; }

        [Description("Invokes the fix command after the restore operation has completed.")]
        [FlagAlias("fix-solution", 's')]
        public bool FixSolutionFlag { get; set; }

        public override string DescribePlan(Solution solution)
        {
            return "Restoring dependencies for solution {0} to {1}".ToFormat(solution.Name, solution.PackagesDirectory());
        }

        public override void ApplyTo(Solution solution)
        {
            if (ForceFlag)
            {
                solution.ForceRestore();
            }
        }

        public IEnumerable<Feed> Feeds()
        {
            return FeedsFlag.GetFeeds();
        }
    }

    [CommandDescription("Restores nugets for the solution")]
    public class RestoreCommand : FubuCommand<RestoreInput>
    {
        public const string BatchFile = "ripple-install.txt";

        public override bool Execute(RestoreInput input)
        {
            var operation = RippleOperation
              .For(input)
              .Step<DownloadMissingNugets>()
              .Step<ExplodeSourceOnlyPackages>()
              .Step<ExplodeDownloadedNugets>()
              .Step<ProcessDirectives>();

            if (input.FixReferencesFlag)
            {
                operation.Step<FixReferences>();
            }

            var success = operation.Execute();

            if (!success)
            {
                return false;
            }

            var file = RippleFileSystem.CurrentDirectory().AppendPath(BatchFile);
            if (File.Exists(file))
            {
                success = new BatchInstallCommand().Execute(new BatchInstallInput
                {
                    FileFlag = file
                });
            }

            if (input.FixSolutionFlag && success)
            {
                return new FixCommand().Execute(new FixInput());
            }

            return success;
        }
    }
}
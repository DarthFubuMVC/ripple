using System;
using ripple.Local;
using ripple.Runners;

namespace ripple.Model
{
    public static class BranchDetector
    {
        public static Func<string> ProvideBranchName = () =>
        {
            var startInfo = Runner.Git.Info("symbolic-ref HEAD");
            var returnValue = new ProcessRunner().Run(startInfo, x => { });
            if (returnValue.ExitCode != 0)
            {
                RippleAssert.Fail("Cannot use branch detection when not in a git repository");
            }
            var output = returnValue.OutputText;

            return output.Substring(output.LastIndexOf('/') + 1)
                .Replace("\n", string.Empty);
        };

        public static string GetBranch()
        {
            return ProvideBranchName();
        }
    }
}
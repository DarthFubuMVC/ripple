using System;
using System.Diagnostics;

namespace ripple.Model
{
    public static class BranchDetector
    {
        public static Func<string> GetBranchHelper = () =>
        {
            var startInfo = new ProcessStartInfo(@"run-git.cmd", "symbolic-ref HEAD")
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process {StartInfo = startInfo, EnableRaisingEvents = false})
            {
                process.Start();

                var standardOutput = process.StandardOutput;
                process.WaitForExit();

                var cmdResult = standardOutput.ReadToEnd();
                return cmdResult.Substring(cmdResult.LastIndexOf('/') + 1)
                    .Replace("\n", string.Empty);
            }
        };

        public static string GetBranch()
        {
            return GetBranchHelper();
        }
    }
}
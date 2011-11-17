using System;
using System.ComponentModel;
using FubuCore;
using FubuCore.CommandLine;
using System.Collections.Generic;
using ripple.Model;
using ripple.Nuget;
using System.Linq;

namespace ripple.Commands
{
    public class RestoreInput : SolutionInput
    {
        [Description("Additional NuGet feed urls separated by '#'")]
        public string FeedsFlag { get; set; }
    }

    [CommandDescription("Interacts with nuget to restore all the nuget dependencies in a solution tree")]
    public class RestoreCommand : FubuCommand<RestoreInput>
    {
        private readonly IFileSystem _fileSystem = new FileSystem();

        public override bool Execute(RestoreInput input)
        {
            input.FindSolutions().Each(s => restoreSolution(s, input));

            return true;
        }

        private void restoreSolution(Solution solution, RestoreInput input)
        {
            Console.WriteLine("Restoring nuget dependencies for solution " + solution.Name);

            var packagesFolder = solution.PackagesFolder();
            _fileSystem.CreateDirectory(packagesFolder);

            var feeds = input.FeedsFlag.ParseFeeds();
            var nugetService = new NugetService(solution, feeds);
            
            solution.GetAllNugetDependencies().OrderBy(x => x.Name).Each(nugetService.Install);
        }
    }

    // Remove when it is possible to have IEnumerable<string> flag
    public static class StringFeeds
    {
        public static IEnumerable<string> ParseFeeds(this string urlString)
        {
            return urlString.IsNotEmpty() 
                ? urlString.ToDelimitedArray('#') 
                : Enumerable.Empty<string>();
        }
    }
}
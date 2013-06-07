using System;
using System.ComponentModel;
using FubuCore;
using FubuCore.CommandLine;
using System.Collections.Generic;
using NuGet;
using ripple.Model;

namespace ripple.Commands
{
    public class PublishInput : SolutionInput
    {
        public PublishInput()
        {
            ArtifactsFlag = "artifacts";

            ApiKey = Environment.GetEnvironmentVariable(PublishingService.ApiKey, EnvironmentVariableTarget.User);
            ServerFlag = "https://nuget.org/";
        }

        [Description("Nuget version number")]
        public string Version { get; set; }

        [Description("Api key for the NuGet server")]
        public string ApiKey { get; set; }

        [Description("Overrides the location of the artifacts folder")]
        [FlagAlias("artifacts", 'r')]
        public string ArtifactsFlag { get; set; }

        [Description("Custom url for the NuGet server")]
        [FlagAlias("server", 's')]
        public string ServerFlag { get; set; }

        [Description("Create also symbols packages")]
        [FlagAlias("symbols")]
        public bool CreateSymbolsFlag { get; set; }
    }
    
    [CommandDescription("Builds and pushes all the nuspec files for a solution(s) to nuget.org")]
    public class PublishCommand : FubuCommand<PublishInput>
    {
        public override bool Execute(PublishInput input)
        {
            input.EachSolution(solution =>
            {
                Console.WriteLine("Building nuget files for " + solution.Name);
                var artifactDirectory = solution.Directory.AppendPath(input.ArtifactsFlag);

                Console.WriteLine("Cleaning out any existing nuget files before continuing");
                new FileSystem().CleanDirectory(artifactDirectory);

                solution.Specifications.Each(nuget =>
                {
                    RippleLog.Info("Creating and publishing Nuget for " + nuget.Name);

					var packageFile = solution.Package(nuget, SemanticVersion.Parse(input.Version), artifactDirectory, input.CreateSymbolsFlag);
                    solution.Publisher.PublishPackage(input.ServerFlag, packageFile, input.ApiKey);
                });
            });

            return true;
        }
    }
}
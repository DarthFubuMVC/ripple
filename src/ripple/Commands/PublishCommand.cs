using System;
using System.ComponentModel;
using FubuCore;
using FubuCore.CommandLine;
using System.Collections.Generic;
using ripple.Local;

namespace ripple.Commands
{
    public class PublishInput : SolutionInput
    {
        public PublishInput()
        {
            ArtifactsFlag = "artifacts";
        }

        [Description("Nuget version number")]
        public string Version { get; set; }

        [Description("Api key for the NuGet server")]
        public string ApiKey { get; set; }

        [Description("Overrides the location of the artifacts folder")]
        public string ArtifactsFlag { get; set; }

        [Description("Custom url for the NuGet server")]
        [FlagAlias("s")]
        public string ServerFlag { get; set; }
    }
    
    [CommandDescription("Builds and pushes all the nuspec files for a solution(s) to nuget.org")]
    public class PublishCommand : FubuCommand<PublishInput>
    {
        public override bool Execute(PublishInput input)
        {
            input.FindSolutions().Each(solution =>
            {
                Console.WriteLine("Building nuget files for " + solution.Name);
                var artifactDirectory = solution.Directory.AppendPath(input.ArtifactsFlag);

                solution.PublishedNugets.Each(nuget =>
                {
                    Console.WriteLine("Creating and publishing Nuget for " + nuget.Name);

                    CLIRunner.RunNuget(BuildPack(nuget, input, artifactDirectory));
                    CLIRunner.RunNuget(BuildPush(nuget, input, artifactDirectory));
                });
            });

            return true;
        }

        public string BuildPack(NugetSpec nuget, PublishInput input, string artifactDirectory)
        {
            var cmd = new List<string>
            {
                "pack {0}".ToFormat(nuget.Filename.FileEscape()),
                "-version {0}".ToFormat(input.Version),
                "-o {0}".ToFormat(artifactDirectory.FileEscape())
            };

            return cmd.Join(" ");
        }

        public string BuildPush(NugetSpec nuget, PublishInput input, string artifactDirectory)
        {
            var nupkgFileName = "{0}.{1}.nupkg".ToFormat(nuget.Name, input.Version);
            var nupkgPath = artifactDirectory.AppendPath(nupkgFileName).FileEscape();

            var cmd = new List<string>
            {
                "push {0} {1}".ToFormat(nupkgPath, input.ApiKey)
            };

            if(input.ServerFlag.IsNotEmpty())
            {
                cmd.Add("-s {0}".ToFormat(input.ServerFlag));
            }

            return cmd.Join(" ");
        }
    }
}
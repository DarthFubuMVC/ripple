using System.Collections.Generic;
using System.Linq;
using FubuCore;
using FubuCore.CommandLine;
using NuGet;
using ripple.Commands;
using ripple.Model;

namespace ripple.Steps
{
    public class CreatePackages : RippleStep<CreatePackagesInput>
    {
        protected override void execute(CreatePackagesInput input, IRippleStepRunner runner)
        {
            new FileSystem().CreateDirectory(input.DestinationFlag);

            Solution.Specifications.Each(spec => {
                var version = input.VersionFlag;

                RippleLog.Info("Building the nuget spec file at " + spec.Filename + " as version " + version);

                Solution.Package(new PackageParams(spec, SemanticVersion.Parse(version), input.DestinationFlag, input.CreateSymbolsFlag));
                ConsoleWriter.PrintHorizontalLine();
            });
        }
    }
}
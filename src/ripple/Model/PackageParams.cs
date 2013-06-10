using NuGet;
using ripple.Local;

namespace ripple.Model
{
    public class PackageParams
    {
        public readonly NugetSpec Spec;
        public readonly SemanticVersion Version;
        public readonly string OutputPath;
        public readonly bool CreateSymbols;

        public PackageParams(NugetSpec spec, SemanticVersion version, string outputPath, bool createSymbols)
        {
            Spec = spec;
            Version = version;
            OutputPath = outputPath;
            CreateSymbols = createSymbols;
        }
    }
}
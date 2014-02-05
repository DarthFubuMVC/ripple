using NuGet;
using ripple.Model;

namespace ripple.Nuget
{
    public interface INugetFile
    {
		string FileName { get; }
        string Name { get; }
        SemanticVersion Version { get; }
        bool IsPreRelease { get; }
        IPackage ExplodeTo(string directory);

	    INugetFile CopyTo(string directory);

	    string NugetFolder(Solution solution);
        void ExplodeSourcesTo(string directory);
        bool IsSourceOnlyPackage();
    }
}
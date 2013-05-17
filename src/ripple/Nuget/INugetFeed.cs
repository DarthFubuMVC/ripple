using NuGet;
using ripple.Model;

namespace ripple.Nuget
{
    public interface INugetFeed
    {
        IRemoteNuget Find(Dependency query);
        IRemoteNuget FindLatest(Dependency query);

        IPackageRepository Repository { get; }
    }
}
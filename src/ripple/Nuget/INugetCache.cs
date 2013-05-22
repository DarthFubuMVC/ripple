using System.Collections.Generic;
using ripple.Model;

namespace ripple.Nuget
{
    public interface INugetCache
    {
	    void Update(IRemoteNuget nuget);
        void UpdateAll(IEnumerable<IRemoteNuget> nugets);
		INugetFile Latest(Dependency query);

        void Flush();

        INugetFile Find(Dependency query);

	    IRemoteNuget Retrieve(IRemoteNuget nuget);

        Feed ToFeed();
        string LocalPath { get; }
    }

	public class NulloNugetCache : INugetCache
	{
		public void Update(IRemoteNuget nuget)
		{
		}

		public void UpdateAll(IEnumerable<IRemoteNuget> nugets)
		{
		}

		public INugetFile Latest(Dependency query)
		{
			return null;
		}

		public void Flush()
		{
		}

	    public INugetFile Find(Dependency query)
		{
			return null;
		}

		public IRemoteNuget Retrieve(IRemoteNuget nuget)
		{
			return nuget;
		}

	    public Feed ToFeed()
	    {
	        return null;
	    }

	    public string LocalPath { get { return null; } }
	}
}
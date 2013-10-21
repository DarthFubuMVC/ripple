using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using FubuCore;
using FubuCore.Descriptions;
using ripple.Nuget;

namespace ripple.Model
{
	public class Feed : DescribesItself
	{
		public static readonly Feed NuGetV1 = new Feed("http://packages.nuget.org/v1/FeedService.svc/");
		public static readonly Feed NuGetV2 = new Feed("http://nuget.org/api/v2");
		public static readonly Feed Fubu = new Feed("http://build.fubu-project.org/guestAuth/app/nuget/v1/FeedService.svc", UpdateMode.Float);

		public Feed()
		{
            Stability = NugetStability.ReleasedOnly;
		}

		public Feed(string url)
			: this(url, UpdateMode.Fixed)
		{
		}

		public Feed(string url, UpdateMode mode)
            : this(url, mode, NugetStability.ReleasedOnly)
		{
		}

	    public Feed(string url, UpdateMode mode, NugetStability stability)
	    {
            Url = url.TrimEnd('/');
            Mode = mode;
	        Stability = stability;
	    }

	    [XmlAttribute]
		public string Url { get; set; }
		[XmlAttribute]
		public UpdateMode Mode { get; set; }
        [XmlAttribute]
        public NugetStability Stability { get; set; }
        [XmlAttribute]
        public string Password { get; set; }
        [XmlAttribute]
        public string Username { get; set; }

		public INugetFeed GetNugetFeed()
		{
			return FeedRegistry.FeedFor(this);
		}

		public IRemoteNuget Find(Dependency query)
		{
			var feed = GetNugetFeed();
			if (Mode == UpdateMode.Float || query.IsFloat())
			{
				return feed.FindLatest(query);
			}

			return feed.Find(query);
		}

		protected bool Equals(Feed other)
		{
			return string.Equals(Url, other.Url);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((Feed) obj);
		}

		public override int GetHashCode()
		{
			return Url.GetHashCode();
		}

        public override string ToString()
        {
            return "{0} ({1})".ToFormat(Url, Mode);
        }

		public void Describe(Description description)
		{
			description.ShortDescription = "{0} ({1})".ToFormat(Url, Mode);
		}

		public static IEnumerable<Feed> Defaults()
		{
			yield return Fubu;
			yield return NuGetV2;
			yield return NuGetV1;
		}

		public static Feed FindOrCreate(string url)
		{
			var existing = Defaults().SingleOrDefault(x => x.Url.EqualsIgnoreCase(url));
			if (existing != null)
			{
				return existing;
			}

			return new Feed(url);
		}

        public static Feed FromPath(string path)
        {
            return new Feed("file://" + path.ToFullPath().Replace("\\", "/"));
        }
	}
}
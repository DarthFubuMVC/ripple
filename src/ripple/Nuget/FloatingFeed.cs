using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using ripple.Model;

namespace ripple.Nuget
{
    public class FloatingFeed : NugetFeed, IFloatingFeed
    {
        public string FindAllLatestCommand
        {
            get
            {
                var template = "/Packages()?$filter={0}&$orderby=DownloadCount%20desc,Id&$skip=0&$top=1000";

                if (_stability == NugetStability.ReleasedOnly)
                    return string.Format(template, "IsLatestVersion");

                return string.Format(template, "IsAbsoluteLatestVersion");
            }
        }
            

		private readonly Lazy<XmlDocument> _feed;
        private bool _dumped;

        public FloatingFeed(string url, NugetStability stability) 
            : base(url, stability)
        {
            _feed = new Lazy<XmlDocument>(loadLatestFeed);
        }

        private XmlDocument loadLatestFeed()
        {
            var url = Url + FindAllLatestCommand;
            RippleLog.Debug("Retrieving latest from " + url);
            
            var client = new WebClient();
            var text = client.DownloadString(url);

            var document = new XmlDocument();
            document.LoadXml(text);

            return document;
        }

        public IEnumerable<IRemoteNuget> GetLatest()
        {
            var feed = new NugetXmlFeed(_feed.Value);
            return feed.ReadAll(this);
        }

        public void DumpLatest()
        {
            lock (this)
            {
                if (_dumped) return;

                var latest = GetLatest();
                var topic = new LatestNugets(latest, Url);

                RippleLog.DebugMessage(topic);
                _dumped = true;
            }
        }

        public override IRemoteNuget FindLatestByName(string name)
        {
            return findLatest(new Dependency(name));
        }

        protected override IRemoteNuget findLatest(Dependency query)
        {
            var floatedResult = GetLatest().SingleOrDefault(x => query.MatchesName(x.Name));
            RippleLog.Debug("Looking for " + query + " in " + Url + "; Found " + floatedResult);
            if (floatedResult != null && query.Mode == UpdateMode.Fixed && floatedResult.IsUpdateFor(query))
            {
                return null;
            }

            return floatedResult;
        }
    }
}
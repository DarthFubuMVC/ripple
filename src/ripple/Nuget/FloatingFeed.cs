using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using FubuCore;
using ripple.Model;

namespace ripple.Nuget
{
    public class FloatingFeed : NugetFeed, IFloatingFeed
    {
        public string FindAllLatestCommand
        {
            get
            {
                if(Stability == NugetStability.ReleasedOnly)
                    return "/Packages()?$filter=IsLatestVersion&$orderby=DownloadCount";

                return "/Packages()?$filter=IsAbsoluteLatestVersion&$orderby=DownloadCount";
            }
        }

        public string FindLatestCommand
        {
            get
            {
                if (Stability == NugetStability.ReleasedOnly)
                    return "/Packages()?$filter=(Id eq '{0}') and IsLatestVersion";

                return "/Packages()?$filter=(Id eq '{0}') and IsAbsoluteLatestVersion";
            }
        }
            

        private bool _dumped;
        private readonly Lazy<IEnumerable<IRemoteNuget>> _latest; 

        public FloatingFeed(string url, NugetStability stability) 
            : base(url, stability)
        {
            _latest = new Lazy<IEnumerable<IRemoteNuget>>(getLatest);
        }

        private IEnumerable<IRemoteNuget> loadLatestFeed(int page)
        {
            var toSkip = (page - 1) * 100;
            var url = Url + FindAllLatestCommand.ToFormat(toSkip);
            RippleLog.Debug("Retrieving latest from " + url);
            
            var client = new WebClient();
            var text = client.DownloadString(url);

            var document = new XmlDocument();
            document.LoadXml(text);
            var ns = new XmlNamespaceManager(document.NameTable);
            ns.AddNamespace("atom", "http://www.w3.org/2005/Atom");
            document.SelectSingleNode("//atom:feed", ns);
            return new NugetXmlFeed(document).ReadAll(this).ToArray();
        }

        public IEnumerable<IRemoteNuget> GetLatest()
        {
            return _latest.Value;
        }

        private IEnumerable<IRemoteNuget> getLatest()
        {
            var all = new List<IRemoteNuget>();

            const string atomXmlNamspace = "http://www.w3.org/2005/Atom";
            var url = Url + FindAllLatestCommand;
            var client = new WebClient();

            XmlNode nextNode;
            do
            {
                RippleLog.Debug("Retrieving latest from " + url);

                var text = client.DownloadString(url);
                var document = new XmlDocument();
                document.LoadXml(text);
                all.AddRange(new NugetXmlFeed(document).ReadAll(this));

                var ns = new XmlNamespaceManager(document.NameTable);
                ns.AddNamespace("atom", atomXmlNamspace);
                nextNode = document.SelectSingleNode("//atom:link[@rel='next']", ns);

                if (nextNode != null && nextNode.Attributes != null && nextNode.Attributes["href"] != null)
                {
                    url = nextNode.Attributes["href"].Value;
                }
                else
                {
                    url = string.Empty;
                }

            } while (!string.IsNullOrEmpty(url));

            return all;
        }

        public void DumpLatest()
        {
        }

        public override IRemoteNuget FindLatestByName(string name)
        {
            return findLatest(new Dependency(name));
        }

        protected override IRemoteNuget findLatest(Dependency query)
        {
            var client = new WebClient();

            var url = string.Format(Url + FindLatestCommand, query.Name);
            var text = client.DownloadString(url);
            var document = new XmlDocument();
            document.LoadXml(text);

            var feed = (new NugetXmlFeed(document).ReadAll(this));

            var floatedResult = feed.SingleOrDefault(x => query.MatchesName(x.Name));
            if (floatedResult != null && query.Mode == UpdateMode.Fixed && floatedResult.IsUpdateFor(query))
            {
                return null;
            }

            return floatedResult;
        }
    }
}
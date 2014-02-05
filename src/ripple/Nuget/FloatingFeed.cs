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
                    return "/Packages()?$filter=IsLatestVersion&$orderby=DownloadCount%20desc,Id&$skip={0}&$take=100";

                return "/Packages()?$filter=IsAbsoluteLatestVersion&$orderby=DownloadCount%20desc,Id&$skip={0}&$take=100";
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

            return new NugetXmlFeed(document).ReadAll(this).ToArray();
        }

        public IEnumerable<IRemoteNuget> GetLatest()
        {
            return _latest.Value;
        }

        private IEnumerable<IRemoteNuget> getLatest()
        {
            var all = new List<IRemoteNuget>();
            var page = 1;
            var results = loadLatestFeed(page);
            all.AddRange(results);
            while (results.Count() == 100)
            {
                page++;
                results = loadLatestFeed(page);
                all.AddRange(results);
            }

            return all;
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
            var floatedResult = GetLatest().FirstOrDefault(x => query.MatchesName(x.Name));
            RippleLog.Debug("Looking for " + query + " in " + Url + "; Found " + floatedResult);
            if (floatedResult != null && query.Mode == UpdateMode.Fixed && floatedResult.IsUpdateFor(query))
            {
                return null;
            }

            return floatedResult;
        }
    }
}
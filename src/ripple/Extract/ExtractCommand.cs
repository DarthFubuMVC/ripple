using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using FubuCore;
using FubuCore.CommandLine;

namespace ripple.Extract
{
	[CommandDescription("Extracts all the latest nugets from a remote feed ")]
	public class ExtractCommand : FubuCommand<ExtractInput>
	{
		public const string Command =
			"/Packages()?$filter=IsAbsoluteLatestVersion&$orderby=DownloadCount%20desc,Id&$skip=0&$top=";

		public override bool Execute(ExtractInput input)
		{
			var system = new FileSystem();
			system.DeleteDirectory(input.Directory);
			system.CreateDirectory(input.Directory);

			var feeds = loadNugetEntries(input).ToList();

			feeds.Each(feed =>
			{
				feed.DownloadTo(input.Directory);

				Console.WriteLine(feed);
			});

			return true;
		}

		private static readonly XNamespace atomNS = "http://www.w3.org/2005/Atom";

		private static IEnumerable<NugetEntry> loadNugetEntries(ExtractInput input)
		{
			var url = input.FeedFlag.TrimEnd('/') + Command + input.MaxFlag;
			
			var document = XDocument.Load(url);

			return document.Elements(atomNS + "entry").Select(e => new NugetEntry(e));
		}
	}
}
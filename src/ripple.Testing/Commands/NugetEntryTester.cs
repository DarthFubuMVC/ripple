using System.Xml.Linq;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Extract;

namespace ripple.Testing.Commands
{
	[TestFixture]
	public class NugetEntryTester
	{
		private const string entryXml = @"<entry xml:base=""http://nuget.org/api/v2/"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://www.w3.org/2005/Atom"">
<id>http://nuget.org/api/v2/Packages(Id='Ripple',Version='2.0.0.92')</id>
<title type=""text"">Ripple</title>
<summary type=""text""></summary>
<updated>2013-05-29T11:20:58Z</updated>
<author>
<name>Josh Arnold,  Jeremy D. Miller</name>
</author>
<link rel=""edit-media"" title=""V2FeedPackage"" href=""Packages(Id='Ripple',Version='2.0.0.92')/$value"" />
<link rel=""edit"" title=""V2FeedPackage"" href=""Packages(Id='Ripple',Version='2.0.0.92')"" />
<category term=""NuGetGallery.V2FeedPackage"" scheme=""http://schemas.microsoft.com/ado/2007/08/dataservices/scheme"" />
<content type=""application/zip"" src=""http://nuget.org/api/v2/package/Ripple/2.0.0.92"" />
<m:properties xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"">
<d:Version>2.0.0.92</d:Version>
<d:Copyright m:null=""true""></d:Copyright>
<d:Created m:type=""Edm.DateTime"">2013-05-20T14:05:51.817</d:Created>
<d:Dependencies></d:Dependencies>
<d:Description>Improved cross solution dependency management using Nuget</d:Description>
<d:DownloadCount m:type=""Edm.Int32"">24</d:DownloadCount>
<d:GalleryDetailsUrl>http://nuget.org/packages/Ripple/2.0.0.92</d:GalleryDetailsUrl>
<d:IconUrl>https://github.com/DarthFubuMVC/fubu-collateral/raw/master/Icons/FubuMVC_256.png</d:IconUrl>
<d:IsLatestVersion m:type=""Edm.Boolean"">true</d:IsLatestVersion>
<d:IsAbsoluteLatestVersion m:type=""Edm.Boolean"">true</d:IsAbsoluteLatestVersion>
<d:IsPrerelease m:type=""Edm.Boolean"">false</d:IsPrerelease>
<d:Language m:null=""true""></d:Language>
<d:Published m:type=""Edm.DateTime"">2013-05-20T14:05:51.817</d:Published>
<d:LicenseUrl>https://github.com/DarthFubuMVC/fubumvc/raw/master/license.txt</d:LicenseUrl>
<d:PackageHash>Wp9j/jkpHpsFzxJ/MRVaESCe39xmyg9SvqQjXgL+S76HlN7EZp0cqgQSzHeYhWQ66aoX5gbUrBl52lBTU4C3sQ==</d:PackageHash>
<d:PackageHashAlgorithm>SHA512</d:PackageHashAlgorithm>
<d:PackageSize m:type=""Edm.Int64"">512990</d:PackageSize>
<d:ProjectUrl>http://fubu-project.org/</d:ProjectUrl>
<d:ReportAbuseUrl>http://nuget.org/package/ReportAbuse/Ripple/2.0.0.92</d:ReportAbuseUrl>
<d:ReleaseNotes m:null=""true""></d:ReleaseNotes>
<d:RequireLicenseAcceptance m:type=""Edm.Boolean"">false</d:RequireLicenseAcceptance>
<d:Tags>nuget</d:Tags>
<d:Title m:null=""true""></d:Title>
<d:VersionDownloadCount m:type=""Edm.Int32"">24</d:VersionDownloadCount>
<d:MinClientVersion m:null=""true""></d:MinClientVersion>
<d:Summary m:null=""true""></d:Summary>
</m:properties>
</entry>";

		[Test]
		public void should_parse_nuget_entry_xml()
		{
			var document = XDocument.Parse(entryXml);
			var result = new NugetEntry(document.Root);

			result.Version.ShouldEqual("2.0.0.92");
			result.Name.ShouldEqual("Ripple");
			result.Url.ShouldContain("Ripple");
			result.Url.ShouldContain("2.0.0.92");
		}

		private const string badEntryXml = @"<entry xml:base=""http://nuget.org/api/v2/"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"" xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns=""http://www.w3.org/2005/Atom"">
<id>http://nuget.org/api/v2/Packages(Id='Ripple',Version='2.0.0.92')</id>
<summary type=""text""></summary>
<content type=""application/zip"" src=""http://nuget.org/api/v2/package/Ripple/2.0.0.92"" />
<m:properties xmlns:m=""http://schemas.microsoft.com/ado/2007/08/dataservices/metadata"" xmlns:d=""http://schemas.microsoft.com/ado/2007/08/dataservices"">
<d:Version>2.0.0.92</d:Version>
</m:properties>
</entry>";

		[Test]
		public void should_thrown_when_title_or_content_url_is_missing()
		{
			var document = XDocument.Parse(badEntryXml);

			typeof (RippleFatalError).ShouldBeThrownBy(() =>
			{
				new NugetEntry(document.Root);
			});
		}

	}
}
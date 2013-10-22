using System.Linq;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.MSBuild;

namespace ripple.Testing.MSBuild
{
	[TestFixture]
	public class removing_duplicate_assembly_references
	{
		private ProjFile theProj;
		private string theFilename;

		[SetUp]
		public void SetUp()
		{
			theFilename = "Bottles.txt";
			var stream = GetType()
				.Assembly
				.GetManifestResourceStream(GetType(), "ProjectTemplate.txt");

			new FileSystem().WriteStreamToFile(theFilename, stream);

			theProj = new ProjFile(theFilename, null);

			theProj
				.References
				.ShouldHaveTheSameElementKeysAs(new[]
				{
                    "yeti",
					"FubuCore", 
					"Ionic.Zip", 
					"Ionic.Zip, Version=1.9.1.8, Culture=neutral, processorArchitecture=MSIL",
					"System",
					"System.Core",
					"System.Web",
					"System.Xml"
				}, x => x.Name);

			theProj.RemoveDuplicateReferences();
			theProj.Write();

			theProj = null;
			theProj = new ProjFile(theFilename, null);
		}

		[Test]
		public void removes_by_matching_on_just_the_assembly_name()
		{
			theProj.FindReferenceNodes().Count().ShouldNotEqual(0);

			theProj
				.References
				.ShouldHaveTheSameElementKeysAs(new[]
				{
					"FubuCore", 
					"Ionic.Zip",
					"System",
					"System.Core",
					"System.Web",
					"System.Xml",
                    "yeti",
				}, x => x.Name);
		}
	}

	[TestFixture]
	public class removing_duplicate_assembly_references_2
	{
		private ProjFile theProj;
		private string theFilename;

		[SetUp]
		public void SetUp()
		{
			theFilename = "Test.txt";
			var stream = GetType()
				.Assembly
				.GetManifestResourceStream(GetType(), "ProjectWithDuplicateRefs.txt");

			new FileSystem().WriteStreamToFile(theFilename, stream);

			theProj = new ProjFile(theFilename, null);

			theProj
				.References
				.ShouldHaveTheSameElementKeysAs(new[]
				{
					"Bottles", 
					"FubuCore",
					"FubuLocalization",
					"FubuMVC.Core",
					"FubuMVC.Core.Assets",
					"FubuMVC.Core.UI",
					"FubuMVC.Core.View",
					"FubuMVC.JQueryUI",
					"FubuMVC.Media",
					"FubuMVC.StructureMap",
					"FubuTestingSupport",
					"HtmlTags",
					"Microsoft.CSharp",
					"nunit.framework",
					"nunit.framework, Version=2.5.10.11092, Culture=neutral, PublicKeyToken=96d09a1eb7f44a77, processorArchitecture=MSIL",
					"nunit.mocks",
					"nunit.mocks, Version=2.5.10.11092, Culture=neutral, PublicKeyToken=96d09a1eb7f44a77, processorArchitecture=MSIL",
					"pnunit.framework",
					"pnunit.framework, Version=1.0.4109.34242, Culture=neutral, processorArchitecture=MSIL",
					"Rhino.Mocks",
					"Rhino.Mocks, Version=3.6.0.0, Culture=neutral, PublicKeyToken=0b3305902db7183f, processorArchitecture=MSIL",
					"StructureMap",
					"StructureMap.AutoMocking",
					"StructureMap.AutoMocking, Version=2.6.3.0, Culture=neutral, PublicKeyToken=e60ad81abae3c223, processorArchitecture=MSIL",
					"System",
					"System.Core",
					"System.Data",
					"System.Data.DataSetExtensions",
					"System.Xml",
					"System.Xml.Linq"
				}, x => x.Name);

			theProj.RemoveDuplicateReferences();
			theProj.Write();

			theProj = null;
			theProj = new ProjFile(theFilename, null);
		}

		[Test]
		public void removes_by_matching_on_just_the_assembly_name()
		{
			theProj.FindReferenceNodes().Count().ShouldNotEqual(0);

			theProj
				.References
				.ShouldHaveTheSameElementKeysAs(new[]
				{
					"Bottles", 
					"FubuCore",
					"FubuLocalization",
					"FubuMVC.Core",
					"FubuMVC.Core.Assets",
					"FubuMVC.Core.UI",
					"FubuMVC.Core.View",
					"FubuMVC.JQueryUI",
					"FubuMVC.Media",
					"FubuMVC.StructureMap",
					"FubuTestingSupport",
					"HtmlTags",
					"Microsoft.CSharp",
					"nunit.framework",
					"nunit.mocks",
					"pnunit.framework",
					"Rhino.Mocks",
					"StructureMap",
					"StructureMap.AutoMocking",
					"System",
					"System.Core",
					"System.Data",
					"System.Data.DataSetExtensions",
					"System.Xml",
					"System.Xml.Linq"
				}, x => x.Name);
		}
	}
}
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using NuGet;
using ripple.Local;
using ripple.Model;

namespace ripple.Testing.Model
{
    [TestFixture]
    public class PublishingServiceTester
    {
        [Test]
        public void finds_the_rules()
        {
            var rules = PublishingService.Rules;
            rules.Each(x => Debug.WriteLine(x.GetType()));

            rules.Any().ShouldBeTrue();
        }

        [Test]
        public void smoke_test_the_package_validation()
        {
            var theFilename = "fubumvc.core.nuspec";
			var stream = GetType()
					.Assembly
					.GetManifestResourceStream(typeof(DataMother), "FubuMVCNuspecTemplate.txt");

			new FileSystem().WriteStreamToFile(theFilename, stream);

            var spec = NugetSpec.ReadFrom(theFilename);
            var service = new PublishingService(new StubSolutionFiles { RootDir = ".".ToFullPath() });

            Exception<RippleFatalError>.ShouldBeThrownBy(() => service.CreatePackage(spec, new SemanticVersion("1.0.0.0"), ".".ToFullPath(), false));
        }
    }
}
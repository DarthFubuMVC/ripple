using System.Linq;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using NuGet;
using ripple.Model;

namespace ripple.Testing.Integration
{
    [TestFixture(false, Description = "IntegratedPackaging tests without symbol files published")]
    [TestFixture(true, Description = "IntegratedPackaging tests with symbol files published")]
    public class IntegratedPackagingTester
    {
        private readonly bool _publishSymbols;
        private SolutionGraphScenario theScenario;

        private FileSystem theFileSystem;
        private string theNugetDirectory;

        private Solution theSolution;

        public IntegratedPackagingTester(bool publishSymbols)
        {
            _publishSymbols = publishSymbols;
        }

        [TestFixtureSetUp()]
        public void FixtureSetUp()
        {
            theScenario = SolutionGraphScenario.Create(scenario =>
            {
                scenario.Solution("FubuCore", fubucore => fubucore.Publishes("FubuCore"));
            });

            theNugetDirectory = theScenario.Directory.AppendPath("nugets");

            theFileSystem = new FileSystem();
            theFileSystem.CreateDirectory(theNugetDirectory);

            theSolution = theScenario.Find("FubuCore");

            theSolution.Package(theSolution.Specifications.Single(), new SemanticVersion("1.1.1.1"), theNugetDirectory, _publishSymbols);
        }

        [TestFixtureTearDown]
        public void TearDown()
        {
            theScenario.Cleanup();
        }

        [Test]
        public void creates_the_published_file()
        {
            theFileSystem.FileExists(theNugetDirectory, "FubuCore.1.1.1.1.nupkg").ShouldBeTrue();
            Assert.AreEqual(_publishSymbols, theFileSystem.FileExists(theNugetDirectory, "FubuCore.1.1.1.1.symbols.nupkg"));
        }
    }
}
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.MSBuild;

namespace ripple.Testing.MSBuild
{
    [TestFixture]
    public class replacing_reference_to_packages_config
    {
        private ProjFile theProj;
        private string theFilename;

        [Test]
        public void convert_to_ripple_dependencies_config()
        {
            theFilename = "TestProject.txt";
            var stream = GetType()
                .Assembly
                .GetManifestResourceStream(GetType(), "ProjectWithPackagesConfig.txt");

            new FileSystem().WriteStreamToFile(theFilename, stream);

            theProj = new ProjFile(theFilename, null);
            theProj.UsesPackagesConfig().ShouldBeTrue();

            theProj.ConvertToRippleDependenciesConfig();
            theProj.Write();

            theProj = null;
            theProj = new ProjFile(theFilename, null);

            theProj.UsesPackagesConfig().ShouldBeFalse();
        }
    }
}
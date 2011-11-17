using System.IO;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;
using ripple.Local;

namespace ripple.Testing.Commands
{
    [TestFixture]
    public class PublishCommandTester : InteractionContext<PublishCommand>
    {
        private NugetSpec theSpec;
        private PublishInput theInput;

        protected override void beforeEach()
        {
            theSpec = new NugetSpec("Nug", "Nug.nuspec");
            theInput = new PublishInput
            {
                Version = "1.0.0",
                ArtifactsFlag = "arts",
                ApiKey = "pw"
            };
        }

        [Test]
        public void build_pack_creates_correct_cmd()
        {
            ClassUnderTest.BuildPack(theSpec, theInput, theInput.ArtifactsFlag)
                .ShouldEqual("pack \"Nug.nuspec\" -version 1.0.0 -o \"arts\"");
        }

        [Test]
        public void build_push_creates_correct_cmd_with_server_url()
        {
            theInput.ServerFlag = "http://somewhere.net:8080";

            ClassUnderTest.BuildPush(theSpec, theInput, theInput.ArtifactsFlag)
                .ShouldEqual("push \"arts{0}Nug.1.0.0.nupkg\" pw -s {1}".ToFormat(Path.DirectorySeparatorChar, theInput.ServerFlag));

        }

        [Test]
        public void build_push_creates_correct_cmd_without_server_url()
        {
            ClassUnderTest.BuildPush(theSpec, theInput, theInput.ArtifactsFlag)
                .ShouldEqual("push \"arts{0}Nug.1.0.0.nupkg\" pw".ToFormat(Path.DirectorySeparatorChar));

        }
    }
}

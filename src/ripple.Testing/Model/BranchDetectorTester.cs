namespace ripple.Testing.Model
{
    using System;
    using System.IO;
    using NUnit.Framework;
    using ripple.Model;

    [TestFixture]
    public class BranchDetectorTester
    {
        [Test]
        public void Should_use_solution_dir_as_base_dir()
        {
            Assert.True(BranchDetector.CanDetectBranch());
            Assert.IsNotEmpty(BranchDetector.Current());


            BranchDetector.Live();

            //simulate a no git dir
            RippleFileSystem.StubCurrentDirectory(Path.GetTempPath());

            Assert.False(BranchDetector.CanDetectBranch());

            Assert.Throws<RippleFatalError>(() => BranchDetector.Current());

        }

        [TearDown]
        public void TearDown()
        {
            RippleFileSystem.Live();
        }
         
    }
}
namespace ripple.Testing.Model
{
    using System;
    using System.IO;
    using NUnit.Framework;
    using ripple.Model;

    [TestFixture]
    public class BranchDetectorTester
    {
        const string hgBranch = "hgbranch";

        string tempPath = null;

        [SetUp]
        public void SetUp()
        {
            tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(tempPath);

            File.Create(Path.Combine(tempPath, "ripple.config")).Close();

            var hgPath = Path.Combine(tempPath, ".hg");
            Directory.CreateDirectory(hgPath);

            File.WriteAllText(Path.Combine(hgPath, "branch"), hgBranch + Environment.NewLine);
        }

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

            //simulate a hg dir
            RippleFileSystem.StubCurrentDirectory(tempPath);

            Assert.True(BranchDetector.CanDetectBranch());
            Assert.AreEqual(hgBranch, BranchDetector.Current());
        }

        [TearDown]
        public void TearDown()
        {
            RippleFileSystem.Live();
            if (tempPath != null && Directory.Exists(tempPath))
                Directory.Delete(tempPath);
        }
         
    }
}
using System.IO;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;
using ripple.Model;

namespace ripple.Testing
{
    [TestFixture]
    public class from_a_solution_dir
    {
        private string theSolutionDir;
        private string theCurrentDir;
        private string theCodeDir;
	    private FileSystem fileSystem;
	    private string rootDir;

	    [SetUp]
        public void SetUp()
	    {
			rootDir = Path.GetTempPath().AppendRandomPath();
            theCodeDir = rootDir.AppendPath("code");
            theSolutionDir = theCodeDir.AppendPath("ripple");
            theCurrentDir = theSolutionDir.AppendPath("src", "project1");

            fileSystem = new FileSystem();
            fileSystem.CreateDirectory(theCodeDir);
            fileSystem.CreateDirectory(theSolutionDir);
            fileSystem.CreateDirectory(theCurrentDir);

            fileSystem.WriteStringToFile(Path.Combine(theSolutionDir, SolutionFiles.ConfigFile), "");

            RippleFileSystem.StubCurrentDirectory(theCurrentDir);
        }

        [TearDown]
        public void TearDown()
        {
            fileSystem.DeleteDirectory(rootDir);

            RippleFileSystem.Live();
        }

        [Test]
        public void is_a_solution_directory()
        {
            RippleFileSystem.IsSolutionDirectory().ShouldBeTrue();
        }

        [Test]
        public void is_not_the_code_dir()
        {
            RippleFileSystem.IsCodeDirectory().ShouldBeFalse();
        }

        [Test]
        public void finds_the_solution_dir()
        {
            RippleFileSystem.FindSolutionDirectory().ShouldEqual(theSolutionDir.ToFullPath());
        }

		[Test]
		public void finding_the_solution_dir_with_do_not_throw_argument_should_not_throw()
		{
			var nonSolutionDir = theCodeDir.AppendPath("not-a-solution-dir");
			fileSystem.CreateDirectory(nonSolutionDir);
			RippleFileSystem.StubCurrentDirectory(nonSolutionDir);

			RippleFileSystem.FindSolutionDirectory(false).ShouldBeNull();
		}

        [Test]
        public void finds_the_code_dir()
        {
            RippleFileSystem.FindCodeDirectory().ShouldEqual(theCodeDir.ToFullPath());
        }
    }

    [TestFixture]
    public class from_the_code_dir
    {
        private string theSolutionDir;
        private string theCodeDir;
	    private string rootDir;

	    [SetUp]
        public void SetUp()
        {
			rootDir = Path.GetTempPath().AppendRandomPath();
            theCodeDir = rootDir.AppendPath("code");
            theSolutionDir = theCodeDir.AppendPath("ripple");

            var fileSystem = new FileSystem();
            fileSystem.CreateDirectory(theCodeDir);
            fileSystem.CreateDirectory(theSolutionDir);

            fileSystem.WriteStringToFile(Path.Combine(theSolutionDir, SolutionFiles.ConfigFile), "");

            RippleFileSystem.StubCurrentDirectory(theCodeDir);
            RippleFileSystem.StopTraversingAt(theCodeDir);
        }

        [TearDown]
        public void TearDown()
        {
            var fileSystem = new FileSystem();
            fileSystem.DeleteDirectory(rootDir);

            RippleFileSystem.Live();
        }

        [Test]
        public void is_the_code_directory()
        {
            RippleFileSystem.IsCodeDirectory().ShouldBeTrue();
        }

        [Test]
        public void is_not_a_solution_dir()
        {
            RippleFileSystem.IsSolutionDirectory().ShouldBeFalse();
        }

        [Test]
        public void finds_the_code_dir()
        {
            RippleFileSystem.FindCodeDirectory().ShouldEqual(theCodeDir.ToFullPath());
        }
    }
}
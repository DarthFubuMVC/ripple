using System.IO;
using System.Linq;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;

namespace ripple.Testing
{
    [TestFixture]
    public class ForceDeleteTester
    {
        private IFileSystem theFileSystem;
        private string theDirectory;

        [SetUp]
        public void Setup()
        {
            theFileSystem = new FileSystem();

			theDirectory = Path.GetTempPath().AppendRandomPath();
            theFileSystem.CreateDirectory(theDirectory);

            createFile("1.txt");
            createFile("2.txt");
            createFile("3.txt");

            createDirectory("A");
            createFile("A", "A1.txt");
            createFile("A", "A2.txt");
            createFile("A", "A3.txt");

            createDirectory("B");
            createFile("B", "B1.txt");

            createDirectory("C");
            createFile("C", "C1.txt");
            createFile("C", "C2.txt");

            theFileSystem.ForceClean(theDirectory);
        }

        private void createDirectory(params string[] parts)
        {
            theFileSystem.CreateDirectory(theDirectory.AppendPath(parts));
        }

        private void createFile(params string[] parts)
        {
            var path = theDirectory.AppendPath(parts);
            theFileSystem.WriteStringToFile(path, "");
        }

        [TearDown]
        public void TearDown()
        {
            theFileSystem.DeleteDirectory(theDirectory);
        }

        [Test]
        public void verify_the_directory_does_not_exist()
        {
            theFileSystem.FindFiles(theDirectory, FileSet.Everything()).Any().ShouldBeFalse();
        }
    }
}
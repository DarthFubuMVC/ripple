using System.Collections.Generic;
using System.IO;
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;
using ripple.Model;

namespace ripple.Testing.Model
{
	[TestFixture]
	public class SolutionFilesTester
	{
		private Solution theSolution;
		private SolutionFiles theSolutionFiles;
		private IFileSystem theFileSystem;

		[SetUp]
		public void SetUp()
		{
			theSolution = new Solution
			{
				Directory = "SolutionFiles"
			};


			theFileSystem = new FileSystem();
			theFileSystem.CreateDirectory("SolutionFiles");

			theSolutionFiles = new SolutionFiles(theFileSystem, new SolutionLoader());
			theSolutionFiles.RootDir = Path.GetTempPath().AppendRandomPath();
			
			theFileSystem.CreateDirectory("SolutionFiles", "src");

			theFileSystem.CreateDirectory("SolutionFiles", "src", "Project1");
			theFileSystem.CreateDirectory("SolutionFiles", "src", "Project2");

			theFileSystem.WriteStringToFile(Path.Combine("SolutionFiles", "src", "Project1", "Project1.csproj"), "test");
			theFileSystem.WriteStringToFile(Path.Combine("SolutionFiles", "src", "Project2", "Project2.csproj"), "test");
		}

		[TearDown]
		public void TearDown()
		{
			theFileSystem.DeleteDirectory(theSolutionFiles.RootDir);
		}

		[Test]
		public void reads_the_projects()
		{
			var projects = new List<string>();
			theSolutionFiles.ForProjects(theSolution, projects.Add);

			var project1 = Path.Combine("SolutionFiles", "src", "Project1", "Project1.csproj");
			var project2 = Path.Combine("SolutionFiles", "src", "Project2", "Project2.csproj");

			projects.ShouldHaveTheSameElementsAs(project1, project2);
		}
	}
}
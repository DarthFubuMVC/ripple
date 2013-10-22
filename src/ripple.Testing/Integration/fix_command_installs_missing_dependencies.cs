using System.Linq;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;
using ripple.Model;

namespace ripple.Testing.Integration
{
	[TestFixture]
	public class fix_command_installs_missing_dependencies
	{
		private SolutionGraphScenario theScenario;
		private Solution theSolution;

		[SetUp]
		public void SetUp()
		{
			theScenario = SolutionGraphScenario.Create(scenario =>
			{
				scenario.Solution("Test", test =>
				{
					test.SolutionDependency("FubuMVC.Core", "1.0.0.0", UpdateMode.Float);
					test.ProjectDependency("Test", "FubuMVC.Core");
				});
			});

			FeedScenario.Create(scenario =>
			{
				scenario.For(Feed.Fubu)
						.Add("FubuCore", "1.1.0.0")
						.Add("Bottles", "1.0.0.5")
						.Add("FubuMVC.Core", "1.0.0.0")
						.ConfigureRepository(fubu =>
						{
							fubu.ConfigurePackage("FubuMVC.Core", "1.0.0.0", mvc =>
							{
								mvc.DependsOn("FubuCore");
								mvc.DependsOn("Bottles");
							});
						});
			});

			theSolution = theScenario.Find("Test");

			RippleOperation
				.With(theSolution)
				.Execute<FixInput, FixCommand>();

			theSolution = SolutionBuilder.ReadFrom(theScenario.DirectoryForSolution("Test"));
		}

		[TearDown]
		public void TearDown()
		{
			theScenario.Cleanup();
			FeedRegistry.Reset();
		}

		[Test]
		public void adds_the_missing_solution_dependencies()
		{
			theSolution.FindDependency("Bottles").ShouldNotBeNull();
			theSolution.FindDependency("FubuCore").ShouldNotBeNull();
		}

		[Test]
		public void adds_the_missing_project_dependencies()
		{
			var project = theSolution.FindProject("Test");

			project.Dependencies.Has("Bottles").ShouldBeTrue();
			project.Dependencies.Has("FubuCore").ShouldBeTrue();
		}

		[Test, Ignore("Need to find a good way to simulate this")]
		public void adds_the_missing_project_references()
		{
			var project = theSolution.FindProject("Test");

			project.Proj.References.Any(x => x.Name == "Bottles").ShouldBeTrue();
			project.Proj.References.Any(x => x.Name == "FubuCore").ShouldBeTrue();
		}

		[Test]
		public void downloads_the_missing_dependencies()
		{
			theSolution.HasLocalCopy("Bottles").ShouldBeTrue();
			theSolution.HasLocalCopy("FubuCore").ShouldBeTrue();
			theSolution.HasLocalCopy("FubuMVC.Core").ShouldBeTrue();
		}
	}
}
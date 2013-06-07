using FubuTestingSupport;
using NUnit.Framework;
using NuGet;
using ripple.Commands;
using ripple.Model;

namespace ripple.Testing.Integration
{
	[TestFixture]
	public class installing_a_new_package_with_no_versions_on_dependencies
	{
		private SolutionGraphScenario theScenario;
		private Solution theSolution;

		[SetUp]
		public void SetUp()
		{
			FeedScenario.Create(scenario =>
			{
				scenario.For(Feed.Fubu)
					    .Add("FubuMVC.Katana", "1.0.0.1")
					    .Add("FubuMVC.Core", "1.0.1.1")
					    .Add("FubuMVC.OwinHost", "1.2.0.0")
					    .ConfigureRepository(teamcity =>
						{
							teamcity.ConfigurePackage("FubuMVC.Katana", "1.0.0.1", katana =>
							{
								katana.DependsOn("FubuMVC.Core");
								katana.DependsOn("FubuMVC.OwinHost");
							});

							teamcity.ConfigurePackage("FubuMVC.OwinHost", "1.2.0.0", owin => owin.DependsOn("FubuMVC.Core"));
						});
			});

			theScenario = SolutionGraphScenario.Create(scenario =>
			{
				scenario.Solution("Test", fubumvc => { });
			});

			theSolution = theScenario.Find("Test");
		}

		[TearDown]
		public void TearDown()
		{
			theScenario.Cleanup();
			FeedRegistry.Reset();
		}

		[Test]
		public void treats_the_dependencies_as_floats_for_installation()
		{
            RippleOperation
                .With(theSolution)
                .Execute<InstallInput, InstallCommand>(input =>
                {
                    input.ProjectFlag = "Test";
                    input.Package = "FubuMVC.Katana";
                });

			var local = theSolution.LocalDependencies();
			local.Get("FubuMVC.Katana").Version.ShouldEqual(new SemanticVersion("1.0.0.1"));
			local.Get("FubuMVC.Core").Version.ShouldEqual(new SemanticVersion("1.0.1.1"));
			local.Get("FubuMVC.OwinHost").Version.ShouldEqual(new SemanticVersion("1.2.0.0"));
		}
	}
}
using FubuCore;
using FubuTestingSupport;
using NUnit.Framework;
using Rhino.Mocks;
using ripple.Classic;
using ripple.Model;

namespace ripple.Testing.Classic
{
	[TestFixture]
	public class marking_floated_dependencies
	{
		private Solution theSolution;
		private SolutionConfig theConfig;

		private Project p1;
		private Project p2;

		private NuGetSolutionLoader theLoader;

		[SetUp]
		public void SetUp()
		{
			theConfig = new SolutionConfig();
			theConfig.FloatNuget("Bottles");
			theConfig.FloatNuget("FubuCore");
			theConfig.FloatNuget("FubuLocalization");

			theSolution = new Solution();
			p1 = theSolution.AddProject("Project1");
			p2 = theSolution.AddProject("Project2");

			p1.AddDependency(new Dependency("Bottles", "1.1.2.3", UpdateMode.Fixed));
			p1.AddDependency(new Dependency("FubuCore", "1.1.2.3", UpdateMode.Fixed));

			p2.AddDependency(new Dependency("FubuLocalization", "1.0.0.1", UpdateMode.Fixed));
			p2.AddDependency(new Dependency("StructureMap", "2.6.3", UpdateMode.Fixed));

			theLoader = new NuGetSolutionLoader();
			theLoader.MarkFloatingDependencies(theConfig, theSolution);
		}

		[Test]
		public void sets_the_project_level_dependencies_as_float()
		{
			p1.Dependencies.Find("Bottles").IsFloat().ShouldBeTrue();
			p1.Dependencies.Find("FubuCore").IsFloat().ShouldBeTrue();

			p2.Dependencies.Find("FubuLocalization").IsFloat().ShouldBeTrue();
		}

		[Test]
		public void leaves_fixed_as_fixed()
		{
			p2.Dependencies.Find("StructureMap").IsFloat().ShouldBeFalse();
		}
	}

	[TestFixture]
	public class extracting_specific_versions
	{
		private Solution theSolution;
		private SolutionConfig theConfig;

		private Project p1;
		private Project p2;

		private NuGetSolutionLoader theLoader;

		[SetUp]
		public void SetUp()
		{
			theConfig = new SolutionConfig();

			theSolution = new Solution();
			p1 = theSolution.AddProject("Project1");
			p2 = theSolution.AddProject("Project2");

			p1.AddDependency(new Dependency("Bottles"));
			p1.AddDependency(new Dependency("FubuCore", "1.1.2.3", UpdateMode.Fixed));

			p2.AddDependency(new Dependency("FubuCore", "1.1.2.3", UpdateMode.Fixed));
			p2.AddDependency(new Dependency("FubuLocalization"));
			p2.AddDependency(new Dependency("StructureMap", "2.6.3", UpdateMode.Fixed));

			theLoader = new NuGetSolutionLoader();
			theLoader.ExtractSolutionLevelConfiguration(theConfig, theSolution);
		}

		[Test]
		public void sets_the_solution_configured_dependencies()
		{
			theSolution.FindDependency("FubuCore").Version.ShouldEqual("1.1.2.3");
			theSolution.FindDependency("FubuCore").IsFloat().ShouldBeFalse();

			theSolution.FindDependency("StructureMap").Version.ShouldEqual("2.6.3");
			theSolution.FindDependency("StructureMap").IsFloat().ShouldBeFalse();
		}

		[Test]
		public void marks_the_projects_as_float()
		{
			p1.Dependencies.Find("FubuCore").IsFloat().ShouldBeTrue();

			p2.Dependencies.Find("FubuCore").IsFloat().ShouldBeTrue();
			p2.Dependencies.Find("StructureMap").IsFloat().ShouldBeTrue();
		}
	}

    [TestFixture]
    public class load_adds_feeds
    {
        private SolutionConfig theConfig;
        private Solution theSolution;
        private Feed theFeed;

        private NuGetSolutionLoader theLoader;

        [SetUp]
        public void SetUp()
        {
            theFeed = new Feed();
            theConfig = new SolutionConfig
                {
                    Feeds = new[] {theFeed}
                };

            const string rippleConfig = "ripple.config";
            var fileSystem = MockRepository.GenerateStub<IFileSystem>();
            fileSystem.Stub(x => x.LoadFromFile<SolutionConfig>(rippleConfig)).Return(theConfig);
            
            theLoader = new NuGetSolutionLoader();
            theSolution = theLoader.LoadFrom(fileSystem, rippleConfig);
        }

        [Test]
        public void adds_the_feed_to_solution()
        {
            theSolution.Feeds.ShouldContain(theFeed);
        }
    }
}
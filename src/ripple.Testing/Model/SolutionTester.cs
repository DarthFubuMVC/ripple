using FubuTestingSupport;
using NUnit.Framework;
using NuGet;
using Rhino.Mocks;
using ripple.Local;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Testing.Model
{
	[TestFixture]
	public class SolutionTester
	{
		[Test]
		public void default_feeds()
		{
			new Solution()
				.Feeds
				.ShouldHaveTheSameElementsAs(Feed.Fubu, Feed.NuGetV2);
		}

		[Test]
		public void default_storage()
		{
			new Solution().Storage.ShouldBeOfType<NugetStorage>();
		}

		[Test]
		public void default_feed_service()
		{
			new Solution().FeedService.ShouldBeOfType<FeedService>();
		}

		[Test]
		public void default_mode_is_ripple()
		{
			new Solution().Mode.ShouldEqual(SolutionMode.Ripple);
		}

		[Test]
		public void adding_a_project_sets_the_solution()
		{
			var solution = new Solution();
			var project = new Project("MyProject.csproj");

			solution.AddProject(project);
			solution.Projects.ShouldHaveTheSameElementsAs(project);

			project.Solution.ShouldBeTheSameAs(solution);
		}

		[Test]
		public void missing_files()
		{
			var solution = new Solution();
			var storage = MockRepository.GenerateStub<INugetStorage>();

			var q1 = new Dependency("Bottles", "1.0.1.1");
			var q2 = new Dependency("FubuCore", "1.0.1.252");

			storage.Stub(x => x.MissingFiles(solution)).Return(new[] {q1, q2});

			solution.UseStorage(storage);

			solution.MissingNugets().ShouldHaveTheSameElementsAs(q1, q2);
		}

		[Test]
		public void local_dependencies()
		{
			var solution = new Solution();
			var storage = MockRepository.GenerateStub<INugetStorage>();

			var dependencies = new LocalDependencies(new[] {new NugetFile("Bottles.1.0.1.252.nupkg", SolutionMode.Ripple)});
			storage.Stub(x => x.Dependencies(solution)).Return(dependencies);

			solution.UseStorage(storage);

			solution.LocalDependencies().ShouldEqual(dependencies);
		}


		[Test]
		public void find_the_dependency_configuration()
		{
			var dependency = new Dependency("Bottles", "1.0.0.0");
			
			var solution = new Solution();
			solution.AddDependency(dependency);

			solution.FindDependency("Bottles").ShouldEqual(dependency);
		}

		[Test]
		public void combines_the_dependencies()
		{
			var solution = new Solution();
			solution.AddDependency(new Dependency("Bottles", "1.0.1.1"));

			var project = new Project("MyProject.csproj");
			project.AddDependency(new Dependency("FubuCore", "1.2.3.4"));

			solution.AddProject(project);

			solution.Dependencies.ShouldHaveTheSameElementsAs(new Dependency("Bottles", "1.0.1.1"), new Dependency("FubuCore", "1.2.3.4"));
		}

		[Test]
		public void saving_the_solution_with_no_changes_in_projects()
		{
			var storage = MockRepository.GenerateStub<INugetStorage>();

			var solution = new Solution();
			var project = new Project("Test.csproj");

			solution.AddProject(project);
			solution.UseStorage(storage);

			solution.Save();

			storage.AssertWasCalled(x => x.Write(solution));
			storage.AssertWasNotCalled(x => x.Write(project));
		}

		[Test]
		public void force_saving_the_solution_with_no_changes_in_projects()
		{
			var storage = MockRepository.GenerateStub<INugetStorage>();

			var solution = new Solution();
			var project = new Project("Test.csproj");

			solution.AddProject(project);
			solution.UseStorage(storage);

			solution.Save(true);

			storage.AssertWasCalled(x => x.Write(solution));
			storage.AssertWasCalled(x => x.Write(project));
		}

		[Test]
		public void saving_the_solution_with_changed_projects()
		{
			var storage = MockRepository.GenerateStub<INugetStorage>();

			var solution = new Solution();
			var project = new Project("Test.csproj");

			solution.AddProject(project);
			solution.UseStorage(storage);

			project.AddDependency("FubuCore");

			solution.Save(true);

			storage.AssertWasCalled(x => x.Write(solution));
			storage.AssertWasCalled(x => x.Write(project));
		}

		[Test]
		public void convert_solution()
		{
			var storage = MockRepository.GenerateStub<INugetStorage>();

			var solution = new Solution();
			solution.UseStorage(storage);

			solution.ConvertTo(SolutionMode.Ripple);

			storage.AssertWasCalled(x => x.Reset(solution));

			solution.Storage.ShouldBeOfType<NugetStorage>().Strategy.ShouldBeOfType<RippleDependencyStrategy>();
		}

		[Test]
		public void retrieve_the_nuget_specs()
		{
			var s1 = new NugetSpec("Test1", "Test1.nuspec");
			var s2 = new NugetSpec("Test2", "Test2.nuspec");

			var solution = new Solution();

			var service = MockRepository.GenerateStub<IPublishingService>();
			service.Stub(x => x.SpecificationsFor(solution)).Return(new[] {s1, s2});

			solution.UsePublisher(service);

			solution.Specifications.ShouldHaveTheSameElementsAs(s1, s2);
		}

		[Test]
		public void publishes_the_specification()
		{
			var s1 = new NugetSpec("Test1", "Test1.nuspec");

			var solution = new Solution();

			var service = MockRepository.GenerateStub<IPublishingService>();
			service.Stub(x => x.SpecificationsFor(solution)).Return(new[] { s1 });

			solution.UsePublisher(service);

			var version = SemanticVersion.Parse("1.1.2.3");
			solution.Package(s1, version, "artifacts", false);

			service.AssertWasCalled(x => x.CreatePackage(s1, version, "artifacts", false));
		}
	}
}
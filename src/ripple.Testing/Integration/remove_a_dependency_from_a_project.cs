using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;
using ripple.Model;

namespace ripple.Testing.Integration
{
    [TestFixture]
    public class remove_a_dependency_from_a_project
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
                            test.SolutionDependency("FubuCore", "1.2.0.0", UpdateMode.Float);
                            test.SolutionDependency("UnneededAnywhereButHere", "1.2.0.0", UpdateMode.Float);

                            test.ProjectDependency("Test1", "FubuCore");
                            test.ProjectDependency("Test1", "UnneededAnywhereButHere");

                            test.ProjectDependency("Test2", "FubuCore");

                            test.LocalDependency("FubuCore", "1.2.0.0");
                            test.LocalDependency("UnneededAnywhereButHere", "1.2.0.0");
                        });
                });

            theSolution = theScenario.Find("Test");

            RippleOperation
                .With(theSolution)
                .Execute<RemoveInput, RemoveCommand>(new RemoveInput { Nuget = "FubuCore", ProjectFlag = "Test1"});
            RippleOperation
                .With(theSolution)
                .Execute<RemoveInput, RemoveCommand>(new RemoveInput { Nuget = "UnneededAnywhereButHere", ProjectFlag = "Test1" });

            theSolution = SolutionBuilder.ReadFrom(theScenario.DirectoryForSolution("Test"));
        }

        [TearDown]
        public void TearDown()
        {
            theScenario.Cleanup();
        }

        [Test]
        public void keeps_the_solution_dependency()
        {
            theSolution.Dependencies.Has("FubuCore").ShouldBeTrue();
        }

        [Test]
        public void removes_the_project_level_dependency()
        {
            theSolution.FindProject("Test1").Dependencies.Has("FubuCore").ShouldBeFalse();
        }

        [Test]
        public void keeps_the_other_project_dependency()
        {
            theSolution.FindProject("Test2").Dependencies.Has("FubuCore").ShouldBeTrue();
        }

        [Test]
        public void keeps_the_directory()
        {
            theSolution.LocalDependencies().Has("FubuCore").ShouldBeTrue();
        }

        [Test]
        public void removes_the_solution_dependency_on_nowhere_referenced_package()
        {
            theSolution.Dependencies.Has("UnneededAnywhereButHere").ShouldBeFalse();
        }
        
        [Test]
        public void removes_the_project_level_dependency_on_nowhere_referenced_package()
        {
            theSolution.FindProject("Test1").Dependencies.Has("UnneededAnywhereButHere").ShouldBeFalse();
        }

        [Test]
        public void removes_the_directory_of_nowhere_referenced_package()
        {
            theSolution.LocalDependencies().Has("UnneededAnywhereButHere").ShouldBeFalse();
        }
    }
}
using FubuTestingSupport;
using NUnit.Framework;
using NuGet;
using ripple.Commands;
using ripple.Model;

namespace ripple.Testing.Integration
{
    [TestFixture]
    public class installing_the_same_package_with_different_casing
    {
        private SolutionGraphScenario theScenario;
        private Solution theSolution;

        [SetUp]
        public void SetUp()
        {
            FeedScenario.Create(scenario => scenario.For(Feed.Fubu)
                                                    .Add("FubuMVC.Core", "1.0.1.1")
                                                    .Add("dep", "1.0.0.0")
                                                    .Add("dep1", "1.0.0.0")
                                                    .Add("dep2", "1.0.0.0")
                                                    .ConfigureRepository(r =>
                                                        {
                                                            r.ConfigurePackage("dep", "1.0.0.0", sp =>
                                                                {
                                                                    sp.DependsOn("dep1", "1.0.0.0"); 
                                                                    sp.DependsOn("dep2", "1.0.0.0"); 
                                                                });
                                                            r.ConfigurePackage("dep1", "1.0.0.0", sp=>sp.DependsOn("FubuMVC.Core", "1.0.1.1"));
                                                            r.ConfigurePackage("dep2", "1.0.0.0", sp=>sp.DependsOn("fubumvc.core", "1.0.1.1"));
                                                        })
                                                    );

            theScenario = SolutionGraphScenario.Create(scenario => scenario.Solution("Test"));

            theSolution = theScenario.Find("Test");
        }

        [TearDown]
        public void TearDown()
        {
            theScenario.Cleanup();
            FeedRegistry.Reset();
        }

        [Test]
        public void treats_both_cases_as_one_dependency()
        {
            RippleOperation
                .With(theSolution)
                .Execute<InstallInput, InstallCommand>(input =>
                    {
                        input.ProjectFlag = "Test";
                        input.Package = "dep";
                    });

            theSolution.Dependencies.Has("FubuMVC.Core");
        }
    }
}
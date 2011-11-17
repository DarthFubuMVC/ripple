using System.Collections.Generic;
using System.Linq;
using FubuTestingSupport;
using NUnit.Framework;
using NuGet;
using Rhino.Mocks;
using ripple.Nuget;

namespace ripple.Testing.Nuget
{
    [TestFixture]
    public class PackageRepositoryBuilderTester : InteractionContext<PackageRepositoryBuilder>
    {
        private readonly IEnumerable<string> _extraFeeds = new List<string> { "http://p1.org/", PackageRepositoryBuilder.GalleryUrl };

        [Test]
        public void build_remote_returns_aggregate()
        {
            ClassUnderTest.BuildRemote(Enumerable.Empty<string>())
                .ShouldBeOfType<AggregateRepository>();
        }

        [Test]
        public void build_remote_returns_aggregate_with_proper_feeds()
        {
            MockFor<IPackageRepositoryFactory>()
                .Stub(x => x.CreateRepository(Arg<string>.Matches(s => _extraFeeds.Contains(s))))
                .Repeat.Twice()
                .Return(MockFor<IPackageRepository>());
            
            ClassUnderTest.BuildRemote(_extraFeeds);
            MockFor<IPackageRepositoryFactory>().VerifyAllExpectations();
        }

        [Test]
        public void build_local_delegates_to_factory()
        {
            MockFor<IPackageRepositoryFactory>().Expect(x => x.CreateRepository("data"));
            ClassUnderTest.BuildLocal("data");
            MockFor<IPackageRepositoryFactory>().VerifyAllExpectations();
        }

        [Test]
        public void build_source_returns_aggregate()
        {
            var repo1 = MockRepository.GenerateMock<IPackageRepository>();
            var repo2 = MockRepository.GenerateMock<IPackageRepository>();
            
            ClassUnderTest.BuildSource(repo1, repo2).ShouldBeOfType<AggregateRepository>()
                .Repositories.ShouldHaveTheSameElementsAs(repo1, repo2);
        }
    }
}

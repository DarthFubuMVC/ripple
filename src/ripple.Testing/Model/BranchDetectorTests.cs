using FubuTestingSupport;
using NUnit.Framework;
using ripple.Model;

namespace ripple.Testing.Model
{
    [TestFixture]
    public class BranchDetectorTests
    {
        [Test]
        public void can_detect_git_branch()
        {
            var branch = BranchDetector.GetBranch();
            branch.ShouldNotBeEmpty();
        }
    }
}
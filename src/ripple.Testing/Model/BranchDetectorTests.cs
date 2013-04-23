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
            //don't want this to fail if someone happenes to be on a different branch than master
            //so not doing an explicit 'master' or other branch name check
            branch.ShouldNotBeEmpty();
        }
    }
}

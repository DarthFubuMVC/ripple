using System.Collections.Generic;
using FubuCore;
using FubuCore.CommandLine;
using FubuCore.Descriptions;
using FubuTestingSupport;
using NUnit.Framework;
using ripple.Commands;
using ripple.Model;

namespace ripple.Testing
{
	[TestFixture]
	public class RippleOperationTester
	{
		[Test]
		public void executes_the_steps()
		{
			var s1 = new RecordingStep();
			var s2 = new RecordingStep();

            RippleOperation
                .With(new Solution { Directory = ".", Path = "test.config"})
                .Execute(new TestInput(), new TestCommand(new[] { s1, s2 }));

			s1.Executed.ShouldBeTrue();
			s2.Executed.ShouldBeTrue();
		}

        [Test]
        public void description_for_branch()
        {
            BranchDetector.Stub(() => true, () => true);
            BranchDetector.Stub(() => "test");

            var operation = new RippleOperation(new Solution {Name = "Test"}, new SolutionInput(), new RippleStepRunner(new FileSystem()));
            var description = new Description();

            operation.Describe(description);

            description.ShortDescription.ShouldEqual("Test (test)");

            BranchDetector.Live();
        }

        [Test]
        public void description_for_no_branch()
        {
            BranchDetector.Stub(() => false, () => false);
            BranchDetector.Stub(() => "test");

            var operation = new RippleOperation(new Solution { Name = "Test" }, new SolutionInput(), new RippleStepRunner(new FileSystem()));
            var description = new Description();

            operation.Describe(description);

            description.ShortDescription.ShouldEqual("Test");

            BranchDetector.Live();
        }

        public class TestCommand : FubuCommand<TestInput>
        {
            private readonly IEnumerable<IRippleStep> _steps;

            public TestCommand(IEnumerable<IRippleStep> steps)
            {
                _steps = steps;
            }

            public override bool Execute(TestInput input)
            {
                return RippleOperation
                    .For<TestInput>(input)
                    .Steps(_steps)
                    .Execute(true);
            }
        }
	}

	public class TestInput : SolutionInput { }

	public class RecordingStep : IRippleStep
	{
		public Solution Solution { get; set; }
		public bool Executed { get; set; }

		public void Execute(RippleInput input, IRippleStepRunner runner)
		{
			Executed = true;
		}
	}
}
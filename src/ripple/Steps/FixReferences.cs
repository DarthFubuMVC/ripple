using FubuCore.Descriptions;
using ripple.Commands;
using ripple.MSBuild;
using ripple.Model;

namespace ripple.Steps
{
    public interface IManageReferenceFixing
    {
        bool ShouldReferencesBeFixed();
    }

	public class FixReferences : IRippleStep, DescribesItself
	{
		public Solution Solution { get; set; }

        public void Execute(RippleInput input, IRippleStepRunner runner)
        {
            var manageFixing = input as IManageReferenceFixing;
            if (manageFixing != null && manageFixing.ShouldReferencesBeFixed() == false)
                return;

            var attacher = new ReferenceAttacher(Solution);
			attacher.Attach();
		}

		public void Describe(Description description)
		{
			description.ShortDescription = "Fix References";
		}
	}
}
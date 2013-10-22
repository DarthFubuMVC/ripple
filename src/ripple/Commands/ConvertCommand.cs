using System.ComponentModel;
using FubuCore.CommandLine;
using ripple.Model;
using System.Linq;

namespace ripple.Commands
{
	public class ConvertInput : SolutionInput
	{
        [Description("Additional NuGet feed urls separated by '#'")]
        public string FeedsFlag { get; set; }
	}

    [CommandDescription("Converts a solution using the Nuget packages.config configuration to the Ripple v2+ format")]
	public class ConvertCommand : FubuCommand<ConvertInput>
	{
		public override bool Execute(ConvertInput input)
		{
			var solution = Solution.For(input);
			solution.ConvertTo(SolutionMode.Ripple);

		    ApplyCustomFeeds(input.FeedsFlag, solution);

		    solution.Save(true);

			new RestoreCommand().Execute(new RestoreInput
			{
				CacheFlag = input.CacheFlag,
			});

			// Third time's a charm, apparently
			forceFixReferences(input);

			return true;
		}

        // may be later converted into a small step
	    private static void ApplyCustomFeeds(string feedsFlag, Solution solution)
	    {
	        var customFeeds = feedsFlag.GetFeeds();
	        if (customFeeds.Any())
	        {
	            solution.ClearFeeds();
	            solution.AddFeeds(customFeeds);
	        }
	    }

	    private static void forceFixReferences(ConvertInput input)
		{
			var solution = Solution.For(input);
			solution.EachProject(x =>
			{
			    x.RemoveDuplicateReferences();

                if (x.Proj.UsesPackagesConfig())
                {
                    x.Proj.ConvertToRippleDependenciesConfig();
                }
			});

			solution.Save(true);
		}
	}
}
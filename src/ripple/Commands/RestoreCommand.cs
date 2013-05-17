using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FubuCore;
using FubuCore.CommandLine;
using ripple.Model;
using ripple.Steps;

namespace ripple.Commands
{
	public class RestoreInput : SolutionInput, IOverrideFeeds
	{
		[Description("Additional NuGet feed urls separated by '#'")]
		public string FeedsFlag { get; set; }

		[Description("Forces the restoration to correct any version mismatches")]
		[FlagAlias("force", 'f')]
		public bool ForceFlag { get; set; }

		public override string DescribePlan(Solution solution)
		{
			return "Restoring dependencies for solution {0} to {1}".ToFormat(solution.Name, solution.PackagesDirectory());
		}

		public override void ApplyTo(Solution solution)
		{
			if (ForceFlag)
			{
				solution.ForceRestore();
			}
		}

		public IEnumerable<Feed> Feeds()
		{
		    return FeedsFlag.GetFeeds();
		}
	}

	// Remove when it is possible to have IEnumerable<string> flag
	public static class StringFeeds
	{
		public static IEnumerable<string> ParseFeeds(this string urlString)
		{
			return urlString.IsNotEmpty()
				? urlString.ToDelimitedArray('#')
				: Enumerable.Empty<string>();
		}

	    public static IEnumerable<Feed> GetFeeds(this string feedsFlag)
	    {
	        if (feedsFlag.IsEmpty())
	        {
	            return new Feed[0];
	        }

	        return feedsFlag
	            .ParseFeeds()
	            .Select(Feed.FindOrCreate);
	    }
	}

	[CommandDescription("Restores nugets for the solution")]
	public class RestoreCommand : FubuCommand<RestoreInput>
	{
		public override bool Execute(RestoreInput input)
		{
			return RippleOperation
				.For<RestoreInput>(input)
				.Step<DownloadMissingNugets>()
				.Step<ExplodeDownloadedNugets>()
				.Step<ProcessDirectives>()
				.Step<FixReferences>()
				.Execute();
		}
	}
}
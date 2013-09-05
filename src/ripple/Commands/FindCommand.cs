using FubuCore.CommandLine;
using ripple.Model;

namespace ripple.Commands
{
    public class FindNugetsInput : RippleInput
    {
        public string Nuget { get; set; }
    }

    [CommandDescription("Queries feeds for nugets", Name = "find")]
    public class FindNugetsCommand : FubuCommand<FindNugetsInput>
    {
        public override bool Execute(FindNugetsInput input)
        {
            var solution = Solution.For(input);
            var connectivity = new FeedConnectivity();

            var feeds = connectivity.FeedsFor(solution);

            foreach (var feed in feeds)
            {
                connectivity.IfOnline(feed, f =>
                {
                    foreach (var nuget in f.FindLatestByName(input.Nuget))
                    {
                        RippleLog.Info(string.Format("{0}, {1} ({2})", nuget.Name, nuget.Version, f.Repository.Source));
                    }
                });
            }

            return true;
        }
    }
}

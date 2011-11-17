using System;
using System.ComponentModel;
using FubuCore.CommandLine;
using System.Collections.Generic;
using FubuCore;
using System.Linq;
using FubuCore.Util;
using ripple.Local;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Commands
{
    public class UpdateInput : SolutionInput
    {
        [Description("Only update a specific nuget by name")]
        public string NugetFlag { get; set; }

        [Description("Only show what would be updated")]
        public bool PreviewFlag { get; set; }

        [Description("Forces the update command to override all dependencies even if they are locked")]
        public bool ForceFlag { get; set; }

        [Description("Additional NuGet feed urls separated by '#'")]
        public string FeedsFlag { get; set; }

        public IEnumerable<string> GetAllNugetNames(Solution solution)
        {
            if (NugetFlag.IsNotEmpty())
            {
                return new[]{NugetFlag};
            }

            if (ForceFlag)
            {
                return solution.GetAllNugetDependencies().Select(x => x.Name).Distinct().ToList();
            }

            return solution.GetAllNugetDependencies()
                .Where(x => x.UpdateMode == UpdateMode.Float)
                .Select(x => x.Name).Distinct().ToList();
        }
    }

    [CommandDescription("Update nuget versions for solutions")]
    public class UpdateCommand : FubuCommand<UpdateInput>
    {
        public override bool Execute(UpdateInput input)
        {
            var system = new FileSystem();

            input.FindSolutions().Each(solution =>
            {
                var feeds = input.FeedsFlag.ParseFeeds();
                var nugetService = new NugetService(solution, feeds);
                system.CreateDirectory(solution.PackagesFolder());

                var plan = new NugetUpdatePlan(solution);

                var allNugetNames = input.GetAllNugetNames(solution);

                allNugetNames.Each(name =>
                {
                    var latest = nugetService.GetLatest(name);
                    Console.WriteLine("Latest of {0} is {1}", latest.Name, latest.Version);
                    plan.UseLatestNuget(latest);
                });

                if (input.PreviewFlag)
                {
                    plan.Preview();                    
                }
                else
                {
                    Console.WriteLine();
                    Console.WriteLine("Updating " + solution.Name);
                    plan.Apply(nugetService);
                }
            });

            var listInput = new ListInput(){
                SolutionFlag = input.SolutionFlag,
                AllFlag = input.AllFlag,
                Mode = ListMode.dependencies
            };

            new ListCommand().Execute(listInput);

            return true;
        }
    }

    public class NugetUpdatePlan
    {
        private readonly Solution _solution;
        private readonly Cache<Project, IList<NugetDependency>> _updates = new Cache<Project, IList<NugetDependency>>(p => new List<NugetDependency>());
        private readonly Cache<Project, IList<NugetDependency>> _removes = new Cache<Project, IList<NugetDependency>>(p => new List<NugetDependency>());
        private readonly IList<NugetDependency> _dependenciesToRemove = new List<NugetDependency>();

        public NugetUpdatePlan(Solution solution)
        {
            _solution = solution;
        }

        public void UseLatestNuget(NugetDependency latest)
        {
            var depsToRemove = _solution.GetAllNugetDependencies().Where(x => x.DifferentVersionOf(latest));
            _dependenciesToRemove.Fill(depsToRemove.ToList());

            _solution.Projects.Where(x => x.ShouldBeUpdated(latest)).Each(proj =>
            {
                _updates[proj].Add(latest);
                _removes[proj].AddRange(proj.NugetDependencies.Where(x => x.DifferentVersionOf(latest)));
            });
        }

        public void Apply(INugetService service)
        {
            _updates.Each(service.Update);

            _removes.Each(service.RemoveFromProject);

            _dependenciesToRemove.Each(service.RemoveFromFileSystem);
        }

        public void Preview()
        {
            ConsoleWriter.Write(ConsoleColor.Cyan, "Needs to be updated");
            _updates.Each((proj, deps) =>
            {
                ConsoleWriter.Write("  Project {0}", proj.ProjectName);
                deps.Each(dep => ConsoleWriter.Write("    - to " + dep));
            });

            ConsoleWriter.Write(ConsoleColor.Cyan, "Remove:");
            _dependenciesToRemove.Each(x => Console.Write("  " + x));
        }
    }



}
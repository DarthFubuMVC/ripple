using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore;
using FubuCore.Descriptions;
using ripple.Commands;
using ripple.Model;
using ripple.Nuget;

namespace ripple.Steps
{
    public class ExplodeSourceOnlyPackages : IRippleStep, DescribesItself
    {
        public Solution Solution { get; set; }

        public void Execute(RippleInput input, IRippleStepRunner runner)
        {
            var downloadedNugets = runner.Get<DownloadedNugets>();

            if (downloadedNugets.Any())
            {
                Solution.AssertNoLockedFiles();
            }

            downloadedNugets.Each(UnpackSourceOnlyNugetToProjects);
        }


        public void Describe(Description description)
        {
            description.ShortDescription = "Explodes source only nugets to the relevant projects";
        }

        void UnpackSourceOnlyNugetToProjects(INugetFile nuget)
        {
            if (!nuget.IsSourceOnlyPackage())
            {
                RippleLog.Debug("{0} is not a source only package and will be skipped".ToFormat(nuget.Name));
                return;
            }

            RippleLog.Info("{0} is a source only package. Going to explode into referencing projects".ToFormat(nuget.Name));

            Solution.Projects.Each(project =>
            {
                if (project.Dependencies.All(dep => dep.Name.ToLower() != nuget.Name.ToLower()))
                {
                    RippleLog.Debug("Package {0} not used by {1}".ToFormat(nuget.Name,project.Name)); 
                    return;
                }

                var directoryName = nuget.Name.Replace(".Sources", "").Replace("-CodeOnly","");

                nuget.ExplodeSourcesTo(Path.Combine(project.Directory, "App_Packages", directoryName));
            });
        }



    }
}
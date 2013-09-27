using NUnit.Framework;
using FubuCore;
using FubuTestingSupport;
using ripple.Local;
using ripple.Model;
using ripple.Nuget;
using ripple.Runners;

namespace ripple.Testing
{
    [TestFixture]
    public class SolutionTester
    {
        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void create_process_info_for_full_build()
        {
            var solution = new Solution
            {
                SourceFolder = "src",
                BuildCommand = "rake",
                FastBuildCommand = "rake compile",
                Directory = "directory1".ToFullPath()
            };

            var processInfo = solution.CreateBuildProcess(false);

            processInfo.WorkingDirectory.ShouldEqual("directory1".ToFullPath());
            processInfo.FileName.ShouldEqual(Runner.Rake.Path);
            processInfo.Arguments.ShouldBeEmpty();
        }

        [Test]
        [Platform(Exclude = "Unix,Linux,MacOsX")]
        public void create_process_for_fast_build()
        {
            var solution = new Solution
            {
                SourceFolder = "src",
                BuildCommand = "rake",
                FastBuildCommand = "rake compile",
                Directory = "directory1".ToFullPath()
            };

            var processInfo = solution.CreateBuildProcess(true);

            processInfo.WorkingDirectory.ShouldEqual("directory1".ToFullPath());
            processInfo.FileName.ShouldEqual(Runner.Rake.Path);
            processInfo.Arguments.ShouldEqual("compile");
        }

        [Test]
        [Platform(Exclude = "Win")]
        public void create_process_info_for_full_build_on_unix()
        {
            var solution = new Solution
            {
                SourceFolder = "src",
                BuildCommand = "rake",
                FastBuildCommand = "rake compile",
                Directory = "directory1".ToFullPath()
            };

            var processInfo = solution.CreateBuildProcess(false);

            processInfo.WorkingDirectory.ShouldEqual("directory1".ToFullPath());
            processInfo.FileName.ShouldEqual("/bin/bash");
            processInfo.Arguments.ShouldEqual("\"" + Runner.Rake.Path + "\"");
        }

        [Test]
        [Platform(Exclude = "Win")]
        public void create_process_for_fast_build_on_unix()
        {
            var solution = new Solution
            {
                SourceFolder = "src",
                BuildCommand = "rake",
                FastBuildCommand = "rake compile",
                Directory = "directory1".ToFullPath()
            };

            var processInfo = solution.CreateBuildProcess(true);

            processInfo.WorkingDirectory.ShouldEqual("directory1".ToFullPath());
            processInfo.FileName.ShouldEqual("/bin/bash");
            processInfo.Arguments.ShouldEqual("\""+ Runner.Rake.Path + "\" compile");
        }

        [Test]
        public void get_nuget_directory()
        {
            var solution = new Solution
            {
                SourceFolder = "source",
                Directory = ".".ToFullPath()
            };

            var storage = new StubNugetStorage();
            storage.Add("FubuCore", "0.9.1.37");
            solution.UseStorage(storage);

            var project = new Project("something.csproj");
            var dependency = new Dependency("FubuCore", "0.9.1.37");
            project.AddDependency(dependency);
            solution.AddProject(project);

            var spec = new NugetSpec("FubuCore", "somefile.nuspec");

            solution.NugetFolderFor(spec)
                .ShouldEqual(".".ToFullPath().AppendPath(solution.PackagesDirectory(), "FubuCore"));
        
        }

        [Test]
        public void gets_the_default_float_constraint()
        {
            var solution = new Solution();
            solution.DefaultFloatConstraint.ShouldEqual(solution.NuspecSettings.Float.ToString());
        }

        [Test]
        public void gets_the_default_fixed_constraint()
        {
            var solution = new Solution();
            solution.DefaultFloatConstraint.ShouldEqual(solution.NuspecSettings.Float.ToString());
        }

        [Test]
        public void sets_the_default_float_constraint()
        {
            var solution = new Solution();
            solution.DefaultFloatConstraint = "Current,NextMinor";

            solution.NuspecSettings.Float.ToString().ShouldEqual("Current,NextMinor");
        }

        [Test]
        public void sets_the_default_fixed_constraint()
        {
            var solution = new Solution();
            solution.DefaultFixedConstraint = "Current";

            solution.NuspecSettings.Fixed.ToString().ShouldEqual("Current");
        }

        [Test]
        public void uses_explicit_dependency_constraint()
        {
            var explicitDep = new Dependency("FubuCore") { Constraint = "Current,NextMinor"};


            var solution = new Solution();
            solution.AddDependency(explicitDep);

            solution.ConstraintFor(explicitDep).ToString().ShouldEqual("Current,NextMinor");
        }

        [Test]
        public void falls_back_to_default_constraint_for_float()
        {
            var dep = new Dependency("FubuCore", UpdateMode.Float);


            var solution = new Solution();
            solution.AddDependency(dep);

            solution.ConstraintFor(dep).ShouldEqual(solution.NuspecSettings.Float);
        }

        [Test]
        public void falls_back_to_default_constraint_for_fixed()
        {
            var dep = new Dependency("FubuCore", UpdateMode.Fixed);


            var solution = new Solution();
            solution.AddDependency(dep);

            solution.ConstraintFor(dep).ShouldEqual(solution.NuspecSettings.Fixed);
        }

        [Test]
        public void resets_the_cache_when_a_custom_directory_is_specified()
        {
            var directory = "MyCache";
            var solution = new Solution();
            solution.NugetCacheDirectory = directory;

            solution.Cache.As<NugetFolderCache>().LocalPath.ShouldEqual(directory);
        }
    }
}

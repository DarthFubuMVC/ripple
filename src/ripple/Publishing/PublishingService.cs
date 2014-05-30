using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FubuCore;
using FubuCore.CommandLine;
using NuGet;
using ripple.Model;
using ripple.Nuget;
using ripple.Packaging;

namespace ripple.Publishing
{
    public class PublishingService : IPublishingService
    {
        // NOTE: the file exclusions taken from the original NuGet

        // Target file paths to exclude when building the lib package for symbol server scenario
        private static readonly string[] _libPackageExcludes = new[] {
            @"**\*.pdb",
            @"src\**\*"
        };
        // Target file paths to exclude when building the symbols package for symbol server scenario
        private static readonly string[] _symbolPackageExcludes = new[] {
            @"content\**\*",
            @"tools\**\*.ps1"
        };

        internal static void ExcludeFilesForLibPackage(ICollection<IPackageFile> files)
        {
            PathResolver.FilterPackageFiles(files, file => file.Path, _libPackageExcludes);
        }
        internal static void ExcludeFilesForSymbolPackage(ICollection<IPackageFile> files)
        {
            PathResolver.FilterPackageFiles(files, file => file.Path, _symbolPackageExcludes);
        }

        public const string ApiKey = "ripple-api-key";

        public static readonly IEnumerable<IPackageRule> Rules;

        static PublishingService()
        {
            try
            {
                var types = typeof(IPackageRule).Assembly.GetTypes();
                Rules = types.Where(x => TypeExtensions.IsConcreteTypeOf<IPackageRule>(x)).Select(x => (IPackageRule)Activator.CreateInstance(x));
            }
            catch
            {
                Rules = new IPackageRule[0];
            }
        }

        private readonly ISolutionFiles _files;

        public PublishingService(ISolutionFiles files)
        {
            _files = files;
        }

        public IEnumerable<NugetSpec> SpecificationsFor(Solution solution)
        {
            var specs = new List<NugetSpec>();
            _files.ForNuspecs(solution, file =>
                {
                    var spec = NugetSpec.ReadFrom(file.ToFullPath());
                    spec.Publisher = solution;

                    specs.Add(spec);
                });

            return specs;
        }

        public static PublishingService Basic()
        {
            return new PublishingService(SolutionFiles.Basic());
        }

        public void CreateSymbolsPackage(PackageParams ctx)
        {
            var symbolsBuilder = symbolsPackageBuilderFor(ctx.Spec, ctx.Version);
            // remove unnecessary files when building the symbols package
            ExcludeFilesForSymbolPackage(symbolsBuilder.Files);

            if (!symbolsBuilder.Files.Any())
            {
                RippleLog.Info("No symbols could be generated for {0} since no symbol files could be found. This is expected if this is a code only package".ToFormat(ctx.Spec.Name));
                return;
            }

            var nupkgSymbolsFileName = Path.Combine(ctx.OutputPath, "{0}.{1}.symbols.nupkg".ToFormat(ctx.Spec.Name, ctx.Version));

            var package = createPackage(symbolsBuilder, nupkgSymbolsFileName);

            var issues = package.Validate(Rules);

            if (issues.Any(x => x.Level == PackageIssueLevel.Error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                issues.Each(issue => Console.WriteLine("[{0}] {1} - {2}", issue.Level, issue.Title, issue.Description));
                Console.ResetColor();

                RippleAssert.Fail("Symbols package failed validation");
            }
        }

        public string CreatePackage(PackageParams ctx)
        {
            var builder = packageBuilderFor(ctx.Spec, ctx.Version);

            if (ctx.CreateSymbols)
            {
                ExcludeFilesForLibPackage(builder.Files);
                if (!builder.Files.Any())
                {
                    throw new CreateNugetException(builder.Id);
                }
            }

            var nupkgFileName = Path.Combine(ctx.OutputPath, "{0}.{1}.nupkg".ToFormat(ctx.Spec.Name, ctx.Version));

            var package = createPackage(builder, nupkgFileName);

            if (ctx.CreateSymbols)
            {
                CreateSymbolsPackage(ctx);
            }
            var issues = package.Validate(Rules);

            if (issues.Any(x => x.Level == PackageIssueLevel.Error))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                issues.Each(issue => Console.WriteLine("[{0}] {1} - {2}", issue.Level, issue.Title, issue.Description));
                Console.ResetColor();

                RippleAssert.Fail("Package failed validation");
            }

            return nupkgFileName;
        }

        public IPublishReportItem PublishPackage(string serverUrl, string file, string apiKey)
        {
            var packageServer = new PackageServer(serverUrl, "ripple");
            var package = new OptimizedZipPackage(file);

            RippleLog.Info("Publishing " + file + " with " + apiKey);

            try
            {
	            var info = new FileInfo(file);
                packageServer.PushPackage(apiKey, package, info.Length, (int) 60.Minutes().TotalMilliseconds, true);
                return new PublishSuccessful(package);
            }
            catch (Exception ex)
            {
                // TODO -- Hate this
                if (ex.Message.Contains("exists"))
                {
                    return new VersionAlreadyExists(package);
                }

                return new PublishFailure(package, ex);

            }
        }

        private IPackage createPackage(PackageBuilder builder, string outputPath)
        {
            bool isExistingPackage = File.Exists(outputPath);
            try
            {
                using (Stream stream = File.Create(outputPath))
                {
                    builder.Save(stream);
                }
            }
            catch (Exception exc)
            {
                if (!isExistingPackage && File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                RippleAssert.Fail("Error creating package: " + exc.Message);
            }

            RippleLog.Info("Created nuget at: " + outputPath);
            return new OptimizedZipPackage(outputPath);
        }

        private PackageBuilder symbolsPackageBuilderFor(NugetSpec spec, SemanticVersion version)
        {
            return packageBuilderFor(spec, version);
        }

        private PackageBuilder packageBuilderFor(NugetSpec spec, SemanticVersion version)
        {
            try
            {
                return new PackageBuilder(spec.Filename, NullPropertyProvider.Instance, true)
                    {
                        Version = version
                    };
            }
            catch (Exception exc)
            {
                RippleAssert.Fail("Error creating package: " + exc.Message);
                return null;
            }
        }
    }
}
using System;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml.XPath;

namespace ripple.MSBuild
{
    public class FrameworkNameDetector
    {
        public const string DefaultIdentifier = ".NETFramework";
        public const string DefaultFrameworkVersion = "v4.0";

        public static FrameworkName Detect(ProjFile project)
        {
            var groups = project.Document.XPathSelectElements("tns:PropertyGroup", ProjFile.Manager).ToArray();

            var identifier = groups.Get("TargetFrameworkIdentifier", DefaultIdentifier);
            var versionString = groups.Get("TargetFrameworkVersion", DefaultFrameworkVersion);
            var profile = groups.Get("TargetFrameworkProfile");
            var version = Version.Parse(versionString.Replace("v", "").Replace("V", ""));

            return new FrameworkName(identifier, version, profile);
        }
    }
}
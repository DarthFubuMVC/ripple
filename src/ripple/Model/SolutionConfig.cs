using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using FubuCore;
using System.Linq;

namespace ripple.Model
{
    [XmlType("ripple")]
    public class SolutionConfig
    {
        public static readonly string FileName = "ripple.config";

        public SolutionConfig()
        {
            NugetSpecFolder = "packaging/nuget";
            SourceFolder = "src";
            BuildCommand = "rake";
            FastBuildCommand = "rake compile";
        } 

        public string Name { get; set; }
        public string NugetSpecFolder { get; set; }
        public string SourceFolder { get; set; } // look for packages.config underneath this


        public string BuildCommand { get; set; }
        public string FastBuildCommand { get; set; }

        private readonly IList<string> _floats = new List<string>();
        private readonly IList<Feed> _feeds = new List<Feed>();

        public void FloatNuget(string nugetName)
        {
            _floats.Fill(nugetName);
        }

        public void LockNuget(string nugetName)
        {
            _floats.Remove(nugetName);
        }

        public string[] Floats
        {
            get
            {
                return _floats.ToArray();
            }
            set
            {
                _floats.Clear();
                _floats.AddRange(value);
            }
        }

        public Feed[] Feeds
        {
            get { return _feeds.ToArray(); }
            set
            {
                _feeds.Clear();
                _feeds.AddRange(value);
            }
        }

        public UpdateMode ModeForNuget(string nugetName)
        {
            return _floats.Contains(nugetName) ? UpdateMode.Float : UpdateMode.Fixed;
        }

        public static SolutionConfig LoadFrom(string directory)
        {
            var fileSystem = new FileSystem();
            var file = directory.AppendPath(FileName);

            return fileSystem.FileExists(file) 
                ? fileSystem.LoadFromFile<SolutionConfig>(file) 
                : null;
            
            
        }

        public string GetSolutionFolder(string directory)
        {
            var source = SourceFolder ?? "src";
            return directory.AppendPath(source);
        }
    }
}
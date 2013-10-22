using System;
using System.Collections.Generic;
using System.IO;
using FubuCore;
using FubuCore.Descriptions;
using ripple.MSBuild;

namespace ripple.Model
{
    public class Project : DescribesItself
    {
        private readonly DependencyCollection _dependencies = new DependencyCollection();
        private readonly IList<Project> _references = new List<Project>();
        private readonly Lazy<ProjFile> _proj;

        public Project(string filePath)
        {
            FilePath = filePath;
            Name = Path.GetFileNameWithoutExtension(filePath);
            Directory = filePath.DirectoryPath();

            _proj = new Lazy<ProjFile>(() => new ProjFile(filePath, Solution));
        }

        public string Name { get; private set; }
        public string Directory { get; private set; }
        public string FilePath { get; private set; }
        public ProjFile Proj { get { return _proj.Value; } }
        public Solution Solution { get; set; }

        // Mostly for testing
        public bool HasProjFile()
        {
            return File.Exists(FilePath);
        }

        public DependencyCollection Dependencies
        {
            get { return _dependencies; }
        }

        public IEnumerable<Project> References { get { return _references; } }

        public bool HasChanges()
        {
            return _dependencies.HasChanges();
        }

        public void AddProjectReference(Project project)
        {
            _references.Fill(project);
        }

        public void AddDependency(Dependency dependency)
        {
            _dependencies.Add(dependency);
        }

        public void AddDependency(string name)
        {
            AddDependency(new Dependency(name));
        }

        protected bool Equals(Project other)
        {
            return string.Equals(FilePath, other.FilePath);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Project)obj);
        }

        public override int GetHashCode()
        {
            return FilePath.GetHashCode();
        }

        public void Clean(IFileSystem system)
        {
            system.CleanWithTracing(Directory.AppendPath("bin"));
            system.CleanWithTracing(Directory.AppendPath("obj"));
        }

        public void RemoveDuplicateReferences()
        {
            Proj.RemoveDuplicateReferences();
        }

        public void Describe(Description description)
        {
            description.Title = "Project \"{0}\"".ToFormat(Name);
            description.ShortDescription = FilePath;

            var list = description.AddList("Dependencies", Dependencies);
            list.Label = "Dependencies";
        }
    }
}
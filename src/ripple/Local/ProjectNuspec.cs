﻿using System.Collections.Generic;
using System.Linq;
using ripple.Model;

namespace ripple.Local
{
    public class ProjectNuspec
    {
        private readonly Project _project;
        private readonly NugetSpec _spec;

        public ProjectNuspec(Project project, NugetSpec spec)
        {
            _project = project;
            _spec = spec;
        }

        public Project Project
        {
            get { return _project; }
        }

        public NugetSpec Spec
        {
            get { return _spec; }
        }

        protected bool Equals(ProjectNuspec other)
        {
            return _project.Equals(other._project) && _spec.Equals(other._spec);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ProjectNuspec) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (_project.GetHashCode()*397) ^ _spec.GetHashCode();
            }
        }

        public IEnumerable<Dependency> DetermineDependencies()
        {
            return Project
                .Dependencies
                .Where(x => !_spec.Dependencies.Any(y => y.MatchesName(x.Name)))
                .Select(x => _project.Solution.FindDependency(x.Name));
        }
    }
}
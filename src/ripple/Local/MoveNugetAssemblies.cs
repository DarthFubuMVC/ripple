using System;
using System.Collections.Generic;
using System.IO;
using FubuCore;
using ripple.Model;

namespace ripple.Local
{
    
    public class MoveNugetAssemblies : IRippleStep
    {
        private readonly NugetSpec _nuget;
        private readonly Solution _destination;

        public MoveNugetAssemblies(NugetSpec nuget, Solution destination)
        {
            _nuget = nuget;
            _destination = destination;
        }

        public bool Equals(MoveNugetAssemblies other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other._nuget, _nuget) && Equals(other._destination, _destination);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof (MoveNugetAssemblies)) return false;
            return Equals((MoveNugetAssemblies) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((_nuget != null ? _nuget.GetHashCode() : 0)*397) ^ (_destination != null ? _destination.GetHashCode() : 0);
            }
        }

        // Tested manually.
        public void Execute(IRippleStepRunner runner)
        {
	        string packageFolder;
	        try
	        {
				 packageFolder = _destination.NugetFolderFor(_nuget);
	        }
	        catch (ArgumentOutOfRangeException exc)
	        {
				RippleLog.Error(ToString(), exc);
		        throw;
	        }
            Directory.GetDirectories(packageFolder).Each(runner.CleanDirectory);
            _nuget.PublishedAssemblies.Each(x =>
            {
                var request = new FileCopyRequest{
                    From = x.Directory,
                    Matching = new FileSet{Include = x.Pattern},
                    To = packageFolder.AppendPath(x.SubFolder.Replace('/', Path.DirectorySeparatorChar))
                };
                
                runner.CopyFiles(request);
            });
        }

        public override string ToString()
        {
            return string.Format("Move {0} to {1}", _nuget, _destination);
        }


    }
}
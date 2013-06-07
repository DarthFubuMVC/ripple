using FubuCore;
using System.Linq;

namespace ripple.MSBuild
{
    public class Reference
    {
	    public string Name { get; set; }
        public string HintPath { get; set; }
        public string Aliases { get; set; }

        public override string ToString()
        {
            return string.Format("Name: {0}, HintPath: {1}", Name, HintPath);
        }

        public bool Matches(string assemblyName)
        {
            if (Name.EqualsIgnoreCase(assemblyName)) return true;

            var guessedName = Name.Split(',').First().Trim();

            return assemblyName.EqualsIgnoreCase(guessedName);
        }

		protected bool Equals(Reference other)
		{
		    return string.Equals(Name, other.Name) && string.Equals(HintPath, other.HintPath) && string.Equals(Aliases, other.Aliases);
		}

		public override bool Equals(object obj)
		{
		    if (ReferenceEquals(null, obj)) return false;
		    if (ReferenceEquals(this, obj)) return true;
		    if (obj.GetType() != this.GetType()) return false;
		    return Equals((Reference) obj);
		}

		public override int GetHashCode()
		{
		    unchecked
		    {
		        var hashCode = (Name != null ? Name.GetHashCode() : 0);
		        hashCode = (hashCode*397) ^ (HintPath != null ? HintPath.GetHashCode() : 0);
		        return (hashCode*397) ^ (Aliases != null ? Aliases.GetHashCode() : 0);
		    }
		}
    }
}
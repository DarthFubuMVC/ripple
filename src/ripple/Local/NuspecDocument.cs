using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FubuCore;

namespace ripple.Local
{
    public class NuspecDocument
    {
        private const string EdgeSuffix = "-Edge";
        public const string Schema = "http://schemas.microsoft.com/packaging/2010/07/nuspec.xsd";
        private static readonly XmlNamespaceManager _xmlNamespaceManager;
        private static readonly XNamespace _xmlns;

        static NuspecDocument()
        {
            var nameTable = new NameTable();

            _xmlNamespaceManager = new XmlNamespaceManager(nameTable);
            _xmlNamespaceManager.AddNamespace("nuspec", Schema);

            _xmlns = Schema;
        }

        private readonly string _filename;
        private readonly XElement _document;

        public NuspecDocument(string filename)
        {
            _filename = filename;

            _document = XElement.Load(filename);
            _document.Name = _xmlns + _document.Name.LocalName;
        }

        public void SaveChanges()
        {
            _document.Save(_filename);
        }

        private XElement findNugetElement(string name)
        {
            var search = "//nuspec:{0}".ToFormat(name);
            return _document.XPathSelectElement(search, _xmlNamespaceManager);
        }

        private IEnumerable<XElement> findNugetElements(string name)
        {
            var search = "//nuspec:{0}".ToFormat(name);
            foreach (XElement element in _document.XPathSelectElements(search, _xmlNamespaceManager))
            {
                yield return element;
            }
        }

        public void AddDependency(NuspecDependency dependency)
        {
            var dependencies = _document.XPathSelectElement("//nuspec:dependencies", _xmlNamespaceManager);
            var element = _document.XPathSelectElement(string.Format("//nuspec:dependencies/nuspec:dependency[@id='{0}']", dependency.Name), _xmlNamespaceManager);
            if (element == null)
            {
                element = new XElement(_xmlns + "dependency");
                element.SetAttributeValue("id", dependency.Name);
                dependencies.Add(element);
            }

            if (dependency.VersionSpec != null)
            {
                element.SetAttributeValue("version", dependency.VersionSpec.ToString());
            }
            else
            {
                if (element.Attribute("version") != null)
                    element.Attribute("version").Remove();
            }

        }

		public void AddPublishedAssembly(string src, string target = "lib")
		{
			var files = _document.XPathSelectElement("//files", _xmlNamespaceManager);

            var element = new XElement("file");
			element.SetAttributeValue("src", src);
			element.SetAttributeValue("target", target);

			files.Add(element);
		}

        public IEnumerable<NuspecDependency> FindDependencies()
        {
            return findNugetElements("dependency").Select(NuspecDependency.ReadFrom);
        }

        public IEnumerable<PublishedAssembly> FindPublishedAssemblies()
        {
            var nuspecDirectory = _filename.ParentDirectory();

            foreach (var element in _document.XPathSelectElements("//file", _xmlNamespaceManager))
            {
                var target = element.Attribute("target").Value;
                if (target.StartsWith("lib") || target.StartsWith("tools"))
                {
                    var path = target.Replace('\\', '/');
                    var assemblyReference = element.Attribute("src").Value;

                    yield return new PublishedAssembly(nuspecDirectory, assemblyReference, path);
                }
            }
        }


        public string Name
        {
            get
            {
                return findNugetElement("id").Value;
            }
            set
            {
                findNugetElement("id").Value = value;
            }
        }

        public void MakeEdge()
        {
            if (Name.EndsWith(EdgeSuffix)) return;

            Console.WriteLine("Changing nuspec file at {0} to '-Edge' mode", _filename);

            Name = ChangeNameToEdge(Name);
            SaveChanges();
        }

        public void MakeRelease()
        {
            if (!Name.EndsWith(EdgeSuffix))
            {
                return;
            }

            Console.WriteLine("Changing nuspec file at {0} to release mode", _filename);

            Name = ChangeNameToRelease(Name);
            SaveChanges();
        }

        public static string ChangeNameToRelease(string name)
        {
            if (!name.EndsWith(EdgeSuffix))
            {
                return name;
            }

            return name.Substring(0, name.Length - EdgeSuffix.Length);
        }

        public static string ChangeNameToEdge(string name)
        {
            if (name.EndsWith(EdgeSuffix)) return name;

            return name + EdgeSuffix;
        }

        public void SetVersion(string dependency, string version)
        {
            var element = findNugetElement("dependency[@id='" + dependency + "']");
            if (element == null)
            {
                throw new InvalidOperationException("Unable to find dependency " + dependency);
            }

            element.SetAttributeValue("version", version);
        }

        public void ClearAllDependencies()
        {
            var dependencies = _document.XPathSelectElement("//nuspec:dependencies", _xmlNamespaceManager);
            dependencies.RemoveAll();
        }
    }
}
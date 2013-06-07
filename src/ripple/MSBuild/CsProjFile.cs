using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using FubuCore;
using FubuCore.Util;
using NuGet;
using ripple.Model;

namespace ripple.MSBuild
{
    public class CsProjFile
    {
        public const string Schema = "http://schemas.microsoft.com/developer/msbuild/2003";
        private static readonly XmlNamespaceManager _manager;
		private static readonly XNamespace _xmlns;
        private readonly XElement _document;
        private readonly string _filename;
        private readonly Solution _solution;

        private readonly Lazy<IList<Reference>> _references;
        private readonly Lazy<IList<string>> _projectReferences; 

        static CsProjFile()
        {
            _manager = new XmlNamespaceManager(new NameTable());
            _manager.AddNamespace("tns", Schema);

	        _xmlns = Schema;
        }

        public CsProjFile(string filename, Solution solution)
        {
            _filename = filename;
            _solution = solution;

	        try
	        {
		        _document = XElement.Load(filename);
		        _document.Name = _xmlns + _document.Name.LocalName;

		        _references = new Lazy<IList<Reference>>(() => new List<Reference>(readReferences()));
		        _projectReferences = new Lazy<IList<string>>(() => new List<string>(readProjectReferences()));
	        }
	        catch (Exception ex)
	        {
		        throw new RippleFatalError("Error reading csproj file: {0}".ToFormat(filename), ex);
	        }
        }

        public string ToolsVersion
        {
            get { return _document.Attribute(XName.Get("ToolsVersion")).Value; }
        }

        public IEnumerable<Reference> References
        {
            get { return _references.Value; }
        }

        public IEnumerable<string> ProjectReferences
        {
            get { return _projectReferences.Value;  }
        }

        public ReferenceStatus AddReference(string name, string hintPath)
        {
			if (hintPath.IsNotEmpty())
			{
				hintPath = hintPath.Trim();
			}

            Reference reference = FindReference(name);
            if (reference == null)
            {
                reference = new Reference {Name = name};
                _references.Value.Add(reference);
            }

            string original = reference.HintPath;
            reference.HintPath = hintPath;

			if (original.IsNotEmpty())
			{
				original = original.Trim();
			}

			var status = string.Equals(original, hintPath, StringComparison.OrdinalIgnoreCase) ? ReferenceStatus.Unchanged : ReferenceStatus.Changed;
			if (status == ReferenceStatus.Changed)
			{
				RippleLog.Info("HintPath changed: " + original + " to " + hintPath);
			}

	        return status;
        }

        public Reference FindReference(string name)
        {
            return _references.Value.FirstOrDefault(x => x.Name == name);
        }

        public bool RemoveReference(string name)
        {
            Reference reference = FindReference(name);
            if (reference == null) return false;

            _references.Value.Remove(reference);

            return true;
        }

        public bool UsesPackagesConfig()
        {
            return FindPackagesConfigItem() != null;
        }

        public void ConvertToRippleDependenciesConfig()
        {
            var item = FindPackagesConfigItem();
            if (item == null)
            {
                throw new InvalidOperationException("Could not find packages.config reference");
            }

            item.SetAttributeValue("Include", "ripple.dependencies.config");
        }

        public XElement FindPackagesConfigItem()
        {
            foreach (var none in _document.XPathSelectElements("tns:ItemGroup/tns:None", _manager))
            {
                if (none.Attribute("Include").Value == "packages.config")
                {
                    return none;
                }
            }

            foreach (var content in _document.XPathSelectElements("tns:ItemGroup/tns:Content", _manager))
            {
                if (content.Attribute("Include").Value == "packages.config")
                {
                    return content;
                }
            }

            return null;
        }

	    public bool RemoveReferences(IEnumerable<string> references)
	    {
		    return references.All(RemoveReference);
	    }

	    public void Write()
        {
            if (_references.IsValueCreated)
            {
                var nodes = FindReferenceNodes().ToList();
                foreach (var node in nodes)
                {
					node.Remove();
                }

                var itemGroup = _document.XPathSelectElement("tns:ItemGroup", _manager);
				itemGroup.Name = _xmlns + itemGroup.Name.LocalName;

                _references.Value.OrderBy(x => x.Name).Each(reference => {

					var node = new XElement(_xmlns + "Reference");
					node.SetAttributeValue("Include", reference.Name);

                    if (reference.HintPath.IsNotEmpty())
                    {
						var hintPath = new XElement(_xmlns + "HintPath");
                        hintPath.Value = reference.HintPath;
                        node.Add(hintPath);
                    }

                    itemGroup.Add(node);
                });
            }

            _document.Save(_filename);
        }

        private IEnumerable<string> readProjectReferences()
        {
            var references = _document.XPathSelectElements("tns:ItemGroup/tns:ProjectReference", _manager);
            foreach (var reference in references)
            {
                var name = reference.XPathSelectElement("tns:Name", _manager);
                if (name != null)
                {
                    yield return name.Value;
                }
            }
        }

        private IEnumerable<Reference> readReferences()
        {
            var nodes = FindReferenceNodes();
            foreach (var node in nodes)
            {
                var reference = new Reference
                {
					Name = node.Attribute("Include").Value
                };

                foreach (var child in node.Elements())
                {
					switch (child.Name.LocalName)
                    {
                        case "HintPath":
                            reference.HintPath = child.Value;
                            break;
                    }
                }

                yield return reference;
            }
        }

        public IEnumerable<XElement> FindReferenceNodes()
        {
	        return _document.XPathSelectElements("tns:ItemGroup/tns:Reference", _manager);
        }

        public override string ToString()
        {
            return string.Format("Filename: {0}", _filename);
        }

		public void AddAssemblies(Dependency dep, IEnumerable<IPackageAssemblyReference> assemblies)
        {
            bool needsSaved = false;

            assemblies = GetCompatibleItemsCore(assemblies).ToList();

            assemblies.Each(assem => 
            {
                string assemblyName = Path.GetFileNameWithoutExtension(assem.Name);

                if (assemblyName == "_._") return;
                
                var nugetDir = _solution.NugetFolderFor(dep.Name);
                var assemblyPath = nugetDir.AppendPath(assem.Path);

                var hintPath = assemblyPath.PathRelativeTo(_filename.ParentDirectory());

                if (AddReference(assemblyName, hintPath) == ReferenceStatus.Changed)
                {
                    Console.WriteLine("Updated reference for {0} to {1}", _filename, hintPath);
                    needsSaved = true;
                }
            });

            if (needsSaved)
            {
                Console.WriteLine("Writing changes to " + _filename);
                Write();
            }
        }

		public void RemoveDuplicateReferences()
		{
			var references = References.Select(x =>
			{
				var name = x.Name;
				if (name.Contains(","))
				{
					name = name.Split(',').First();
				}

				return name;
			});

			var counts = new Cache<string, List<Reference>>(x => new List<Reference>());
			references.Each(dependency =>
			{
				var duplicates = References.Where(x => x.Matches(dependency));
				counts[dependency].Fill(duplicates);
			});

			var removals = new List<string>();
			counts
				.Where(x => x.Count > 1)
				.Each(duplicates =>
				{
					// Naive but it works for now
					duplicates.Where(x => x.Name.Contains(",")).Each(x => removals.Add(x.Name));
				});

			RemoveReferences(removals);
		}

        internal static IEnumerable<T> GetCompatibleItemsCore<T>(IEnumerable<T> items) where T : IFrameworkTargetable
        {
            IEnumerable<T> compatibleItems;

            // TODO -- this obviously isn't good enough.
            if (VersionUtility.TryGetCompatibleItems(new FrameworkName(".NETFramework, Version=4.0"), items,
                                                     out compatibleItems))
            {
                return compatibleItems;
            }
            return Enumerable.Empty<T>();
        }
    }
}
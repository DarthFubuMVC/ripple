using NuGet;
using NUnit.Framework;
using ripple.Nuget;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FubuTestingSupport;
using System.Xml.Linq;

namespace ripple.Testing.Nuget
{
    public class NuspecDocumentTester
    {
        public NuspecDocument createDocument(string fileName, string id, string version)
        {
            XNamespace  ns = NuspecDocument.Schema;
            var xdoc = (new XDocument(new XElement(ns + "package", 
                new XElement(ns + "metadata",
                    new XElement(ns + "id",
                        new XText(id)
                     ),
                     new XElement(ns + "version",
                        new XText(version)
                     )
             ))));
            xdoc.Save(fileName);
            var doc = new NuspecDocument(fileName);
            return doc;
        }

        [Test]
        public void when_no_depency_add_depency_works()
        {
            var fileName = Path.GetTempFileName();
            var doc = createDocument(fileName, "Test", "1.2.1");
            doc.AddDependency(new NuspecDependency()
            {
                Name = "Test2",
                VersionSpec = new VersionSpec(SemanticVersion.Parse("1.0.0"))
            });
            doc.FindDependencies().ShouldContain(c => c.Name == "Test2");

            File.Delete(fileName);
        }
    }
}

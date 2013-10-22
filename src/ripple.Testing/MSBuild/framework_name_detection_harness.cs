using System.Runtime.Versioning;
using System.Xml.Linq;
using ripple.MSBuild;

namespace ripple.Testing.MSBuild
{
    public abstract class framework_name_detection_harness
    {
        protected abstract string theXml { get; }

        protected FrameworkName theFrameworkName
        {
            get
            {
                var element = XElement.Parse(theXml);
                var project = ProjFile.For(element);

                return FrameworkNameDetector.Detect(project);
            }
        }
    }
}
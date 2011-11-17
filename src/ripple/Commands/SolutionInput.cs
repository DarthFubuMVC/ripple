using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using FubuCore;
using ripple.Model;

namespace ripple.Commands
{
    public class SolutionInput
    {
        private readonly Lazy<SolutionGraph> _graph = new Lazy<SolutionGraph>(SolutionGraphBuilder.BuildForRippleDirectory);

        [Description("override the solution to be cleaned")]
        public string SolutionFlag { get; set; }

        [Description("Apply restore to all solutions")]
        public bool AllFlag { get; set; }

        public IEnumerable<Solution> FindSolutions()
        {
            if (SolutionFlag.IsNotEmpty())
            {
                yield return _graph.Value[SolutionFlag];
            }
            else if (AllFlag || ".".ToFullPath() == RippleFileSystem.CodeDirectory())
            {
                foreach (var solution in _graph.Value.AllSolutions)
                {
                    yield return solution;
                }
            }
            else
            {
                if (new FileSystem().FileExists(".".ToFullPath(), SolutionConfig.FileName))
                {
                    yield return Solution.ReadFrom(".");
                }                
            }
        }
    }
}
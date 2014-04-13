using System;

namespace ripple.Model
{
    using System.IO;

    public static class BranchDetector
    {
        private static Lazy<string> _current;
        private static Func<string> _detectCurrent;
        private static Func<bool> _canDetectGit;
        private static Func<bool> _canDetectHg; 
 
        static BranchDetector()
        {
            Live();
        }
        
        public static void Live()
        {
            _canDetectGit = () => Directory.Exists(GitDirectory);
            _canDetectHg = () => Directory.Exists(HgDirectory);
            _detectCurrent = () =>
            {
                var canDetectGitResult = _canDetectGit();
                var canDetectHgResult = _canDetectHg();
                if (!canDetectGitResult && !canDetectHgResult)
                {
                    RippleAssert.Fail("Cannot use branch detection when not in a git or hg repository");
                }

                if (canDetectGitResult)
                {
                    var head = File.ReadAllText(Path.Combine(GitDirectory, "HEAD"));
                    return head.Substring(head.LastIndexOf("/") + 1).Trim();
                }
                var hghead = File.ReadAllText(Path.Combine(HgDirectory, "branch"));
                return hghead.Trim('\r', '\n', ' ', '\t');
            };

            reset();
        }

        private static void reset()
        {
            _current = new Lazy<string>(() => _detectCurrent());
        }

        public static void Stub(Func<string> current)
        {
            _detectCurrent = current;
            reset();
        }

        public static void Stub(Func<bool> canDetectGit, Func<bool> canDetectHg)
        {
            _canDetectGit = canDetectGit;
            _canDetectHg = canDetectHg;
        }

        public static void SetBranch(string branch)
        {
            _canDetectGit = () => true;
            _detectCurrent = () => branch;
        }

        public static bool CanDetectBranch()
        {
            return _canDetectGit() || _canDetectHg();
        }

        public static string Current()
        {
            return _current.Value;
        }

        private static string GitDirectory
        {
            get
            {
                return Path.Combine(RippleFileSystem.FindSolutionDirectory(false) ?? "", ".git");
            }
        }

        private static string HgDirectory
        {
            get
            {
                return Path.Combine(RippleFileSystem.FindSolutionDirectory(false) ?? "", ".hg");
            }
        }
    }
}

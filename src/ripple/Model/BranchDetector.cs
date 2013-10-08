using System;

namespace ripple.Model
{
    using System.IO;

    public static class BranchDetector
    {
        private static Lazy<string> _current;
        private static Func<string> _detectCurrent;
        private static Func<bool> _canDetect;

        static BranchDetector()
        {
            Live();
        }

        public static void Live()
        {
            _canDetect = () => Directory.Exists(GitDirectory);
            _detectCurrent = () =>
            {
                if (!_canDetect())
                {
                    RippleAssert.Fail("Cannot use branch detection when not in a git repository");
                }

                var head = File.ReadAllText(Path.Combine(GitDirectory,"HEAD"));

                return head.Substring(head.LastIndexOf("/") + 1).Trim();
            };

            reset();
        }

        private static void reset()
        {
            _current = new Lazy<string>(_detectCurrent);
        }

        public static void Stub(Func<string> current)
        {
            _detectCurrent = current;
            reset();
        }

        public static void Stub(Func<bool> canDetect)
        {
            _canDetect = canDetect;
        }

        public static bool CanDetectBranch()
        {
            return _canDetect();
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
    }
}
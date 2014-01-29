using System;
using System.Diagnostics;
using System.IO;
using FubuCore;
using FubuCore.CommandLine;
using ripple.Local;

namespace ripple.Runners
{
    public class Runner
    {
        public static readonly Runner Git = new Runner("run-git.cmd", "run-git.sh");
        public static readonly Runner Rake = new Runner("run-rake.cmd", "run-rake.sh");

        private readonly string _windows;
        private readonly string _unix;

        public Runner(string windows, string unix)
        {
            _windows = windows;
            _unix = unix;
        }

        public string Path
        {
            get
            {
                return RippleFileSystem
                    .RippleExeLocation()
                    .ParentDirectory()
                    .AppendPath(PlatformTarget)
                    .ToFullPath();
            }
        }

        public ProcessStartInfo Info(string command, params object[] parameters)
        {
            var location = Path;

            if (!File.Exists(location))
            {
                ExplodeTo(location);
            }

            if (Platform.IsUnix ()) 
            {
                return new ProcessStartInfo
                {
                    FileName = "/bin/bash",
                    Arguments = location + " " + command.ToFormat(parameters)
                };
            }


            return new ProcessStartInfo
            {
                FileName = location,
                Arguments = command.ToFormat(parameters)
            };
        }

        public string PlatformTarget
        {
            get
            {
                return Platform.IsUnix() ? _unix : _windows;
            }
        }

        public void ExplodeTo(string path)
        {
            var stream = GetType()
                .Assembly
                .GetManifestResourceStream(GetType(), PlatformTarget);

            new FileSystem().WriteStreamToFile(path, stream);
        }

        public bool Run(string command, params object[] parameters)
        {
            var runner = new ProcessRunner();
            var start = Console.ForegroundColor;
            var processStartInfo = Info(command, parameters);


            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine();

            ConsoleWriter.PrintHorizontalLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("{0} {1}", processStartInfo.FileName, processStartInfo.Arguments);
            ConsoleWriter.PrintHorizontalLine();


            var returnValue = runner.Run(processStartInfo, new TimeSpan(0, 1, 0), text => { });
            var color = returnValue.ExitCode == 0 ? ConsoleColor.Gray : ConsoleColor.Red;


            Console.ForegroundColor = color;


            Console.WriteLine(returnValue.OutputText);
            Console.WriteLine("ExitCode:  " + returnValue.ExitCode);

            ConsoleWriter.PrintHorizontalLine();

            Console.ForegroundColor = start;

            return returnValue.ExitCode == 0;
        }
    }
}
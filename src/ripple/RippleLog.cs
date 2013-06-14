using System;
using System.Collections.Generic;
using FubuCore;
using FubuCore.Descriptions;
using FubuCore.Logging;

namespace ripple
{
    public class RippleFatalError : Exception
    {
        public RippleFatalError(string message) : base(message) { }
		public RippleFatalError(string message, Exception ex) : base(message,ex) { }
    }

    public class RippleAssert
    {
        public static void Fail(string message)
        {
            RippleLog.Error(message);
            throw new RippleFatalError(message);
        }
    }

	public class RippleLog
	{
		private static Lazy<ILogger> _logger;
		private static readonly IList<ILogListener> Listeners;
	    private static readonly ILogListener File;

		static RippleLog()
		{
			Listeners = new List<ILogListener>();

			RegisterListener(new RippleLogger());
			File = new FileListener();
            
            AddFileListener();
		}

        private static void resetLogger()
        {
            _logger = new Lazy<ILogger>(() => new Logger(Listeners, new ILogModifier[0]));
        }

        public static void RemoveFileListener()
        {
            Listeners.Remove(File);
            resetLogger();
        }

        public static void AddFileListener()
        {
            RegisterListener(File);
        }

		public static void Verbose(bool verbose)
		{
			RippleLogger.PrintDebug = verbose;
		}

		private static ILogger Logger { get { return _logger.Value; } }

		public static void RegisterListener(ILogListener listener)
		{
			Listeners.Add(listener);
            resetLogger();
		}

		public static void DebugMessage(LogTopic message)
		{
			Logger.DebugMessage(message);
		}

		public static void InfoMessage(LogTopic message)
		{
			Logger.InfoMessage(message);
		}

		public static void Debug(string message)
		{
			Logger.Debug(message);
		}

		public static void Info(string message)
		{
			Logger.Info(message);
		}

        public static void Error(string message)
        {
            Error(message, new RippleFatalError(message));
        }

		public static void Error(string message, Exception ex)
		{
            withConsoleColor(ConsoleColor.Red, () => Console.WriteLine("ripple: " + message));
			Logger.Error(message, ex);
		}

        private static void withConsoleColor(ConsoleColor color, Action action)
        {
            Console.ForegroundColor = color;
            action();
            Console.ResetColor();
        }

		public class RippleLogger : ILogListener
		{
			public static bool PrintDebug = false;
			public static bool PrintInfo = true;

			public bool ListensFor(Type type)
			{
				return true;
			}

			public void Debug(string message)
			{
				Console.WriteLine(message);
			}

			public void DebugMessage(object message)
			{
				Debug(message.ToDescriptionText());
			}

			public void Info(string message)
			{
				writeWithColor(ConsoleColor.Cyan, () => Console.WriteLine(message));
			}

			public void InfoMessage(object message)
			{
				Info(message.ToDescriptionText());
			}

			public void Error(string message, Exception ex)
			{
			}

			public void Error(object correlationId, string message, Exception ex)
			{
			}

			private void writeWithColor(ConsoleColor color, Action action)
			{
				Console.ForegroundColor = color;
				action();
				Console.ResetColor();
			}

			public bool IsDebugEnabled { get { return PrintDebug; } }
			public bool IsInfoEnabled { get { return PrintInfo; } }
		}

        public class FileListener : ILogListener
        {
            public const string File = "ripple.log";

            private readonly IFileSystem _fileSystem = new FileSystem();

            public bool ListensFor(Type type)
            {
                return true;
            }

            public void DebugMessage(object message)
            {
                Debug(message.ToDescriptionText());
            }

            public void InfoMessage(object message)
            {
                Info(message.ToDescriptionText());
            }

            public void Debug(string message)
            {
                write("Debug", message);
            }

            public void Info(string message)
            {
                write("Info", message);
            }

            public void Error(string message, Exception ex)
            {
                write("Error", message);
                write("Error", ex.ToString());
            }

            public void Error(object correlationId, string message, Exception ex)
            {
                Error(message, ex);
            }

            private void write(string level, string message)
            {
                if (!RippleFileSystem.IsSolutionDirectory())
                {
                    return;
                }

                var log = "{0}: [{1}] {2}{3}".ToFormat(DateTime.Now.ToString(), level, message, Environment.NewLine);
                _fileSystem.AppendToLogFile(File, log);
            }

            public bool IsDebugEnabled { get { return true; } }
            public bool IsInfoEnabled { get { return true; } }
        }
	}
}
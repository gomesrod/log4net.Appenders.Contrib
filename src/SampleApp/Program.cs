using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using log4net.Config;

namespace log4net.Appenders.Contrib.SampleApp
{
	static class Program
	{
		static void Main(string[] args)
		{
			try
			{
				XmlConfigurator.Configure();

				var logJson = LogManager.GetLogger("RemoteSyslog5424_Json");
				var logPlain = LogManager.GetLogger("RemoteSyslog5424_Plain");

				AppDomain.CurrentDomain.UnhandledException +=
					(sender, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject.ToString());

				Console.WriteLine("Writing logs...\n\n");

				var logs = new List<string>
				{
					"I'm broken. Please show this to someone who can fix me.",
					"An error has occured on the error logging device.",
					"Error ocurred when attempting to print error message."
				};

				for (var i = 0; i < logs.Count; i++)
				{
					var id = string.Format("{0}_{1}", i, Guid.NewGuid());
					var message = logs[i] + " " + id;

					logJson.Error(
						new {
							Message = message,
							Id = id,
						});

					logPlain.Error(message);
				}

				Console.WriteLine("\n\nPress a key to exit.");
				Console.ReadKey();

				RemoteSyslog5424Appender.Flush("RemoteAppenderJson");
				RemoteSyslog5424Appender.Flush("RemoteAppenderPlain");
			}
			catch (Exception exc)
			{
				Console.WriteLine(exc);
				if (Debugger.IsAttached)
					Debugger.Break();
			}
		}
	}
}

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

				var log = LogManager.GetLogger(typeof(Program));

				AppDomain.CurrentDomain.UnhandledException +=
					(sender, eventArgs) => Console.WriteLine(eventArgs.ExceptionObject.ToString());

				Console.WriteLine("Writing logs...\n\n");

				var logs = new List<string> { "I’m broken. Please show this to someone who can fix can fix", "An error has occured on the error logging device.", "Error ocurred when attempting to print error message." };
				
				for (var i = 0; i < 3; i++)
				{
				 log.ErrorFormat("{0} ({1}_{2})", logs[i], i, Guid.NewGuid());
				}

				Console.WriteLine("\n\nPress a key to exit.");
				Console.ReadKey();

				log.Logger.Repository.Shutdown();
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

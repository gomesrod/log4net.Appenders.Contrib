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

				for (var i = 0; i < 3; i++)
				{
					var message = i + "_" + Guid.NewGuid();
					log.Info(message);
				}

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

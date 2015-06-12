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

				var message = Guid.NewGuid().ToString();
				var log = LogManager.GetLogger(typeof(Program));
				log.Info(message);
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

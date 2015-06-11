using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using log4net.Config;
using log4net.Layout;
using NUnit.Framework;

namespace log4net.Appenders.Contrib.UnitTests
{
	class RemoteSyslog5424AppenderTest
	{
		[Test]
		public static void TestSimpleAppending()
		{
			var time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss+00:00");
			var id = Guid.NewGuid().ToString();
			var message = string.Format("94 <11>1 {0} {1} {2}", time, typeof(RemoteSyslog5424AppenderTest).FullName, id);

			var layout = new PatternLayout("%.255message%newline");
			layout.ActivateOptions();

			using (var appender = new RemoteSyslog5424Appender(
				TestSettings.Server, TestSettings.Port, TestSettings.CertificatePath) { Layout = layout })
			{
				BasicConfigurator.Configure(appender);
				var log = LogManager.GetLogger(typeof(RemoteSyslog5424AppenderTest));

				log.Info(message);
			}
		}
	}
}

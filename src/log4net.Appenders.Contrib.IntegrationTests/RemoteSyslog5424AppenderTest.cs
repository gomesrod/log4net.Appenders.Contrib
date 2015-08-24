using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using log4net.Config;
using log4net.Layout;
using NUnit.Framework;

namespace log4net.Appenders.Contrib.IntegrationTests
{
	class RemoteSyslog5424AppenderTest
	{
		[SetUp]
		public void SetUp()
		{
			_server.Start(Port, @"Certificate\test.pfx");
		}

		[TearDown]
		public void TearDown()
		{
			_server.Dispose();
		}

		[Test]
		public void TestConnectionInterruption()
		{
			var layout = new PatternLayout("%.255message");
			layout.ActivateOptions();

			using (var appender = new RemoteSyslog5424Appender("localhost", Port, @"Certificate\test.cer"))
			{
				appender.Layout = layout;
				appender.AppName = typeof(RemoteSyslog5424AppenderTest).Name;
				appender.ActivateOptions();

				BasicConfigurator.Configure(appender);
				var log = LogManager.GetLogger(typeof(RemoteSyslog5424AppenderTest));

				var i = 0;
				for (; i < 3; i++)
					log.Info(FormatMessage(i));

				Thread.Sleep(TimeSpan.FromSeconds(6));
				_server.CloseConnections();

				for (; i < 6; i++)
					log.Info(FormatMessage(i));

				Thread.Sleep(TimeSpan.FromSeconds(110));
			}
		}

		private static string FormatMessage(int i)
		{
			var message = i + "_" + Guid.NewGuid();
			return message;
		}

		readonly MockServer _server = new MockServer();
		private const int Port = 44344;
	}
}

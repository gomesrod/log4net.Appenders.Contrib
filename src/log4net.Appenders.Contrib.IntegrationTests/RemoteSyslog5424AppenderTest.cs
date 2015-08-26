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
			CreateAppender();
		}

		[TearDown]
		public void TearDown()
		{
			if (_appender != null)
			{
				_appender.Dispose();
				_appender = null;
			}

			_server.Dispose();
		}

		[Test]
		public void TestConnectionInterruption()
		{
			var i = 0;
			for (; i < 3; i++)
				_log.Info(FormatMessage(i));

			Thread.Sleep(TimeSpan.FromSeconds(6));
			_server.CloseConnections();

			for (; i < 6; i++)
				_log.Info(FormatMessage(i));

			Thread.Sleep(TimeSpan.FromSeconds(10));
		}

		private static string FormatMessage(int i)
		{
			var message = i + "_" + Guid.NewGuid();
			return message;
		}

		void CreateAppender()
		{
			var layout = new PatternLayout("%.255message");
			layout.ActivateOptions();

			var appender = new RemoteSyslog5424Appender("localhost", Port, @"Certificate\test.cer")
			{
				Layout = layout,
				AppName = typeof(RemoteSyslog5424AppenderTest).Name
			};

			appender.ActivateOptions();

			BasicConfigurator.Configure(appender);

			_appender = appender;
			_log = LogManager.GetLogger(typeof(RemoteSyslog5424AppenderTest));
		}

		readonly MockServer _server = new MockServer();
		private const int Port = 44344;

		private RemoteSyslog5424Appender _appender;
		private ILog _log;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using log4net.Appender;
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
		public void TestServerNotResponding()
		{
			var testMessage = FormatMessage("TestServerNotResponding");
			_log.Info(testMessage);

			Thread.Sleep(TimeSpan.FromSeconds(6));
			StartServer();
			Thread.Sleep(TimeSpan.FromSeconds(6));

			var messages = _server.GetMessages();
			Assert.IsTrue(messages.Any(message => message.Contains(testMessage)));

			_server.ClearMessages();
		}

		[Test]
		public void TestConnectionInterruption()
		{
			StartServer();

			var sentMessages = new List<string>();
			var i = 0;
			for (; i < 3; i++)
			{
				var message = FormatMessage("TestConnectionInterruption" + i);
				_log.Info(message);
				sentMessages.Add(message);
			}

			Thread.Sleep(TimeSpan.FromSeconds(6));
			_server.CloseConnections();

			for (; i < 6; i++)
			{
				var message = FormatMessage("TestConnectionInterruption" + i);
				_log.Info(message);
				sentMessages.Add(message);
			}

			Thread.Sleep(TimeSpan.FromSeconds(16));

			var messages = _server.GetMessages();
			foreach (var sentMessage in sentMessages)
			{
				Assert.IsTrue(messages.Any(message => message.Contains(sentMessage)));
			}
		}

		private static string FormatMessage(string id)
		{
			var message = id + "_" + Guid.NewGuid();
			return message;
		}

		private void StartServer()
		{
			_server.Start(Port, @"Certificate\test.pfx");
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

			var diagAppender = new TraceAppender
			{
				Layout = layout,
				Name = "RemoteSyslog5424AppenderDiagLogger",
			};
			diagAppender.ActivateOptions();

			BasicConfigurator.Configure(diagAppender, appender);

			_appender = appender;
			_log = LogManager.GetLogger(typeof(RemoteSyslog5424AppenderTest));
		}

		readonly MockServer _server = new MockServer();
		private const int Port = 44344;

		private RemoteSyslog5424Appender _appender;
		private ILog _log;
	}
}

﻿using System;
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
			var layout = new PatternLayout("%.255message");
			layout.ActivateOptions();

			using (var appender = new RemoteSyslog5424Appender(TestSettings.Server, TestSettings.Port, TestSettings.CertificatePath))
			{
				appender.Layout = layout;
				appender.AppName = typeof(RemoteSyslog5424AppenderTest).Name;
				appender.ActivateOptions();

				BasicConfigurator.Configure(appender);
				var log = LogManager.GetLogger(typeof(RemoteSyslog5424AppenderTest));

				for (var i = 0; i < 3; i++)
				{
					var message = i + "_" + Guid.NewGuid();
					log.Info(message);
				}
			}
		}
	}
}

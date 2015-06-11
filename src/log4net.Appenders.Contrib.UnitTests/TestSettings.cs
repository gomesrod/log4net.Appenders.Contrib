using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace log4net.Appenders.Contrib.UnitTests
{
	static class TestSettings
	{
		public static string Server
		{
			get { return Get("Log4netContribTests_Server"); }
		}

		public static int Port
		{
			get { return int.Parse(Get("Log4netContribTests_Port")); }
		}

		public static string CertificatePath
		{
			get { return Get("Log4netContribTests_CertificatePath"); }
		}

		static string Get(string name)
		{
			var res = Environment.GetEnvironmentVariable(name);
			if (string.IsNullOrEmpty(res))
				throw new ApplicationException(string.Format("Environment variable {0} is not found", name));
			return res;
		}
	}
}

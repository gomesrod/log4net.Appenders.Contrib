using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net.Core;
using log4net.Layout.Pattern;

namespace log4net.Appenders.Contrib.Converters
{
	class Iso8601DatePatternConverter : PatternLayoutConverter
	{
		public const string Iso8601Format = "yyyy-MM-ddTHH:mm:ss.fffZ";

		protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			writer.Write(loggingEvent.TimeStamp.ToUniversalTime().ToString(Iso8601Format));
		}
	}
}

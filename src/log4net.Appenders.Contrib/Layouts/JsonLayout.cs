using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net.Layout;
using log4net.Util;

using log4net.Appenders.Contrib.Converters;

namespace log4net.Appenders.Contrib.Layouts
{
	public sealed class JsonLayout : PatternLayout
	{
		public JsonLayout()
		{
			ConversionPattern = "{%event_as_json}%n";
			IgnoresException = false;

			AddConverter(new ConverterInfo { Name = "event_as_json", Type = typeof(JsonFragmentPatternConverter) });
			AddConverter(new ConverterInfo { Name = "iso8601_date", Type = typeof(Iso8601DatePatternConverter) });
		}
	}
}

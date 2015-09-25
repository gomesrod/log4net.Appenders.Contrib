using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using log4net.Core;
using log4net.Layout.Pattern;
using log4net.Util;
using Newtonsoft.Json;

namespace log4net.Appenders.Contrib.Converters
{
	public class JsonFragmentPatternConverter : PatternLayoutConverter
	{
		protected override void Convert(TextWriter writer, LoggingEvent loggingEvent)
		{
			var json = string.Empty;
			if (loggingEvent.ExceptionObject != null)
			{
				json = JsonConvert.SerializeObject(
					new
					{
						Message = loggingEvent.RenderedMessage,
						Exception = loggingEvent.ExceptionObject
					});
			}
			else
			{
				var obj = loggingEvent.MessageObject;
				if (obj == null || obj is string || obj is SystemStringFormat)
				{
					json = JsonConvert.SerializeObject(
						new
						{
							Message = loggingEvent.RenderedMessage
						},
						Formatting.None);
				}
				else
				{
					json = JsonConvert.SerializeObject(obj, Formatting.None);
				}
			}

			if (json.StartsWith("{") && json.EndsWith("}"))
				json = json.Substring(1, json.Length - 2);
			writer.Write(json);
		}
	}
}

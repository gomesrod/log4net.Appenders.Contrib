using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;

using log4net.Appender;
using log4net.Core;
using SyslogFacility = log4net.Appender.RemoteSyslogAppender.SyslogFacility;
using SyslogSeverity = log4net.Appender.RemoteSyslogAppender.SyslogSeverity;

namespace log4net.Appenders.Contrib
{
	public class RemoteSyslog5424Appender : AppenderSkeleton, IDisposable
	{
		public RemoteSyslog5424Appender(string server, int port, X509Certificate certificate)
		{
			Hostname = Dns.GetHostName();
			Version = 0;
			Facility = SyslogFacility.User;

			_socket = new Socket(SocketType.Stream, ProtocolType.IP);
			_socket.Connect(server, port);

			var rawStream = new NetworkStream(_socket);

			_stream = new SslStream(rawStream, false, VerifyServerCertificate);
			var certificates = new X509CertificateCollection(new[] { certificate });
			_stream.AuthenticateAsClient(server, certificates, SslProtocols.Tls, false);

			_writer = new StreamWriter(_stream, Encoding.UTF8);
		}

		public RemoteSyslog5424Appender(string host, int port, string certificatePath)
			: this(host, port, new X509Certificate(certificatePath))
		{
		}

		public SyslogFacility Facility { get; set; }
		public int Version { get; set; }

		public string Hostname
		{
			get { return _hostname ?? "-"; }
			set { _hostname = value; }
		}

		private string _hostname;

		public string AppName
		{
			get { return _appName ?? "-"; }
			set { _appName = value; }
		}

		private string _appName;

		public string ProcId
		{
			get { return _procId ?? "-"; }
			set { _procId = value; }
		}

		private string _procId;

		public string MessageId
		{
			get { return _messageId ?? "-"; }
			set { _messageId = value; }
		}

		private string _messageId;

		protected override void Append(LoggingEvent loggingEvent)
		{
			var sourceMessage = RenderLoggingEvent(loggingEvent);

			var time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
			var message = string.Format("<{0}>{1} {2} {3} {4} {5} {6} {7}",
				GeneratePriority(loggingEvent.Level), Version, time, Hostname, AppName, ProcId, MessageId, sourceMessage);
			_writer.Write(message);
			_writer.Flush();
		}

		private static bool VerifyServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		// Priority generation in RFC 5424 seems to be the same as in RFC 3164
		int GeneratePriority(Level level)
		{
			return RemoteSyslogAppender.GeneratePriority(Facility, GetSeverity(level));
		}

		public static SyslogSeverity GetSeverity(Level level)
		{
			if (level >= Level.Alert)
				return SyslogSeverity.Alert;

			if (level >= Level.Critical)
				return SyslogSeverity.Critical;

			if (level >= Level.Error)
				return SyslogSeverity.Error;

			if (level >= Level.Warn)
				return SyslogSeverity.Warning;

			if (level >= Level.Notice)
				return SyslogSeverity.Notice;

			if (level >= Level.Info)
				return SyslogSeverity.Informational;

			return SyslogSeverity.Debug;
		}

		public void Dispose()
		{
			if (_writer != null)
			{
				_writer.Flush();
				_writer.Dispose();
				_writer = null;
			}

			if (_stream != null)
			{
				_stream.Dispose();
				_stream = null;
			}

			if (_socket != null)
			{
				_socket.Disconnect(false);
				_socket.Dispose();
				_socket = null;
			}
		}

		private Socket _socket;
		private SslStream _stream;
		private TextWriter _writer;
	}
}

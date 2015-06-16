using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		public RemoteSyslog5424Appender()
		{
		}

		public RemoteSyslog5424Appender(string server, int port, string certificatePath)
		{
			Server = server;
			Port = port;
			CertificatePath = certificatePath;
		}

		private void EnsureInit()
		{
			if (_disposed)
				throw new ObjectDisposedException(GetType().FullName);

			if (_socket != null)
				return;

			lock (_initSync)
			{
				Hostname = Dns.GetHostName();
				Version = 1;
				Facility = SyslogFacility.User;

				_socket = new Socket(SocketType.Stream, ProtocolType.IP);
				_socket.Connect(Server, Port);

				var rawStream = new NetworkStream(_socket);

				_stream = new SslStream(rawStream, false, VerifyServerCertificate);
				var certificate = (string.IsNullOrEmpty(CertificatePath))
					? new X509Certificate(Encoding.ASCII.GetBytes(Certificate.Trim()))
					: new X509Certificate(CertificatePath);
				var certificates = new X509CertificateCollection(new[] { certificate });
				_stream.AuthenticateAsClient(Server, certificates, SslProtocols.Tls, false);

				_writer = new StreamWriter(_stream, Encoding.UTF8);
			}
		}

		public string Server { get; set; }
		public int Port { get; set; }
		public string CertificatePath { get; set; }
		public string Certificate { get; set; }

		public SyslogFacility Facility { get; set; }
		public int Version { get; private set; }

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
			EnsureInit();

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

		void Disconnect()
		{
			lock (_initSync)
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
					try
					{
						_socket.Disconnect(false);
					}
					catch (SocketException exc)
					{
						Trace.WriteLine(exc);
					}
					_socket.Dispose();
					_socket = null;
				}
			}
		}

		public void Dispose()
		{
			lock (_initSync)
			{
				Disconnect();
				_disposed = true;
			}
		}

		protected override void OnClose()
		{
			Dispose();
		}

		private Socket _socket;
		private SslStream _stream;
		private TextWriter _writer;

		private volatile bool _disposed;
		private readonly object _initSync = new object();
	}
}

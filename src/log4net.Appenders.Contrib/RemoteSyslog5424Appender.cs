using System;
using System.Collections.Concurrent;
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
using System.Threading;

using log4net.Appender;
using log4net.Core;
using SyslogFacility = log4net.Appender.RemoteSyslogAppender.SyslogFacility;
using SyslogSeverity = log4net.Appender.RemoteSyslogAppender.SyslogSeverity;

namespace log4net.Appenders.Contrib
{
	/// <summary>
	/// Transfer logs using syslog protocol over TLS
	/// related RFCs:
	/// https://tools.ietf.org/html/rfc5424
	/// https://tools.ietf.org/html/rfc5425
	/// https://tools.ietf.org/html/rfc6587
	/// </summary>
	public class RemoteSyslog5424Appender : AppenderSkeleton, IDisposable
	{
		public RemoteSyslog5424Appender()
		{
			Hostname = Dns.GetHostName();
			Version = 1;
			Facility = SyslogFacility.User;
			TrailerChar = '\n';

			_senderThread = new Thread(SenderThreadEntry) { Name = "SenderThread" };
		}

		public RemoteSyslog5424Appender(string server, int port, string certificatePath)
			: this()
		{
			Server = server;
			Port = port;
			CertificatePath = certificatePath;
		}

		public string Server { get; set; }
		public int Port { get; set; }
		public string CertificatePath { get; set; }
		public string Certificate { get; set; }

		public SyslogFacility Facility { get; set; }
		public int Version { get; private set; }

		public char? TrailerChar { get; set; }

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

		public override void ActivateOptions()
		{
			base.ActivateOptions();
			_senderThread.Start();
		}

		protected override void Append(LoggingEvent loggingEvent)
		{
			try
			{
				var sourceMessage = RenderLoggingEvent(loggingEvent);

				var time = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
				var message = string.Format("<{0}>{1} {2} {3} {4} {5} {6} {7}",
					GeneratePriority(loggingEvent.Level), Version, time, Hostname, AppName, ProcId, MessageId, sourceMessage);
				if (TrailerChar != null)
					message += TrailerChar;
				var frame = string.Format("{0} {1}", message.Length, message);

				_messageQueue.Enqueue(frame);
			}
			catch (Exception exc)
			{
				LogError(exc);
			}
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

		private void EnsureConnected()
		{
			if (_disposed)
				throw new ObjectDisposedException(GetType().FullName);

			lock (_initSync)
			{
				if (_socket != null)
					return;

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

		private static bool VerifyServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
		{
			return true;
		}

		private void SenderThreadEntry()
		{
			try
			{
				while (!_disposed)
				{
					var startTime = DateTime.UtcNow;
					while (DateTime.UtcNow - startTime < _sendingPeriod && !_closing)
						Thread.Sleep(10);

					TrySendMessages();
					if (_closing)
						break;
				}
			}
			catch (ThreadInterruptedException)
			{
			}
			catch (Exception exc)
			{
				LogError(exc);
			}
		}

		private void TrySendMessages()
		{
			try
			{
				try
				{
					EnsureConnected();

					while (true)
					{
						string frame;
						if (!_messageQueue.TryPeek(out frame))
							break;

						_writer.Write(frame);
						_writer.Flush();

						_messageQueue.TryDequeue(out frame);
					}

					return;
				}
				catch (SocketException exc)
				{
					if (exc.SocketErrorCode != SocketError.TimedOut)
						LogError(exc);
				}
				catch (IOException exc)
				{
					if ((uint)exc.HResult != 0x80131620) // COR_E_IO
						LogError(exc);
				}

				Disconnect();
			}
			catch (ThreadInterruptedException)
			{
			}
			catch (ThreadAbortException)
			{
			}
			catch (ObjectDisposedException)
			{
			}
			catch (Exception exc)
			{
				LogError(exc);
			}
		}

		void Disconnect()
		{
			lock (_initSync)
			{
				if (_writer != null)
				{
					try
					{
						_writer.Dispose();
					}
					catch (Exception exc)
					{
						LogError(exc);
					}
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
						if (_socket.Connected)
							_socket.Disconnect(true);
					}
					catch (Exception exc)
					{
						LogError(exc);
					}

					_socket.Dispose();
					_socket = null;
				}
			}
		}

		public void Dispose()
		{
			try
			{
				_closing = true;

				_senderThread.Join(TimeSpan.FromSeconds(10)); // give the sender thread some time to flush the messages

				_senderThread.Interrupt();
				_senderThread.Join(TimeSpan.FromSeconds(5));

				_senderThread.Abort();

				_disposed = true;
			}
			catch (Exception exc)
			{
				LogError(exc);
			}

			try
			{
				Disconnect();
			}
			catch (Exception exc)
			{
				LogError(exc);
			}
		}

		protected override void OnClose()
		{
			Dispose();
			base.OnClose();
		}

		void LogError(Exception exc)
		{
			if (_closing)
				Trace.WriteLine(exc);
			else
				_log.Error(exc);
		}

		private Socket _socket;
		private SslStream _stream;
		private TextWriter _writer;

		private volatile bool _disposed;
		private volatile bool _closing;
		private readonly object _initSync = new object();

		private readonly ILog _log = LogManager.GetLogger("RemoteSyslog5424AppenderDiagLogger");

		readonly ConcurrentQueue<string> _messageQueue = new ConcurrentQueue<string>();
		private readonly Thread _senderThread;
		private readonly TimeSpan _sendingPeriod = TimeSpan.FromSeconds(5);
	}
}

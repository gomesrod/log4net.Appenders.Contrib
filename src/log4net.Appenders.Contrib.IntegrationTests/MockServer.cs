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
using System.Threading;

namespace log4net.Appenders.Contrib.IntegrationTests
{
	class MockServer : IDisposable
	{
		public void Start(int port, string certificatePath)
		{
			Trace.WriteLine("     ===== MockServer.Start() =====     ");
			lock (_sync)
			{
				if (_listenerThread != null)
					throw new InvalidOperationException();

				_port = port;
				_serverCertificate = new X509Certificate2(certificatePath);

				_listenerThread = new Thread(Listen);
				_listenerThread.Start();
			}
		}

		public void Stop()
		{
			Trace.WriteLine("     ===== MockServer.Stop() =====     ");
			lock (_sync)
			{
				if (_listener != null)
				{
					_listener.Stop();
					_listener = null;
				}

				CloseConnections(false);

				if (!_listenerThread.Join(TimeSpan.FromSeconds(5)))
					_listenerThread.Abort();
				_listenerThread = null;
			}
		}

		public void CloseConnections(bool trace = true)
		{
			if (trace)
				Trace.WriteLine("     ===== MockServer.CloseConnections() =====     ");

			lock (_sync)
			{
				foreach (var connection in _connections)
				{
					connection.Close();
				}
				_connections.Clear();
			}
		}

		public void Dispose()
		{
			Stop();
		}

		void Listen()
		{
			try
			{
				_listener = new TcpListener(IPAddress.Any, _port);
				_listener.Start();

				while (true)
				{
					var client = _listener.AcceptTcpClient();
					ProcessConnection(client);
				}
			}
			catch (SocketException)
			{ }
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}
		}

		void ProcessConnection(TcpClient client)
		{
			lock (_sync)
			{
				_connections.Add(client);
			}

			var thread = new Thread(ConnectionThreadEntry);
			thread.Start(client);
		}

		void ConnectionThreadEntry(object state)
		{
			try
			{
				var client = (TcpClient)state;

				var sslStream = new SslStream(client.GetStream(), false);
				try
				{
					sslStream.AuthenticateAsServer(_serverCertificate, false, SslProtocols.Tls, false);

					using (var reader = new StreamReader(sslStream))
					{
						while (true)
						{
							var line = reader.ReadLine();
							if (line == null)
								break;

							lock (_sync)
							{
								_messages.Add(line);
							}
							Trace.WriteLine("  : " + line);
						}
					}
				}
				catch (AuthenticationException)
				{ }
				catch (SocketException)
				{ }
				catch (IOException)
				{ }

				sslStream.Close();
				client.Close();
			}
			catch (Exception exc)
			{
				Trace.WriteLine(exc);
			}
		}

		public List<string> GetMessages()
		{
			lock (_sync)
			{
				return new List<string>(_messages);
			}
		}

		public void ClearMessages()
		{
			lock (_sync)
			{
				_messages.Clear();
			}
		}

		readonly object _sync = new object();

		private X509Certificate _serverCertificate;
		private int _port;
		private TcpListener _listener;
		private readonly List<TcpClient> _connections = new List<TcpClient>();
		private Thread _listenerThread;

		readonly List<string> _messages = new List<string>();
	}
}

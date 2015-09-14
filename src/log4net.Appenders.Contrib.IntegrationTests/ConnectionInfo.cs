using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace log4net.Appenders.Contrib.IntegrationTests
{
	class ConnectionInfo
	{
		public ConnectionInfo(Socket socket, NetworkStream stream)
		{
			Socket = socket;
			Stream = stream;
		}

		public Socket Socket;
		public NetworkStream Stream;

		public void Close()
		{
			if (Stream != null)
				Stream.Close();
			Stream = null;

			if (Socket != null)
			{
				Socket.Shutdown(SocketShutdown.Both);
				Socket.Disconnect(false);
				Socket.Close();
			}
			Socket = null;
		}
	}
}

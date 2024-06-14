using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using EncryptChat.ViewModels;

namespace EncryptChat.Models
{
    public class SocketConnection
    {
        private string? _ip;
        private int _port;
        private bool _isServer;
        private static IPHostEntry _host = default!;
        private static IPAddress _ipAddress = default!;
        private static IPEndPoint _localEndPoint = default!;
        private static IPEndPoint _remoteEP = default!;
        public static Socket? ClientSocket;
        public static Socket? ServerSocket;

        private static List<Socket> _clientSockets = new List<Socket>();
        private static object _lock = new object();

        public SocketConnection(bool isServer)
        {
            this._isServer = isServer;
            if (_isServer)
            {
                StartServer();
            }
        }

        public SocketConnection(string ip, int port)
        {
            this._ip = ip;
            this._port = port;
            StartClient();
        }

        private static async Task StartServer()
        {
            _host = Dns.GetHostEntry("localhost");
            _ipAddress = _host.AddressList[0];
            _localEndPoint = new IPEndPoint(_ipAddress, 11000);

            try
            {
                // Create a socket
                ServerSocket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // A Socket must be associated with an endpoint using the Bind method
                ServerSocket.Bind(_localEndPoint);

                // Listen for incoming connections
                ServerSocket.Listen(10);
                MainWindowViewModel.Messages.Add("Server listening on port : 11000...");
                MainWindowViewModel.Messages.Add("Waiting for connections...");

                while (true)
                {
                    // Accept a connection
                    var handler = await ServerSocket.AcceptAsync();
                    lock (_lock)
                    {
                        _clientSockets.Add(handler);
                    }
                    MainWindowViewModel.Messages.Add("Client connected.");

                    // Handle the client in a separate task
                    Task.Run(() => HandleClient(handler));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static async Task HandleClient(Socket handler)
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRec = await handler.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (bytesRec > 0)
                    {
                        string data = Encoding.ASCII.GetString(buffer, 0, bytesRec);
                        MainWindowViewModel.Messages.Add("Text received: " + data);

                        byte[] msg = Encoding.ASCII.GetBytes(data);

                        lock (_lock)
                        {
                            foreach (var clientConnection in _clientSockets)
                            {
                                if (clientConnection != handler) // Avoid echoing the message back to the sender
                                {
                                    clientConnection.Send(new ArraySegment<byte>(msg), SocketFlags.None);
                                }
                            }
                        }
                    }
                }
            }
            catch (SocketException)
            {
                // Handle client disconnection
                lock (_lock)
                {
                    _clientSockets.Remove(handler);
                }
                MainWindowViewModel.Messages.Add("Client disconnected.");
                handler.Close();
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Messages.Add("Error: " + ex.Message);
            }
        }

        private static void StartClient()
        {
            byte[] bytes = new byte[1024];

            try
            {
                _host = Dns.GetHostEntry("localhost");
                _ipAddress = _host.AddressList[0];
                _remoteEP = new IPEndPoint(_ipAddress, 11000);

                // Create a socket
                ClientSocket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                try
                {
                    ClientSocket.Connect(_remoteEP);
                    MainWindowViewModel.Messages.Add("Socket connected to " + ClientSocket?.RemoteEndPoint?.ToString());

                    // Start receiving messages from the server
                    Task.Run(() => ReceiveMessages());
                }
                catch (ArgumentNullException ane)
                {
                    MainWindowViewModel.Messages.Add("ArgumentNullException: " + ane.ToString());
                }
                catch (SocketException se)
                {
                    MainWindowViewModel.Messages.Add("SocketException: " + se.ToString());
                }
                catch (Exception e)
                {
                    MainWindowViewModel.Messages.Add("Unexpected exception: " + e.ToString());
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private static async Task ReceiveMessages()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRec = await ClientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (bytesRec > 0)
                    {
                        string data = Encoding.ASCII.GetString(buffer, 0, bytesRec);
                        MainWindowViewModel.Messages.Add("Server: " + data);
                    }
                }
            }
            catch (SocketException)
            {
                MainWindowViewModel.Messages.Add("Disconnected from server.");
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Messages.Add("Error: " + ex.Message);
            }
        }

        private static void SendMessageClient(byte[] bytes)
        {
            try
            {
                // Send the data through the socket.
                int bytesSent = ClientSocket.Send(bytes);
            }
            catch (Exception ex)
            {
                MainWindowViewModel.Messages.Add("Error sending message: " + ex.Message);
            }
        }

        public void SendMessageClientPublic(string message)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(message + "<EOF>");
            SendMessageClient(bytes);
        }
    }
}

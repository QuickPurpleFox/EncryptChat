using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using DynamicData;
using EncryptChat.ViewModels;

namespace EncryptChat.Models
{
    public class SocketConnection
    {
        private string? _ip;
        private int _port;
        private static bool _isServerStatic;
        private bool _isServer;
        private static IPAddress _ipAddress = default!;
        private static IPEndPoint _localEndPoint = default!;
        private static IPEndPoint _remoteEP = default!;
        public static Socket? ClientSocket;
        public static Socket? ServerSocket;

        private static List<Socket> _clientSockets = new List<Socket>();
        private static List<Socket> _socketSendPublicKey = new List<Socket>();

        private static object _lock = new object();
        private static object _SendLock = new object();
        public static MessageCryptography Crypto;

        // Dictionary to store public keys for each client IP address
        private static Dictionary<string, byte[]> _clientPublicKeys = new Dictionary<string, byte[]>();

        // Dictionary to store private AES keys
        private static Dictionary<string, byte[]> _clientAESKeys = new Dictionary<string, byte[]>();

        public SocketConnection(bool isServer)
        {
            this._isServer = isServer;
            _isServerStatic = isServer;
            if (_isServer)
            {
                Crypto = new MessageCryptography();
                StartServer();
            }
        }

        public SocketConnection(string ip, int port)
        {
            this._ip = ip;
            this._port = port;
            Crypto = new MessageCryptography();
            StartClient();
        }

        private static async Task StartServer()
        {
            // Bind to all available network interfaces (both IPv4 and IPv6)
            _localEndPoint = new IPEndPoint(IPAddress.IPv6Any, 11000);

            try
            {
                ServerSocket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                ServerSocket.SetSocketOption(SocketOptionLevel.IPv6, SocketOptionName.IPv6Only, false);
                ServerSocket.Bind(_localEndPoint);
                ServerSocket.Listen(10);

                MainWindowViewModel.Messages.Add("Server listening on port : 11000...");
                MainWindowViewModel.Messages.Add("Waiting for connections...");

                while (true)
                {
                    var handler = await ServerSocket.AcceptAsync();
                    lock (_lock)
                    {
                        _clientSockets.Add(handler);
                    }
                    MainWindowViewModel.Messages.Add("Client connected, IP: " + IPAddress.Parse(((IPEndPoint)handler.RemoteEndPoint).Address.ToString()));
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
                foreach (var clientConnection in _clientSockets)
                {
                    if (!_socketSendPublicKey.Contains(clientConnection))
                    {
                        MainWindowViewModel.Messages.Add("Sending: " + "<DH_PUBLIC_KEY_REQUEST>");
                        handler.Send(System.Text.Encoding.UTF8.GetBytes("<DH_PUBLIC_KEY_REQUEST>"));
                        _socketSendPublicKey.Add(clientConnection);
                    }
                }
                
                while (true)
                {

                    byte[] buffer = new byte[1024];
                    int bytesRec = await handler.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (bytesRec > 0)
                    {
                        string data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRec);

                        MainWindowViewModel.Messages.Add("Received but no display: " + data);

                        // Get the sender's IP address
                        string senderIp = ((IPEndPoint)handler.RemoteEndPoint).Address.ToString();
                        string messageWithIp = $"{senderIp}<split> {data}";
                        MainWindowViewModel.Messages.Add("Received: " + messageWithIp);

                        byte[] msg = System.Text.Encoding.UTF8.GetBytes(messageWithIp);

                        lock (_lock)
                        {
                            foreach (var clientConnection in _clientSockets)
                            {
                                if (data.Contains("<DH_PUBLIC_KEY>"))
                                {
                                    data = data.Replace("<DH_PUBLIC_KEY>", "");
                                    byte[] publicKey = Convert.FromBase64String(data);
                                    _clientPublicKeys[senderIp] = publicKey;
                                    foreach (var clientSend in _clientSockets)
                                    {
                                        if (clientConnection != handler)
                                        {
                                            MainWindowViewModel.Messages.Add("Sending public key to: " + ((IPEndPoint)clientSend.RemoteEndPoint).Address.ToString() + "key: " + Convert.ToBase64String(publicKey));
                                            clientSend.Send(System.Text.Encoding.UTF8.GetBytes("<DH_PUBLIC_KEY>" + "<split>" + senderIp + "<split>" + Convert.ToBase64String(publicKey)));
                                        }
                                    }
                                }
                                else if (clientConnection != handler) 
                                {
                                    clientConnection.Send(new ArraySegment<byte>(msg), SocketFlags.None);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private void StartClient()
        {
            try
            {
                _ipAddress = IPAddress.Parse(_ip);  // Ensure _ipAddress is set correctly
                _remoteEP = new IPEndPoint(_ipAddress, _port);
                ClientSocket = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                ClientSocket.Connect(_remoteEP);

                byte[] publicKey = Crypto.PublicKeyNonStatic;
                
                MainWindowViewModel.Messages.Add("Your PK: " + Convert.ToBase64String(publicKey));

                Task.Run(() => ListenForServerMessages());
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        private async Task ListenForServerMessages()
        {
            try
            {
                while (true)
                {
                    byte[] buffer = new byte[1024];
                    int bytesRec = await ClientSocket.ReceiveAsync(new ArraySegment<byte>(buffer), SocketFlags.None);
                    if (bytesRec > 0)
                    {
                        string data = System.Text.Encoding.UTF8.GetString(buffer, 0, bytesRec);

                        MainWindowViewModel.Messages.Add("Received but not display: " + data);

                        if (data.Contains("<DH_PUBLIC_KEY_REQUEST>"))
                        {
                            SendMessageClient("<DH_PUBLIC_KEY>" + Convert.ToBase64String(Crypto.PublicKeyNonStatic));
                        }
                        else if (data.Contains("<DH_PUBLIC_KEY>"))
                        {
                            string[] parts = data.Split("<split>");
                            string senderIp = parts[1];
                            byte[] publicKey = Convert.FromBase64String(parts[2]);
                            _clientPublicKeys[senderIp] = publicKey;
                            MainWindowViewModel.Messages.Add("DH key from " + senderIp + ": " + Convert.ToBase64String(publicKey));
                            byte[] sharedSecret = Crypto.ComputeDiffieHellmanSharedSecret(publicKey);
                            _clientAESKeys[senderIp] = sharedSecret;
                        }
                        else
                        {
                            string[] parts = data.Split("<split>");
                            string senderIp = parts[0];
                            MainWindowViewModel.Messages.Add("SenderIP: " + parts[0]);
                            MainWindowViewModel.Messages.Add(data);
                            string? message = Crypto.DecryptMessage(Convert.FromBase64String(parts[0]) , _clientPublicKeys.GetValueOrDefault(senderIp));
                            MainWindowViewModel.Messages.Add(message);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void SendMessageClient(string message)
        {
            try
            {
                MainWindowViewModel.Messages.Add("Sending: " + message);
                if (message.Contains("<DH_PUBLIC_KEY>"))
                {
                    byte[] msg = System.Text.Encoding.UTF8.GetBytes(message);
                    ClientSocket.Send(msg);
                }
                else
                {
                    string senderIp = ((IPEndPoint)ClientSocket.LocalEndPoint).Address.ToString();
                    byte[] encryptedMessage = Crypto.EncryptMessage(message);
                    string encryptedMessageBase64 = Convert.ToBase64String(encryptedMessage);
                    byte[] msg = Convert.FromBase64String(encryptedMessageBase64);
                    ClientSocket.Send(msg);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        
        public void SendMessageClientNonStatic(string message)
        {
            SendMessageClient(message);
        }
    }
}

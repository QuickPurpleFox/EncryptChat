using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using EncryptChat.ViewModels;

namespace EncryptChat.Models;

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
    private static Socket? _handler = default!;

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

    private static void StartServer()
    {
        _host = Dns.GetHostEntry("localhost");
        _ipAddress = _host.AddressList[0];
        _localEndPoint = new IPEndPoint(_ipAddress, 11000);

        try
        {
            // Create a socket
            ServerSocket  = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            
            // A Socket must be associated with an endpoint using the Bind method
            ServerSocket.Bind(_localEndPoint);
            
            // Listen for incoming connections
            ServerSocket.Listen(10);
            MainWindowViewModel.Messages.Add("Server listening on port : 11000..." );

            MainWindowViewModel.Messages.Add("Waiting for a connection...");
            
            // Accept a connection
            _handler = ServerSocket.Accept();

                // Incoming data from the client.
                string? data = null;
                byte[]? bytes = null;

                while (true)
                {
                    bytes = new byte[1024];
                    int bytesRec = _handler.Receive(bytes);
                    data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                    if (data.IndexOf("<EOF>") > -1)
                    {
                        break;
                    }
                }

                MainWindowViewModel.Messages.Add("Text received : " + data);

                byte[] msg = Encoding.ASCII.GetBytes(data);
                _handler.Send(msg);
                /*handler.Shutdown(SocketShutdown.Both);
                handler.Close();*/
        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
    
    private static void StartClient()
    {
        byte[] bytes = new byte[1024];

        try
        {
            // Connect to a Remote server
            // Get Host IP Address that is used to establish a connection
            // In this case, we get one IP address of localhost that is IP : 127.0.0.1
            // If a host has multiple addresses, you will get a list of addresses
            _host = Dns.GetHostEntry("localhost");
            _ipAddress = _host.AddressList[0];
            _remoteEP = new IPEndPoint(_ipAddress, 11000);

            // Create a socket
            ClientSocket  = new Socket(_ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            // Connect the socket to the remote endpoint. Catch any errors.
            try
            {
                ClientSocket.Connect(_remoteEP);
                MainWindowViewModel.Messages.Add("Socket connected to "+ ClientSocket?.RemoteEndPoint?.ToString());
            }
            catch (ArgumentNullException ane)
            {
                MainWindowViewModel.Messages.Add("ArgumentNullException : " + ane.ToString());
            }
            catch (SocketException se)
            {
                MainWindowViewModel.Messages.Add("SocketException : " + se.ToString());
            }
            catch (Exception e)
            {
                MainWindowViewModel.Messages.Add("Unexpected exception : " + e.ToString());
            }

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }

    private static void SendMessageClient(byte[] bytes)
    {
        // Send the data through the socket.
        int bytesSent = ClientSocket.Send(bytes);

        // Receive the response from the remote device.
        //int bytesRec = ClientSocket.Receive(bytes);
        //MainWindowViewModel.Messages.Add("Echoed test = " + Encoding.ASCII.GetString(bytes, 0, bytesRec));
    }

    public void SendMessageClientPublic(string message)
    {
        byte[] bytes = new byte[1024];
        
        message = message + "<EOF>";
        bytes = Encoding.ASCII.GetBytes(message);
        
        SendMessageClient(bytes);
    }
}
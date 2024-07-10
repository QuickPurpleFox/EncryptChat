using System.Text;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EncryptChat.Models;
using EncryptChat.ViewModels;

namespace EncryptChat.Views
{
    /// <summary>
    /// Main window for the chat application.
    /// </summary>
    public partial class MainWindow : Window
    {
        private SocketConnection _socket;

        /// <summary>
        /// Initializes a new instance of the MainWindow class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
            ChatList.ItemsSource = MainWindowViewModel.Messages;
        }

        /// <summary>
        /// Event handler for the Send button click event.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private void OnSendClick(object sender, RoutedEventArgs e)
        {
            SendMessage();
        }

        /// <summary>
        /// Event handler for the Connect button click event.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private void OnConnectClick(object sender, RoutedEventArgs e)
        {
            if (NetMode.SelectedIndex == 1)
            {
                _socket = new SocketConnection(true);
            }
            else if (IpBlock.Text != null)
            {
                MainWindowViewModel.Messages.Add("Debug: " + IpBox.Text);
                _socket = new SocketConnection(IpBox.Text.ToString(), 11000);
            }
            else
            {
                MainWindowViewModel.Messages.Add("ERROR: " + "internal error, contact with administrator");
            }
        }

        /// <summary>
        /// Event handler for the KeyDown event of the input box.
        /// Sends a message when the Enter key is pressed.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                SendMessage();
            }
        }

        /// <summary>
        /// Sends the message entered in the input box.
        /// </summary>
        private async void SendMessage()
        {
            var message = MessageText.Text;
            if (!string.IsNullOrEmpty(message))
            {
                MessageText.Text = string.Empty;
                if ((_socket.GetType() == typeof(SocketConnection)))
                {
                    _socket.SendMessageClientNonStatic(message);
                    MainWindowViewModel.Messages.Add("You: " + message);
                }
                else
                {
                    MainWindowViewModel.Messages.Add("Error: connection not established!");
                }
            }
        }

        /// <summary>
        /// Event handler for changing the network mode (client/server).
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">Event arguments.</param>
        private void ChangedNetMode(object sender, SelectionChangedEventArgs e)
        {
            if (NetMode?.SelectedIndex == 1)
            {
                MessageText.IsVisible = false;
                SendMessageButton.IsVisible = false;
                IpBlock.IsVisible = false;
                IpBox.IsVisible = false;
                UserInfo.Text = "SERVER MODE";
                ConnectButton.Content = "Start server";
            }
        }
    }
}

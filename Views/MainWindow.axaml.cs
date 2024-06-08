using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using EncryptChat.Models;
using EncryptChat.ViewModels;

namespace EncryptChat.Views;

public partial class MainWindow : Window
{
    private SocketConnection socket;
    
    public MainWindow()
    {
        InitializeComponent();
        ChatList.ItemsSource = MainWindowViewModel.Messages;
    }

    private void OnSendClick(object sender, RoutedEventArgs e)
    {
        SendMessage();
    }

    private void OnConnectClick(object sender, RoutedEventArgs e)
    {
        if (NetMode.SelectedIndex == 1)
        {
            socket = new SocketConnection(true);
        }
        else if(IpBlock.Text != null)
        {
            socket = new SocketConnection(IpBlock.Text, 1621);
        }
        else
        {
            MainWindowViewModel.Messages.Add("ERROR: " + "internal error, contact with administrator");
        }
    }
    
    private async void InputBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            SendMessage();
        }
    }

    private async void SendMessage()
    {
        var message = MessageText.Text;
        if (!string.IsNullOrEmpty(message))
        {
            MainWindowViewModel.Messages.Add("You: " + message);
            MessageText.Text = string.Empty;
        }
    }
}
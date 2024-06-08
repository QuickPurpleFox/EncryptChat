namespace EncryptChat.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using System.Collections.ObjectModel;

public class MainWindowViewModel : UserControl
{
    
#pragma warning disable CA1822 // Mark members as static    
    public static ObservableCollection<string> Messages { get; } = new ObservableCollection<string>();
    

#pragma warning restore CA1822 // Mark members as static
    
}

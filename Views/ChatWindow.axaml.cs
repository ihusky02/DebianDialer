using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using DebianDialer.ViewModels;
using System.Linq;

namespace DebianDialer.Views;

public partial class ChatWindow : Window
{
    public ChatWindow(string phoneNumber, string displayName)
    {
        InitializeComponent();
        
        var viewModel = new ChatWindowViewModel(phoneNumber, displayName);
        DataContext = viewModel;

        viewModel.OnNewMessageAdded += () => 
        {
            Dispatcher.UIThread.Post(() => ScrollToBottom(), DispatcherPriority.Background);
        };
    }

    private void ScrollToBottom()
    {
        var chatListBox = this.FindControl<ListBox>("ChatListBox");
        if (chatListBox != null && chatListBox.ItemCount > 0)
        {
            var lastItem = chatListBox.Items.Cast<object>().LastOrDefault();
            if (lastItem != null)
            {
                chatListBox.ScrollIntoView(lastItem);
            }
        }
    }

    // Bezpośrednie wywołanie metody z ViewModelu po naciśnięciu Enter
    private async void TextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Shift))
            {
                return; // Shift + Enter pozwala pisać w nowej linii
            }

            e.Handled = true; 

            if (DataContext is ChatWindowViewModel vm)
            {
                await vm.SendMessageAsync();
            }
        }
    }
}
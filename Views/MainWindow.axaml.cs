using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DebianDialer.ViewModels;
using DebianDialer.Services;
using System;
using System.IO;

namespace DebianDialer.Views;

public partial class MainWindow : Window
{
    // Przechowujemy instancję klienta, aby połączenie z D-Bus było stale aktywne
    private readonly OfonoClient _ofonoClient;

    public MainWindow()
    {
        InitializeComponent();
        
        // 1. Inicjalizacja usług - tutaj żyje nasza komunikacja z telefonem
        _ofonoClient = new OfonoClient();
        
        // 2. Tworzymy ViewModel i przekazujemy mu istniejącego klienta
        var vm = new MainWindowViewModel(_ofonoClient);
        
        // 3. Ustawiamy DataContext dla całego okna (wszystkie TabItem to odziedziczą)
        DataContext = vm;

        // 4. Obsługa argumentów startowych (tel:// itd.)
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1 && args[1] != "--hidden")
        {
            string input = args[1];
            
            if (input.StartsWith("tel://", StringComparison.OrdinalIgnoreCase)) input = input.Substring(6);
            else if (input.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) input = input.Substring(4);
            else if (input.StartsWith("callto:", StringComparison.OrdinalIgnoreCase)) input = input.Substring(7);

            input = input.Replace(" ", "").Replace("-", "");
            vm.PhoneNumber = input;
        }
    }

    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var args = Environment.GetCommandLineArgs();
        if (Array.Exists(args, arg => arg == "--hidden"))
        {
            WindowState = WindowState.Minimized;
        }
    }
}

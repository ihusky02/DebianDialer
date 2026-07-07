using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DebianDialer.Models;
using DebianDialer.ViewModels;
using DebianDialer.Services;
using System;
using System.IO;

namespace DebianDialer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        var ofonoClient = new OfonoClient();
        var vm = new MainWindowViewModel(ofonoClient);
        DataContext = vm;

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

    // --- NAPRAWA AUTOSTARTU ---
    // Ta funkcja odpala się dopiero, gdy system operacyjny "zauważy" nasze okno
    protected override void OnOpened(EventArgs e)
    {
        base.OnOpened(e);
        var args = Environment.GetCommandLineArgs();
        if (Array.Exists(args, arg => arg == "--hidden"))
        {
            WindowState = WindowState.Minimized;
        }
    }

    private async void ImportVcf_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Wybierz plik z kontaktami (.vcf)",
            AllowMultiple = false
        });

        if (files.Count >= 1)
        {
            var filePath = files[0].Path.LocalPath;
            var lines = File.ReadAllLines(filePath);
            var parser = new VcfParser();
            var contacts = parser.ParseLines(lines);

            if (contacts.Count > 0)
            {
                var dialog = new ContactsWindow(contacts);
                var selectedContact = await dialog.ShowDialog<Contact>(this);

                if (selectedContact != null && DataContext is MainWindowViewModel vm)
                {
                    vm.PhoneNumber = selectedContact.PhoneNumber.Replace(" ", "").Replace("-", "");
                }
            }
        }
    }
}

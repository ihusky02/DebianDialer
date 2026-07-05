using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DebianDialer.Models;
using DebianDialer.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;

namespace DebianDialer.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        var vm = new MainWindowViewModel();
        DataContext = vm;

        // Odbieranie numeru telefonu z zewnątrz (np. od Evolution)
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            // Drugi argument (args[1]) to przysłany link
            string input = args[1];
            
            // Czyszczenie prefiksów
            if (input.StartsWith("tel://", StringComparison.OrdinalIgnoreCase)) input = input.Substring(6);
            else if (input.StartsWith("tel:", StringComparison.OrdinalIgnoreCase)) input = input.Substring(4);
            else if (input.StartsWith("callto:", StringComparison.OrdinalIgnoreCase)) input = input.Substring(7);

            // Usuwamy spacje i myślniki, zostawiamy plus (np. +48)
            input = input.Replace(" ", "").Replace("-", "");
            
            // Wpisujemy numer do interfejsu
            vm.PhoneNumber = input;
            
            // UWAGA: Jeśli chcesz, aby po kliknięciu w Evolution aplikacja 
            // nie tylko wpisała numer, ale OD RAZU dzwoniła, odkomentuj linię poniżej:
            // vm.DialCommand.Execute(null);
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
            var contacts = ParseVcf(filePath);

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

    private List<Contact> ParseVcf(string filePath)
    {
        var contacts = new List<Contact>();
        try
        {
            Contact? current = null;
            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith("BEGIN:VCARD")) current = new Contact();
                else if (line.StartsWith("FN:") && current != null) current.Name = line.Substring(3);
                else if (line.StartsWith("TEL") && current != null)
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1) current.PhoneNumber = parts[1];
                }
                else if (line.StartsWith("END:VCARD") && current != null)
                {
                    if (!string.IsNullOrEmpty(current.PhoneNumber)) contacts.Add(current);
                    current = null;
                }
            }
        }
        catch (Exception) { }
        return contacts;
    }
}

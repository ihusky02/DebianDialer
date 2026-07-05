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
        DataContext = new MainWindowViewModel();
    }

    private async void ImportVcf_Click(object? sender, RoutedEventArgs e)
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        // Otwieramy systemowe okno wyboru plików
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

                // Jeśli użytkownik coś wybrał, aktualizujemy ViewModel
                if (selectedContact != null && DataContext is MainWindowViewModel vm)
                {
                    // Usuwamy zbędne znaki (spacje, myślniki), żeby oFono nie miało problemu
                    vm.PhoneNumber = selectedContact.PhoneNumber.Replace(" ", "").Replace("-", "");
                }
            }
        }
    }

    // Prosty parser plików .vcf
    private List<Contact> ParseVcf(string filePath)
    {
        var contacts = new List<Contact>();
        try
        {
            Contact? current = null;
            foreach (var line in File.ReadLines(filePath))
            {
                if (line.StartsWith("BEGIN:VCARD"))
                {
                    current = new Contact();
                }
                else if (line.StartsWith("FN:") && current != null)
                {
                    current.Name = line.Substring(3); // Pobiera imię po "FN:"
                }
                else if (line.StartsWith("TEL") && current != null)
                {
                    var parts = line.Split(':');
                    if (parts.Length > 1)
                    {
                        current.PhoneNumber = parts[1]; // Pobiera numer po ":"
                    }
                }
                else if (line.StartsWith("END:VCARD") && current != null)
                {
                    if (!string.IsNullOrEmpty(current.PhoneNumber))
                    {
                        contacts.Add(current);
                    }
                    current = null;
                }
            }
        }
        catch (Exception)
        {
            // W przypadku błędu odczytu (np. brak uprawnień) funkcja zwróci pustą listę
        }
        return contacts;
    }
}

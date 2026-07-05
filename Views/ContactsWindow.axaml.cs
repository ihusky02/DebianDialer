using Avalonia.Controls;
using Avalonia.Interactivity;
using DebianDialer.Models;
using System.Collections.Generic;

namespace DebianDialer.Views;

public partial class ContactsWindow : Window
{
    public ContactsWindow()
    {
        InitializeComponent();
    }

    // Konstruktor, który przyjmuje listę kontaktów do wyświetlenia
    public ContactsWindow(IEnumerable<Contact> contacts) : this()
    {
        var listBox = this.FindControl<ListBox>("ContactsList");
        if (listBox != null)
            listBox.ItemsSource = contacts;
    }

    private void Select_Click(object? sender, RoutedEventArgs e)
    {
        var listBox = this.FindControl<ListBox>("ContactsList");
        if (listBox?.SelectedItem is Contact selected)
        {
            Close(selected); // Zamyka okno i zwraca wybrany kontakt
        }
        else
        {
            Close(null);
        }
    }
}

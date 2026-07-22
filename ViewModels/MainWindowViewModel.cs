using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Avalonia.Threading; // <-- Wymagane do działania DispatcherTimer
using DebianDialer.Services;
using System.Linq;

namespace DebianDialer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly SmsClient _smsClient = new SmsClient();
    private readonly OfonoClient _ofonoClient;
    private readonly GoogleContactsClient _contactsClient = new GoogleContactsClient();
    private readonly DispatcherTimer _smsTimer; // <-- Obiekt zegara odświeżającego SMS

    private List<Contact> _allContacts = new List<Contact>();

    [ObservableProperty]
    private string _phoneNumber = "";

    [ObservableProperty]
    private string _messageText = "";

    [ObservableProperty]
    private string _searchText = "";

    [ObservableProperty]
    private bool _isSmsPopupOpen = false;

    [ObservableProperty]
    private ObservableCollection<SmsMessage> _messages = new ObservableCollection<SmsMessage>();

    public ObservableCollection<Contact> Contacts { get; } = new ObservableCollection<Contact>();

    public MainWindowViewModel(OfonoClient ofonoClient)
    {
        _ofonoClient = ofonoClient;
        
        _ofonoClient.IncomingCallReceived += (number) => {
            PhoneNumber = number;
        };

        _ = LoadContactsAsync();
        
        // 1. Pobierz SMS-y natychmiast przy starcie aplikacji
        _ = LoadMessages();

        // 2. Skonfiguruj i uruchom zegar, który będzie odświeżał listę co 10 sekund
        _smsTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(10)
        };
        _smsTimer.Tick += async (sender, e) => await LoadMessages();
        _smsTimer.Start();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    private void ApplyFilter()
    {
        Contacts.Clear();
        var filtered = string.IsNullOrWhiteSpace(SearchText) 
            ? _allContacts 
            : _allContacts.Where(c => c.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) 
                                   || c.Number.Contains(SearchText));
        
        foreach (var contact in filtered) Contacts.Add(contact);
    }

    private async Task LoadContactsAsync()
    {
        try 
        {
            _allContacts = await _contactsClient.GetContactsAsync();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd pobierania kontaktów Google: {ex.Message}");
        }
    }

    [RelayCommand]
    public async Task RefreshContacts() => await LoadContactsAsync();

    [RelayCommand]
    public async Task LoadMessages()
    {
        try
        {
            var fetchedMessages = await _smsClient.GetLatestMessagesAsync();
            Messages.Clear();
            foreach (var msg in fetchedMessages) Messages.Add(msg);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd pobierania wiadomości: {ex.Message}");
        }
    }

    // ZINTEGROWANA METODA WYSYŁANIA SMS
    [RelayCommand]
    public async Task SendSms()
    {
        if (!string.IsNullOrWhiteSpace(PhoneNumber) && !string.IsNullOrWhiteSpace(MessageText))
        {
            Console.WriteLine($"Wysyłam SMS do: {PhoneNumber}, Treść: {MessageText}");
            
            // Wywołanie faktycznej metody wysyłania z serwisu SmsClient
            await _smsClient.SendAsync(PhoneNumber, MessageText);
            
            MessageText = "";
            IsSmsPopupOpen = false; 

            // 3. Po wysłaniu natychmiast odśwież listę, aby pokazać wysłaną wiadomość
            await LoadMessages();
        }
    }

    [RelayCommand]
    public async Task Answer() => await _ofonoClient.AnswerAsync();

    [RelayCommand]
    public async Task Reject() => await _ofonoClient.HangupAsync();

    [RelayCommand]
    public async Task EnableAudioBridge() => await Task.CompletedTask;

    [RelayCommand]
    public async Task CallContact(string number)
    {
        Console.WriteLine($"Inicjowanie połączenia z: {number}");
        await _ofonoClient.DialAsync(number);
    }

    [RelayCommand]
    public void PrepareSms(string number)
    {
        PhoneNumber = number;
        MessageText = "";
        IsSmsPopupOpen = true;
        Console.WriteLine($"Przygotowano wiadomość dla: {number}");
    }

    [RelayCommand]
    public void CloseSmsPopup() => IsSmsPopupOpen = false;

    [RelayCommand]
    public void OpenChat(string number)
    {
        // 1. "Czyścimy" numer ze spacji, kresek i kierunkowego
        string cleanTarget = number.Replace(" ", "").Replace("-", "").Replace("+48", "");

        // 2. Szukamy w kontaktach, czyszcząc ich numery "w locie" i sprawdzając dopasowanie
        var contact = _allContacts.FirstOrDefault(c => 
        {
            if (string.IsNullOrEmpty(c.Number)) return false;
            
            string cleanContact = c.Number.Replace(" ", "").Replace("-", "").Replace("+48", "");
            return cleanContact.Contains(cleanTarget) || cleanTarget.Contains(cleanContact);
        });

        // 3. Jeśli znaleźliśmy kontakt, używamy imienia i nazwiska. Jak nie - numeru.
        string displayName = (contact != null && !string.IsNullOrEmpty(contact.Name)) ? contact.Name : number;

        // 4. Otwieramy okno czatu, podając wyciągniętą nazwę
        var chatWin = new DebianDialer.Views.ChatWindow(number, displayName);
        chatWin.Show();
    }
}
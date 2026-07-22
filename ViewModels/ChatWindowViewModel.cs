using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using DebianDialer.Services;
using System.Linq;

namespace DebianDialer.ViewModels;

public partial class ChatWindowViewModel : ObservableObject
{
    private readonly SmsClient _smsClient = new SmsClient();
    private readonly DispatcherTimer _chatTimer;
    
    private int _currentOffset = 0;
    private const int ChunkSize = 50;

    public event Action? OnNewMessageAdded;

    [ObservableProperty]
    private string _phoneNumber;

    [ObservableProperty]
    private string _contactName = "";

    [ObservableProperty]
    private string _newMessageText = "";

    [ObservableProperty]
    private bool _canLoadMore = true;

    public ObservableCollection<SmsMessage> ChatMessages { get; } = new ObservableCollection<SmsMessage>();

    public ChatWindowViewModel(string phoneNumber, string displayName)
    {
        PhoneNumber = phoneNumber;
        ContactName = displayName;

        _ = LoadInitialChatAsync();

        _chatTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
        _chatTimer.Tick += async (s, e) => await CheckForNewMessagesAsync();
        _chatTimer.Start();
    }

    private async Task LoadInitialChatAsync()
    {
        ChatMessages.Clear();
        _currentOffset = 0;
        CanLoadMore = true;
        await LoadOlderMessages();
        OnNewMessageAdded?.Invoke();
    }

    [RelayCommand]
    public async Task LoadOlderMessages()
    {
        var olderMessages = await _smsClient.GetConversationAsync(PhoneNumber, ChunkSize, _currentOffset);
        if (olderMessages.Count < ChunkSize) CanLoadMore = false;
        for (int i = olderMessages.Count - 1; i >= 0; i--) ChatMessages.Insert(0, olderMessages[i]);
        _currentOffset += olderMessages.Count;
    }

    private async Task CheckForNewMessagesAsync()
    {
        var newest = await _smsClient.GetConversationAsync(PhoneNumber, 5, 0);
        if (newest.Count == 0) return;
        bool addedAny = false;
        var recentList = ChatMessages.TakeLast(10).ToList();
        foreach (var msg in newest)
        {
            if (!recentList.Any(m => m.Text == msg.Text && m.IsIncoming == msg.IsIncoming))
            {
                ChatMessages.Add(msg);
                _currentOffset++;
                addedAny = true;
            }
        }
        if (addedAny) OnNewMessageAdded?.Invoke();
    }

    [RelayCommand]
    public async Task SendMessageAsync()
    {
        if (!string.IsNullOrWhiteSpace(NewMessageText))
        {
            string textToSend = NewMessageText;
            NewMessageText = ""; // Od razu czyszczymy pole wpisywania

            // 1. NATYCHMIASTOWE DODANIE DO UI (Optymistyczny dymek)
            var localMsg = new SmsMessage
            {
                Number = PhoneNumber,
                Text = textToSend,
                IsIncoming = false // To nasza wiadomość (prawa strona)
            };
            ChatMessages.Add(localMsg);
            OnNewMessageAdded?.Invoke();

            // 2. FIZYCZNE WYSŁANIE PRZEZ MODEM/SYSTEM
            await _smsClient.SendAsync(PhoneNumber, textToSend);
        }
    }
}
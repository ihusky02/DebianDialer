using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DebianDialer.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DebianDialer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly IOfonoClient _ofono;

    public MainWindowViewModel(IOfonoClient ofono)
    {
        _ofono = ofono;
        _ofono.IncomingCallReceived += OnIncomingCall;
        _ = _ofono.ConnectAsync();
    }

    private void OnIncomingCall(string number)
    {
        ExecuteBash($"notify-send 'Ktoś dzwoni!' 'Numer: {number}' --icon=call-start -u critical");
        Dispatcher.UIThread.InvokeAsync(() => PhoneNumber = number);
    }

    [ObservableProperty]
    private string _phoneNumber = "";

    [RelayCommand]
    private async Task Dial() => await _ofono.DialAsync(PhoneNumber);

    [RelayCommand]
    private async Task Answer() => await _ofono.AnswerAsync();

    [RelayCommand]
    private async Task Hangup() 
    { 
        await _ofono.HangupAsync();
        ExecuteBash("pactl unload-module module-loopback");
    }

    // --- PRZYWRÓCONE KOMENDY AUDIO ---

    [RelayCommand]
    private void EnableAudioBridge() => ExecuteBash("pactl load-module module-loopback");

    [RelayCommand]
    private void DisableAudioBridge() => ExecuteBash("pactl unload-module module-loopback");

    [RelayCommand]
    private void OpenSoundSettings() => ExecuteBash("pavucontrol");

    // ---------------------------------

    private void ExecuteBash(string command)
    {
        try { Process.Start(new ProcessStartInfo { FileName = "bash", Arguments = $"-c \"{command}\"", UseShellExecute = false, CreateNoWindow = true }); } catch { }
    }
}

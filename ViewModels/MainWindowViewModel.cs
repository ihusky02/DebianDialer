using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DebianDialer.Services;
using System.Diagnostics;
using System.Threading.Tasks;

namespace DebianDialer.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    private readonly OfonoClient _ofono = new();

    public MainWindowViewModel()
    {
        // Podpinamy się pod nowe zdarzenie z modemu
        _ofono.IncomingCallReceived += OnIncomingCall;
        _ = _ofono.ConnectAsync();
    }

    private void OnIncomingCall(string number)
    {
        // 1. Wyświetlamy powiadomienie systemowe (Dymek w XFCE)
        // -u critical sprawia, że powiadomienie nie zniknie zbyt szybko
        ExecuteBash($"notify-send 'Ktoś dzwoni!' 'Numer: {number}' --icon=call-start -u critical");

        // 2. Automatycznie wpisujemy numer dzwoniącego do okna dialera
        // Ponieważ ten kod wykonuje się w tle, musimy użyć Dispatchera do aktualizacji UI
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            PhoneNumber = number;
        });
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
        DisableAudioBridge();
    }

    [RelayCommand]
    private void OpenSoundSettings()
    {
        ExecuteBash("pavucontrol");
    }

    [RelayCommand]
    private void EnableAudioBridge()
    {
        string micToPhone = "pactl load-module module-loopback latency_msec=200 source=@DEFAULT_SOURCE@ sink=$(pactl list short sinks | grep -i bluez | head -n 1 | awk '{print $2}')";
        string phoneToSpeaker = "pactl load-module module-loopback latency_msec=200 source=$(pactl list short sources | grep -i bluez | head -n 1 | awk '{print $2}') sink=@DEFAULT_SINK@";

        ExecuteBash(micToPhone);
        ExecuteBash(phoneToSpeaker);
    }

    [RelayCommand]
    private void DisableAudioBridge()
    {
        ExecuteBash("pactl unload-module module-loopback");
    }

    private void ExecuteBash(string command)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "bash",
                Arguments = $"-c \"{command}\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });
        }
        catch { }
    }
}

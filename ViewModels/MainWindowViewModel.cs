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
        _ = _ofono.ConnectAsync();
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
        DisableAudioBridge(); // Automatycznie wyłączamy nasłuch przy rozłączeniu
    }

    [RelayCommand]
    private void OpenSoundSettings()
    {
        try { Process.Start(new ProcessStartInfo { FileName = "pavucontrol", UseShellExecute = true }); } catch { }
    }

    [RelayCommand]
    private void EnableAudioBridge()
    {
        // 1. Z mikrofonu PC do Telefonu (rozmówca nas słyszy)
        string micToPhone = "pactl load-module module-loopback source=@DEFAULT_SOURCE@ sink=$(pactl list short sinks | grep -i bluez | head -n 1 | awk '{print $2}')";
        
        // 2. Z Telefonu na głośniki PC (my słyszymy rozmówcę)
        string phoneToSpeaker = "pactl load-module module-loopback source=$(pactl list short sources | grep -i bluez | head -n 1 | awk '{print $2}') sink=@DEFAULT_SINK@";

        ExecuteBash(micToPhone);
        ExecuteBash(phoneToSpeaker);
    }

    [RelayCommand]
    private void DisableAudioBridge()
    {
        // Usuwa wszystkie wirtualne mostki dźwiękowe
        ExecuteBash("pactl unload-module module-loopback");
    }

    private void ExecuteBash(string command)
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
}

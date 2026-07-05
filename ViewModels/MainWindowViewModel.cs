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
        // Dodano latency_msec=200, aby zredukować trzaski
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

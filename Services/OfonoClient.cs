using System.Threading.Tasks;
using Tmds.DBus;

namespace DebianDialer.Services;

// Definiujemy interfejs, który D-Bus automatycznie zmapuje na oFono
[DBusInterface("org.ofono.VoiceCallManager")]
public interface IVoiceCallManager : IDBusObject
{
    Task DialAsync(string number, string hideCallerId);
    Task AnswerAsync();
    Task HangupAllAsync();
}

public class OfonoClient
{
    private const string ServiceName = "org.ofono";
    private const string ModemPath = "/hfp/org/bluez/hci0/dev_48_EF_1C_C6_06_5F";
    private IVoiceCallManager? _voiceCallManager;

    public async Task ConnectAsync()
    {
        // Podłączamy się pod magistralę systemową
        var connection = Connection.System;
        
        // Tworzymy "proxy" - obiekt, który łączy nasz interfejs z oFono
        _voiceCallManager = connection.CreateProxy<IVoiceCallManager>(ServiceName, ModemPath);
        
        await Task.CompletedTask;
    }

    public async Task DialAsync(string number) 
    {
        if (_voiceCallManager != null)
            await _voiceCallManager.DialAsync(number, "default");
    }

    public async Task AnswerAsync()
    {
        if (_voiceCallManager != null)
            await _voiceCallManager.AnswerAsync();
    }

    public async Task HangupAsync()
    {
        if (_voiceCallManager != null)
            await _voiceCallManager.HangupAllAsync();
    }
}

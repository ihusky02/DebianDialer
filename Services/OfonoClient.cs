using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace DebianDialer.Services;

// Interfejs do zarządzania całym modemem
[DBusInterface("org.ofono.VoiceCallManager")]
public interface IVoiceCallManager : IDBusObject
{
    Task DialAsync(string number, string hideCallerId);
    Task HangupAllAsync();
    
    // Zwraca listę aktywnych/dzwoniących połączeń (ścieżka i ich właściwości)
    Task<(ObjectPath path, IDictionary<string, object> properties)[]> GetCallsAsync();
}

// Interfejs dla konkretnego połączenia telefonicznego
[DBusInterface("org.ofono.VoiceCall")]
public interface IVoiceCall : IDBusObject
{
    Task AnswerAsync();
}

public class OfonoClient
{
    private const string ServiceName = "org.ofono";
    private const string ModemPath = "/hfp/org/bluez/hci0/dev_48_EF_1C_C6_06_5F";
    private IVoiceCallManager? _voiceCallManager;
    private Connection? _connection;

    public async Task ConnectAsync()
    {
        _connection = Connection.System;
        _voiceCallManager = _connection.CreateProxy<IVoiceCallManager>(ServiceName, ModemPath);
        await Task.CompletedTask;
    }

    public async Task DialAsync(string number) 
    {
        if (_voiceCallManager != null)
            await _voiceCallManager.DialAsync(number, "default");
    }

    public async Task AnswerAsync()
    {
        if (_voiceCallManager == null || _connection == null) return;
        
        try
        {
            // 1. Pobieramy wszystkie aktualne połączenia
            var calls = await _voiceCallManager.GetCallsAsync();
            
            // 2. Przechodzimy przez nie i próbujemy je odebrać
            foreach (var call in calls)
            {
                var voiceCall = _connection.CreateProxy<IVoiceCall>(ServiceName, call.path);
                try 
                { 
                    await voiceCall.AnswerAsync(); 
                } 
                catch 
                { 
                    // Jeśli połączenie nie jest w stanie "dzwoniącym", oFono zignoruje polecenie.
                    // Łapiemy błąd, żeby aplikacja nie zamknęła się (Crash).
                }
            }
        }
        catch { }
    }

    public async Task HangupAsync()
    {
        if (_voiceCallManager != null)
            await _voiceCallManager.HangupAllAsync();
    }
}

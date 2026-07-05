using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace DebianDialer.Services;

[DBusInterface("org.ofono.VoiceCallManager")]
public interface IVoiceCallManager : IDBusObject
{
    Task DialAsync(string number, string hideCallerId);
    Task HangupAllAsync();
    Task<(ObjectPath path, IDictionary<string, object> properties)[]> GetCallsAsync();
    
    // Nowa metoda do nasłuchiwania przychodzących połączeń
    Task<IDisposable> WatchCallAddedAsync(Action<(ObjectPath path, IDictionary<string, object> properties)> handler, Action<Exception>? onError = null);
}

[DBusInterface("org.ofono.VoiceCall")]
public interface IVoiceCall : IDBusObject
{
    Task AnswerAsync();
}

public class OfonoClient
{
    private const string ServiceName = "org.ofono";
    private const string ModemPath = "/hfp/org/bluez/hci0/dev_48_EF_1C_C6_06_5F"; // Zmień jeśli zmienił się adres MAC Twojego telefonu
    private IVoiceCallManager? _voiceCallManager;
    private Connection? _connection;

    // Zdarzenie, które odpalamy w momencie przychodzącego połączenia
    public event Action<string>? IncomingCallReceived;

    public async Task ConnectAsync()
    {
        _connection = Connection.System;
        _voiceCallManager = _connection.CreateProxy<IVoiceCallManager>(ServiceName, ModemPath);
        
        // Rozpoczynamy nasłuchiwanie na nowe połączenia
        if (_voiceCallManager != null)
        {
            await _voiceCallManager.WatchCallAddedAsync(OnCallAdded);
        }
    }

    private void OnCallAdded((ObjectPath path, IDictionary<string, object> properties) args)
    {
        // Sprawdzamy czy nowe połączenie jest w stanie "incoming" (przychodzące)
        if (args.properties.TryGetValue("State", out var stateObj) && stateObj?.ToString() == "incoming")
        {
            string number = "Nieznany numer";
            
            // Próbujemy wyciągnąć numer telefonu z właściwości
            if (args.properties.TryGetValue("LineIdentification", out var lineIdObj) && lineIdObj != null)
            {
                var id = lineIdObj.ToString();
                if (!string.IsNullOrWhiteSpace(id)) number = id;
            }
            
            // Informujemy ViewModel
            IncomingCallReceived?.Invoke(number);
        }
    }

    public async Task DialAsync(string number) 
    {
        if (_voiceCallManager != null) await _voiceCallManager.DialAsync(number, "default");
    }

    public async Task AnswerAsync()
    {
        if (_voiceCallManager == null || _connection == null) return;
        try
        {
            var calls = await _voiceCallManager.GetCallsAsync();
            foreach (var call in calls)
            {
                var voiceCall = _connection.CreateProxy<IVoiceCall>(ServiceName, call.path);
                try { await voiceCall.AnswerAsync(); } catch { }
            }
        }
        catch { }
    }

    public async Task HangupAsync()
    {
        if (_voiceCallManager != null) await _voiceCallManager.HangupAllAsync();
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Tmds.DBus;

namespace DebianDialer.Services
{
    public class OfonoClient : IOfonoClient
    {
        private readonly Connection _connection = Connection.System;

        public event Action<string>? IncomingCallReceived;

        public Task ConnectAsync() => Task.CompletedTask;

        // Prawdziwa metoda ODBIERANIA
        public async Task AnswerAsync()
        {
            var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
            var modems = await manager.GetModemsAsync();
            if (modems.Length == 0) return;

            var modemPath = modems[0].Item1;
            var callManager = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", modemPath);
            
            // Pobieramy listę wszystkich aktualnych połączeń
            var calls = await callManager.GetCallsAsync();
            foreach (var call in calls)
            {
                // Szukamy tego, które właśnie dzwoni ("incoming")
                if (call.properties.TryGetValue("State", out var stateObj) && stateObj.ToString() == "incoming")
                {
                    // Łączymy się z tym konkretnym połączeniem i odbieramy
                    var voiceCall = _connection.CreateProxy<IOfonoVoiceCall>("org.ofono", call.path);
                    await voiceCall.AnswerAsync();
                    break;
                }
            }
        }

        // Prawdziwa metoda ROZŁĄCZANIA / ODRZUCANIA
        public async Task HangupAsync()
        {
            var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
            var modems = await manager.GetModemsAsync();
            if (modems.Length == 0) return;

            var modemPath = modems[0].Item1;
            var callManager = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", modemPath);
            
            // Komenda HangupAll kończy wszystkie aktywne i dzwoniące rozmowy
            await callManager.HangupAllAsync();
        }

        public async Task DialAsync(string number)
        {
            var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
            var modems = await manager.GetModemsAsync();
            if (modems.Length == 0) throw new Exception("Brak modemu!");

            var modemPath = modems[0].Item1;
            var callManager = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", modemPath);
            await callManager.DialAsync(number, "default");
        }

        public async Task StartListeningAsync()
        {
            try
            {
                var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
                var modems = await manager.GetModemsAsync();
                if (modems.Length == 0) return;

                var modemPath = modems[0].Item1;
                var callManager = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", modemPath);

                await callManager.WatchCallAddedAsync(
                    handler: e => 
                    {
                        // Wyciągamy numer telefonu z metadanych połączenia oFono
                        string number = "Nieznany";
                        if (e.properties.TryGetValue("LineIdentification", out var idObj))
                        {
                            number = idObj.ToString() ?? "Nieznany";
                        }
                        
                        // Przekazujemy prawdziwy numer do ViewModelu!
                        IncomingCallReceived?.Invoke(number);
                    },
                    onError: ex => Console.WriteLine($"Błąd nasłuchiwania: {ex.Message}")
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Nie udało się uruchomić nasłuchiwania: {ex.Message}");
            }
        }
    }

    [DBusInterface("org.ofono.Manager")]
    public interface IOfonoManager : IDBusObject
    {
        Task<(ObjectPath, IDictionary<string, object>)[]> GetModemsAsync();
    }

    [DBusInterface("org.ofono.VoiceCallManager")]
    public interface IOfonoVoiceCallManager : IDBusObject
    {
        Task DialAsync(string number, string hideCallerId);
        Task HangupAllAsync();
        
        // Zwraca listę aktywnych połączeń
        Task<(ObjectPath path, IDictionary<string, object> properties)[]> GetCallsAsync();
        
        Task<IDisposable> WatchCallAddedAsync(Action<(ObjectPath path, IDictionary<string, object> properties)> handler, Action<Exception>? onError = null);
    }

    // Dodajemy nowy interfejs do obsługi konkretnego połączenia (odebranie)
    [DBusInterface("org.ofono.VoiceCall")]
    public interface IOfonoVoiceCall : IDBusObject
    {
        Task AnswerAsync();
        Task HangupAsync();
    }
}

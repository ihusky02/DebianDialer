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
        public Task AnswerAsync() => Task.CompletedTask;
        public Task HangupAsync() => Task.CompletedTask;

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

                // Subskrypcja sygnału CallAdded na odpowiednim interfejsie
                await callManager.WatchCallAddedAsync(
                    handler: e => 
                    {
                        // e.path to ścieżka do nowego połączenia
                        Process.Start("notify-send", "\"DebianDialer\" \"Wykryto przychodzące połączenie!\"");
                        
                        // Wykorzystujemy zdarzenie, by pozbyć się ostrzeżenia kompilatora
                        IncomingCallReceived?.Invoke("Przychodzące połączenie!");
                    },
                    onError: ex => 
                    {
                        Console.WriteLine($"Błąd nasłuchiwania D-Bus: {ex.Message}");
                    }
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
        
        // Właściwa deklaracja sygnału dla Tmds.DBus
        Task<IDisposable> WatchCallAddedAsync(Action<(ObjectPath path, IDictionary<string, object> properties)> handler, Action<Exception>? onError = null);
    }
}

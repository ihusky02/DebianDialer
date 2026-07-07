using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace DebianDialer.Services
{
    public class OfonoClient : IOfonoClient
    {
        private readonly Connection _connection = Connection.System;

        // --- Metoda, o którą walczyliśmy ---
        public async Task DialAsync(string number)
        {
            var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
            var modems = await manager.GetModemsAsync();
            if (modems.Length == 0) throw new Exception("Brak modemu!");

            var modemPath = modems[0].Item1;
            var callManager = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", modemPath);
            await callManager.DialAsync(number, "default");
        }

        // --- Brakujące metody wymagane przez Twój interfejs ---
        public event Action<string>? IncomingCallReceived;
        public Task ConnectAsync() => Task.CompletedTask;
        public Task AnswerAsync() => Task.CompletedTask;
        public Task HangupAsync() => Task.CompletedTask;
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
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Tmds.DBus;

namespace DebianDialer.Services
{
    // USUNIĘTO: public interface IOfonoClient { ... } (nie definiuj go tutaj!)

    public class OfonoClient : IOfonoClient // Tutaj używamy istniejącego interfejsu
    {
        private readonly Connection _connection;
        public event Action<string>? IncomingCallReceived;

        public OfonoClient()
        {
            _connection = Connection.System;
            _ = StartListeningAsync();
        }

        public async Task ConnectAsync() => await Task.CompletedTask;
        
        public async Task StartListeningAsync()
        {
            try
            {
                var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
                var modems = await manager.GetModemsAsync();
                foreach (var modem in modems)
                {
                    var callManager = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", modem.Item1);
                    await callManager.WatchCallAddedAsync(
                        handler: e => {
                            string number = e.properties.TryGetValue("LineIdentification", out var id) ? id.ToString() : "Nieznany";
                            IncomingCallReceived?.Invoke(number);
                        },
                        onError: ex => Console.WriteLine(ex.Message)
                    );
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }
        }

        public async Task AnswerAsync()
        {
            var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
            var modems = await manager.GetModemsAsync();
            foreach (var m in modems)
            {
                var cm = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", m.Item1);
                var calls = await cm.GetCallsAsync();
                foreach (var call in calls)
                {
                    if (call.properties.TryGetValue("State", out var s) && s.ToString() == "incoming")
                    {
                        var vc = _connection.CreateProxy<IOfonoVoiceCall>("org.ofono", call.path);
                        await vc.AnswerAsync();
                    }
                }
            }
        }

        public async Task HangupAsync()
        {
            var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
            var modems = await manager.GetModemsAsync();
            foreach (var m in modems)
            {
                var cm = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", m.Item1);
                await cm.HangupAllAsync();
            }
        }

        public async Task DialAsync(string number)
        {
            var manager = _connection.CreateProxy<IOfonoManager>("org.ofono", "/");
            var modems = await manager.GetModemsAsync();
            if (modems.Length > 0)
            {
                var cm = _connection.CreateProxy<IOfonoVoiceCallManager>("org.ofono", modems[0].Item1);
                await cm.DialAsync(number, "default");
            }
        }
    }

    // Interfejsy D-Bus wymagane do poprawnej pracy
    [DBusInterface("org.ofono.Manager")]
    public interface IOfonoManager : IDBusObject { Task<(ObjectPath, IDictionary<string, object>)[]> GetModemsAsync(); }

    [DBusInterface("org.ofono.VoiceCallManager")]
    public interface IOfonoVoiceCallManager : IDBusObject 
    { 
        Task DialAsync(string number, string hide);
        Task HangupAllAsync();
        Task<(ObjectPath path, IDictionary<string, object> properties)[]> GetCallsAsync();
        Task<IDisposable> WatchCallAddedAsync(Action<(ObjectPath path, IDictionary<string, object> properties)> handler, Action<Exception> onError);
    }

    [DBusInterface("org.ofono.VoiceCall")]
    public interface IOfonoVoiceCall : IDBusObject { Task AnswerAsync(); }
}

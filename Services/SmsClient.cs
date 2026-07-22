using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DebianDialer.Services;

public class SmsMessage
{
    public string Number { get; set; } = "";
    public string Text { get; set; } = "";
    public bool IsIncoming { get; set; }
    
    // DODANE: Pomaga w łatwym stylowaniu czatu w Avalonia (dymki lewy/prawy)
    public bool IsOutgoing => !IsIncoming; 
    
    public override string ToString() => $"{(IsIncoming ? "Odebrane od" : "Wysłane do")}: {Number}\n{Text}\n";
}

public class SmsClient
{
    private readonly string _host = "xxx.xxx.xxx.xx";
    private readonly string _username = "xxxxxxx";
    private readonly string _password = "xxxxxxx;

    public async Task<List<SmsMessage>> GetLatestMessagesAsync()
    {
        var messages = new List<SmsMessage>();
        await Task.Run(() =>
        {
            using var client = new SshClient(_host, _username, _password);
            try
            {
                client.Connect();
                
                string sqlCommand = "sqlite3 -separator '|||' /home/defaultuser/.local/share/commhistory/commhistory.db \"SELECT remoteUid, direction, freeText FROM events WHERE type=2 ORDER BY startTime DESC LIMIT 10;\"";
                
                var cmd = client.CreateCommand(sqlCommand);
                var result = cmd.Execute();
                
                Console.WriteLine($"[SMS DEBUG] Otrzymano tekst z bazy (długość: {result.Length} znaków).");
                if (!string.IsNullOrEmpty(cmd.Error))
                {
                    Console.WriteLine($"[SMS BŁĄD BAZY]: {cmd.Error}");
                }
                
                var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { "|||" }, 3, StringSplitOptions.None);
                    if (parts.Length == 3)
                    {
                        messages.Add(new SmsMessage
                        {
                            Number = parts[0],
                            IsIncoming = parts[1] == "1",
                            Text = parts[2].Replace("\\n", "\n")
                        });
                    }
                }
                Console.WriteLine($"[SMS DEBUG] Udało się przetworzyć {messages.Count} wiadomości.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd pobierania SMS: {ex.Message}");
            }
            finally
            {
                if (client.IsConnected) client.Disconnect();
            }
        });
        return messages;
    }

    public async Task<List<SmsMessage>> GetConversationAsync(string number, int limit = 50, int offset = 0)
    {
        var messages = new List<SmsMessage>();
        await Task.Run(() =>
        {
            using var client = new SshClient(_host, _username, _password);
            try
            {
                client.Connect();
                
                string cleanNum = number.Replace(" ", "").Replace("-", "").Replace("+48", "");
                
                string sqlCommand = $"sqlite3 -separator '|||' /home/defaultuser/.local/share/commhistory/commhistory.db \"SELECT remoteUid, direction, freeText FROM events WHERE type=2 AND remoteUid LIKE '%{cleanNum}%' ORDER BY startTime DESC LIMIT {limit} OFFSET {offset};\"";
                
                var cmd = client.CreateCommand(sqlCommand);
                var result = cmd.Execute();
                
                var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    var parts = line.Split(new[] { "|||" }, 3, StringSplitOptions.None);
                    if (parts.Length == 3)
                    {
                        messages.Add(new SmsMessage
                        {
                            Number = parts[0],
                            IsIncoming = parts[1] == "1",
                            Text = parts[2].Replace("\\n", "\n")
                        });
                    }
                }
                
                messages.Reverse();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd pobierania czatu: {ex.Message}");
            }
            finally
            {
                if (client.IsConnected) client.Disconnect();
            }
        });
        return messages;
    }

    public async Task SendAsync(string number, string message)
    {
        await Task.Run(() =>
        {
            using var client = new SshClient(_host, _username, _password);
            try
            {
                client.Connect();
                
                string cleanNumber = number.Replace(" ", "").Replace("-", "");
                string formattedNumber = cleanNumber.StartsWith("+") ? cleanNumber : "+48" + cleanNumber;
                string escapedMessage = message.Replace("\"", "\\\"");
                
                // 1. WYSYŁANIE PRZEZ MODEM
                string command = $"sudo /usr/local/bin/send.sh \"{formattedNumber}\" \"{escapedMessage}\"";
                var cmd = client.CreateCommand(command);
                string result = cmd.Execute();
                
                Console.WriteLine($"--- DEBUG ---");
                Console.WriteLine($"Wynik: {result}");
                Console.WriteLine($"Błędy: {cmd.Error}");
                Console.WriteLine($"Exit Status: {cmd.ExitStatus}");

                if (cmd.ExitStatus == 0 && result.Contains("/ril_0/message"))
                {
                    Console.WriteLine($"Sukces: SMS do {formattedNumber} został przekazany do modemu.");
                    
                    // 2. NOWE: RĘCZNE DOPISANIE WIADOMOŚCI DO BAZY HISTORII
                    // Zabezpieczamy apostrofy przed zapytaniem SQL
                    string sqlText = message.Replace("'", "''"); 
                    // direction=2 oznacza wysłaną wiadomość
                    string insertSql = $"sqlite3 /home/defaultuser/.local/share/commhistory/commhistory.db \"INSERT INTO events (type, remoteUid, direction, freeText, startTime) VALUES (2, '{formattedNumber}', 2, '{sqlText}', strftime('%s','now'));\"";
                    
                    var insertCmd = client.CreateCommand(insertSql);
                    insertCmd.Execute();
                    Console.WriteLine("Wiadomość została poprawnie zapisana w bazie commhistory.db!");
                }
                else
                {
                    Console.WriteLine("Wysyłanie nie powiodło się.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Błąd krytyczny podczas wysyłania SMS: {ex.Message}");
            }
            finally
            {
                if (client.IsConnected) client.Disconnect();
            }
        });
    }
}
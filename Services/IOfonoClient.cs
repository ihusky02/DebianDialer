using System;
using System.Threading.Tasks;

namespace DebianDialer.Services;

public interface IOfonoClient
{
    event Action<string>? IncomingCallReceived;
    Task ConnectAsync();
    Task DialAsync(string number);
    Task AnswerAsync();
    Task HangupAsync();
}

using Moq;
using Xunit;
using DebianDialer.ViewModels;
using DebianDialer.Services;

namespace DebianDialer.Tests;

public class MainWindowViewModelTests
{
    [Fact]
    public void DialCommand_ShouldCallDialAsyncOnOfonoClient()
    {
        // 1. ARRANGE (Przygotowanie)
        // Tworzymy udawanego klienta modemu
        var mockOfono = new Mock<IOfonoClient>();
        
        // Tworzymy ViewModel i wstrzykujemy mu nasz sztuczny modem
        var viewModel = new MainWindowViewModel(mockOfono.Object);
        
        // Wpisujemy testowy numer telefonu
        viewModel.PhoneNumber = "500600700";

        // 2. ACT (Działanie)
        // Symulujemy kliknięcie przycisku "Zadzwoń" w interfejsie
        viewModel.DialCommand.Execute(null);

        // 3. ASSERT (Sprawdzenie)
        // Oczekujemy, że ViewModel wywołał funkcję DialAsync z poprawnym numerem dokładnie JEDEN raz
        mockOfono.Verify(m => m.DialAsync("500600700"), Times.Once);
    }
}

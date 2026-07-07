using Xunit;
using DebianDialer.Services;

namespace DebianDialer.Tests;

public class VcfParserTests
{
    [Fact]
    public void ParseLines_ShouldExtractNameAndNumber()
    {
        // 1. ARRANGE (Przygotowanie)
        var fakeVcfLines = new[]
        {
            "BEGIN:VCARD",
            "VERSION:3.0",
            "FN:Jan Kowalski",
            "TEL;TYPE=CELL:+48123456789",
            "END:VCARD"
        };
        var parser = new VcfParser();

        // 2. ACT (Działanie)
        var results = parser.ParseLines(fakeVcfLines);

        // 3. ASSERT (Sprawdzenie)
        Assert.Single(results); // Oczekujemy, że znajdzie dokładnie 1 kontakt
        Assert.Equal("Jan Kowalski", results[0].Name);
        Assert.Equal("+48123456789", results[0].PhoneNumber);
    }
}

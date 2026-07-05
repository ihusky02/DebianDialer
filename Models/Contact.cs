namespace DebianDialer.Models;

public class Contact
{
    public string Name { get; set; } = "Nieznany";
    public string PhoneNumber { get; set; } = "";
    
    // Proste i niezawodne łączenie stringów
    public override string ToString() => Name + " (" + PhoneNumber + ")";
}

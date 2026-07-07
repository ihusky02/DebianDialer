using System.Collections.Generic;
using DebianDialer.Models;

namespace DebianDialer.Services;

public class VcfParser
{
    public List<Contact> ParseLines(string[] lines)
    {
        var contacts = new List<Contact>();
        Contact? current = null;
        
        foreach (var line in lines)
        {
            if (line.StartsWith("BEGIN:VCARD")) current = new Contact();
            else if (line.StartsWith("FN:") && current != null) current.Name = line.Substring(3);
            else if (line.StartsWith("TEL") && current != null)
            {
                var parts = line.Split(':');
                if (parts.Length > 1) current.PhoneNumber = parts[1];
            }
            else if (line.StartsWith("END:VCARD") && current != null)
            {
                if (!string.IsNullOrEmpty(current.PhoneNumber)) contacts.Add(current);
                current = null;
            }
        }
        return contacts;
    }
}

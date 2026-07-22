using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.People.v1;
using Google.Apis.People.v1.Data;
using Google.Apis.Services;

namespace DebianDialer.Services;

public class Contact
{
    public string Name { get; set; } = "Nieznany";
    public string Number { get; set; } = "";
}

public class GoogleContactsClient
{
    public async Task<List<Contact>> GetContactsAsync()
    {
        using var stream = new FileStream("credentials.json", FileMode.Open, FileAccess.Read);
        
        var secrets = GoogleClientSecrets.Load(stream).Secrets;
        
        var credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
            secrets,
            new[] { PeopleService.Scope.ContactsReadonly },
            "user",
            CancellationToken.None);

        var service = new PeopleService(new BaseClientService.Initializer 
        { 
            HttpClientInitializer = credential,
            ApplicationName = "DebianDialer" 
        });

        var contacts = new List<Contact>();
        string? pageToken = null;

        // Pętla pobierająca wszystkie strony kontaktów (stronicowanie)
        do
        {
            var request = service.People.Connections.List("people/me");
            request.RequestMaskIncludeField = "person.names,person.phoneNumbers";
            request.PageSize = 100; // Maksymalna wielkość strony
            request.PageToken = pageToken; // Ustawienie tokena dla kolejnej strony

            var response = await request.ExecuteAsync();

            if (response.Connections != null)
            {
                foreach (var person in response.Connections)
                {
                    if (person.PhoneNumbers != null && person.Names != null)
                    {
                        contacts.Add(new Contact {
                            Name = person.Names[0].DisplayName ?? "Nieznany",
                            Number = person.PhoneNumbers[0].Value ?? ""
                        });
                    }
                }
            }
            
            // Pobieramy token do następnej strony, jeśli istnieje
            pageToken = response.NextPageToken; 
        } 
        while (!string.IsNullOrEmpty(pageToken));

        return contacts;
    }
}

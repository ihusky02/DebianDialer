

## Wymagania
* System: Debian 13 (lub pochodne)
* Audio: PipeWire, wireplumber, libspa-0.2-bluetooth
* Działające środowisko Bluetooth (HFP/HSP)

## Instalacja zależności
```bash
sudo apt update
sudo apt install pipewire-audio-client-libraries libspa-0.2-bluetooth wireplumber
---------------------------------------------------------------------------------
# DebianDialer (Wersja V6)

Nowoczesna aplikacja desktopowa napisana w C# (Avalonia UI) przeznaczona na systemy Linux (np. Debian), pełniąca funkcję komunikatora i dialera. Integruje się z systemem SailfishOS poprzez protokół SSH, pozwalając na wysyłanie i odbieranie wiadomości SMS oraz korzystanie ze zsynchronizowanych kontaktów Google.

## 🛠 Wymagania wstępne i Konfiguracja

Aby aplikacja działała poprawnie z Twoim telefonem (SailfishOS) oraz książką adresową, musisz wykonać kilka kroków konfiguracyjnych.

### 1. Konfiguracja telefonu (SailfishOS)
Aplikacja łączy się z telefonem przez SSH, aby odczytywać bazę SQLite z historią wiadomości oraz wysyłać nowe SMS-y.

1. **Uruchom tryb deweloperski** w ustawieniach SailfishOS i włącz połączenia SSH. Ustal hasło.
2. Zaloguj się na telefon przez SSH i stwórz skrypt do wysyłania wiadomości:
   `sudo nano /usr/local/bin/send.sh`
3. Wklej do niego odpowiednie polecenie `dbus-send` komunikujące się z modemem (Ofono/RIL). *Uwaga: Treść skryptu zależy od konkretnego urządzenia i modemu.*
4. Nadaj skryptowi uprawnienia do wykonywania:
   `sudo chmod +x /usr/local/bin/send.sh`
5. *(Opcjonalnie)* Upewnij się, że użytkownik `defaultuser` ma prawo wykonać ten skrypt jako root (np. poprzez dodanie reguły w `visudo`), jeśli wymaga tego modem.

### 2. Konfiguracja połączenia w kodzie aplikacji
Nie udostępniamy haseł w repozytorium. Zanim skompilujesz projekt:

1. Otwórz plik `Services/SmsClient.cs`.
2. Odszukaj zmienne na początku klasy `SmsClient`.
3. Podmień wartości z "x" na rzeczywiste dane Twojego telefonu:
   ```csharp
   private readonly string _host = "192.168.X.X"; // IP Twojego telefonu
   private readonly string _username = "defaultuser";
   private readonly string _password = "TwojeHasloSSH";
   ------------------------------------------------------------

   3. Własny plik credentials.json (Integracja z Kontaktami Google)
Repozytorium nie zawiera mojego prywatnego pliku credentials.json, co oznacza, że domyślnie aplikacja będzie wyświetlać tylko numery telefonów. Jeśli chcesz, aby aplikacja rozpoznawała nazwy Twoich kontaktów, musisz wygenerować własny plik kluczy od Google:

Przejdź na stronę Google Cloud Console.

Utwórz nowy projekt (np. "DebianDialer-Contacts").

W pasku wyszukiwania u góry wpisz Google People API i kliknij "Włącz" (Enable).

Przejdź do zakładki Dane logowania (Credentials) z lewego menu.

Kliknij Utwórz dane logowania (Create credentials) -> Identyfikator klienta OAuth (OAuth client ID).

Jako typ aplikacji wybierz Aplikacja na komputer (Desktop app) i nadaj jej dowolną nazwę.

Po utworzeniu, pobierz plik JSON (przycisk z ikoną pobierania).

Zmień nazwę pobranego pliku na dokładnie credentials.json i wrzuć go do głównego folderu z projektem (tam, gdzie znajduje się plik DebianDialer.csproj).

# DebianDialer
Aplikacja do obsługi połączeń telefonicznych na systemie Debian 13 przy użyciu oFono.

## Wymagania
* System: Debian 13 (lub pochodne)
* Audio: PipeWire, wireplumber, libspa-0.2-bluetooth
* Działające środowisko Bluetooth (HFP/HSP)

## Instalacja zależności
```bash
sudo apt update
sudo apt install pipewire-audio-client-libraries libspa-0.2-bluetooth wireplumber

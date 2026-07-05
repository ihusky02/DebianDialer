#!/bin/bash
# Pobiera aktualnie zaznaczony myszką tekst z systemu
SELECTED_TEXT=$(xclip -o -selection primary)

# Sprawdza, czy w ogóle coś zaznaczono, żeby nie otwierać pustego okna
if [ ! -z "$SELECTED_TEXT" ]; then
    # Uruchamia Twój dialer, wymuszając prefiks "tel:",
    # z którym aplikacja (dzięki ostatniej aktualizacji) świetnie sobie radzi!
    debiandialer "tel:$SELECTED_TEXT"
fi

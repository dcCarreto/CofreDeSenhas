# Sicherheitsbericht und Prüfung

Die Anwendung bewertet den Zustand des Tresors, ohne dass etwas den
Computer verlässt (außer der Leak-Prüfung, siehe unten).

## Sicherheitsbericht

Gibt eine **Gesamtbewertung** und listet die Probleme nach Art auf:

- **Schwache Passwörter**: kurz oder vorhersehbar. Ersetzen Sie sie mit
  dem [Passwortgenerator](gerador).
- **Wiederverwendete Passwörter**: dasselbe Passwort bei verschiedenen
  Diensten. Das größte praktische Risiko — beheben Sie diese zuerst.
- **Alte Passwörter**: lange nicht geändert, sofern der Nutzungsverlauf
  aktiv ist.

Ein Klick auf einen Eintrag filtert die Hauptliste auf die betroffenen
Einträge.

## Tresor-Prüfung

Ein ausführlicherer Durchlauf, Zugangsdatum für Zugangsdatum, mit dem
Grund für jeden Hinweis. Nützlich für eine vollständige Durchsicht von
Zeit zu Zeit.

## Leak-Prüfung

Vergleicht Ihre Passwörter mit öffentlichen Datenbanken bekannter Lecks
mittels **k-Anonymität**: Es wird nur ein Teil des Passwort-Hashes
gesendet, nie das Passwort oder der Dienst. Erscheint eine
Übereinstimmung, ändern Sie das Passwort so bald wie möglich.

## Bewertungsverlauf

Wenn Sie den Nutzungsverlauf aktiv lassen, zeichnet die Anwendung auf, wie
sich die Bewertung im Lauf der Zeit entwickelt, damit Sie sehen, ob der
Tresor besser wird.

# Häufige Fragen

## Ich habe das Master-Passwort vergessen. Was nun?

Es gibt keine Möglichkeit, es wiederherzustellen — nicht durch uns, nicht
durch irgendjemanden. Das ist der Preis der Verschlüsselung, die den
Tresor vor anderen schützt. Wenn Sie den **Sicherungs-QR-Code**
gespeichert haben, importieren Sie damit das Passwort erneut. Ohne das
Passwort oder den QR-Code ist der Inhalt des Tresors unzugänglich. Siehe
[Master-Passwort](senha-mestra).

## Wo liegen meine Daten?

In einer verschlüsselten Datei im Datenordner der Anwendung, innerhalb
Ihres Benutzerprofils im System. Nichts wird an Server von uns gesendet.
Einzelheiten in der [Einführung](introducao).

## Wie ziehe ich den Tresor auf einen anderen Computer um?

Exportieren Sie den Tresor (verschlüsselte Datei) oder kopieren Sie eine
Sicherung, installieren Sie die Anwendung auf dem Zielcomputer und
importieren Sie. Schritt für Schritt in
[Importieren und Exportieren](importar-exportar).

## Synchronisiert sich der Tresor selbst in die Cloud?

Nein. Es gibt keine Cloud von uns. Sie können die
[Ordner-Synchronisierung](sincronizacao) oder eine
[Datenbank](banco-de-dados) einrichten, die **Sie** kontrollieren — dann
läuft der Abgleich über Ihre Infrastruktur.

## Ist es sicher? Welche Verschlüsselung wird verwendet?

AES-256-GCM, mit dem Schlüssel, der bei jedem Öffnen aus Ihrem
Master-Passwort abgeleitet und nie gespeichert wird. Die praktische Stärke
hängt vor allem davon ab, dass Sie ein gutes Master-Passwort wählen.

## Ich habe das Telefon mit dem QR-Code verloren. Und Windows Hello?

[Windows Hello](windows-hello) ist an Ihr Windows-Konto auf diesem
Computer gebunden; der QR-Code ist davon unabhängig. Wenn Sie den Tresor
noch öffnen können (per Passwort oder per Hello), erzeugen Sie einen
**neuen QR-Code** unter *Einstellungen → Sicherheit* und bewahren Sie ihn
auf.

## Kann ich es nutzen, ohne außer der Anwendung etwas zu installieren?

Ja. Der lokale Tresor braucht keine Datenbank, keinen Server und kein
Konto. Die Netzwerkfunktionen sind alle optional — siehe
[Privatsphäre und Netzwerk](privacidade-rede).

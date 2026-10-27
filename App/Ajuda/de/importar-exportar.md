# Importieren und Exportieren

## Aus einem anderen Manager importieren

Unter **Einstellungen → Sicherung und Synchronisierung → Aus CSV
importieren**. Die meisten Manager exportieren eine **CSV**-Datei; die
Anwendung erkennt die gängigsten Formate und lässt Sie die
Spaltenzuordnung prüfen, bevor Sie abschließen.

> Die von anderen Programmen exportierte CSV ist **Klartext**,
> unverschlüsselt. Löschen Sie diese Datei nach dem Import sicher.

### Aus KeePass

Verwenden Sie im selben Abschnitt **Aus KeePass importieren (.kdbx)** und
geben Sie das Passwort der KeePass-Datenbank ein. Anders als CSV werden
auch der Passwortverlauf, die Anhänge und die benutzerdefinierten Felder
(in die Notizen) übernommen. Gruppen werden zu Tags, der Papierkorb von
KeePass wird ausgelassen.

Unterstützt werden Datenbanken im Format **KDBX 4**, die nur mit einem
Passwort geschützt sind. Liegt die Datenbank in KDBX 3.1 vor, erklärt die
Anwendung, wie man sie in KeePass oder KeePassXC umwandelt.

## Ihren Tresor exportieren

Der Export erzeugt eine **verschlüsselte** Datei, geschützt durch ein
Passwort, das Sie dabei festlegen. Nutzen Sie ihn, um auf einen anderen
Computer umzuziehen oder eine Langzeitkopie außerhalb des
[Sicherungsformats](backup) zu behalten.

Zum erneuten Importieren öffnen Sie die Anwendung auf dem Zielcomputer und
verwenden die Importoption auf der exportierten Datei, mit dem für den
Export verwendeten Passwort.

## Zu einem anderen Passwortmanager wechseln

Unter **Einstellungen → Sicherung und Synchronisierung → Unverschlüsselt
exportieren (CSV / Bitwarden)**. Nach Bestätigung des Master-Passworts
wählen Sie das Format:

- **CSV**: wird von Bitwarden und den meisten anderen Managern
  akzeptiert.
- **Bitwarden-JSON**: enthält zusätzlich den Passwortverlauf und die
  Zusatzfelder jedes Eintragstyps.

Einträge im Papierkorb und Anhänge werden nicht übernommen.

> Diese Datei ist **Klartext**: Jede Person und jedes Programm mit Zugriff
> darauf kann alle Ihre Passwörter lesen. Löschen Sie sie, sobald Sie sie
> in den anderen Manager importiert haben.

## Auf einen anderen Computer umziehen

1. Exportieren Sie den Tresor (oder kopieren Sie eine [Sicherung](backup)).
2. Installieren Sie die Anwendung auf dem anderen Computer.
3. Importieren Sie die Datei und geben Sie das Passwort ein.
4. Wenn beide abgeglichen bleiben sollen, richten Sie die
   [Ordner-Synchronisierung](sincronizacao) ein.

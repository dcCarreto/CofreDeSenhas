# Ordner-Synchronisierung

Hält denselben Tresor auf mehreren Ihrer Geräte über eine **gemeinsame
Datei** in einem Ordner Ihrer Wahl — meist ein Ordner eines
Dateidienstes, den Sie ohnehin nutzen (das Synchronisieren der Datei
selbst übernimmt dieser Dienst).

## Wie es funktioniert

- Die Anwendung schreibt eine verschlüsselte Datei in den gewählten
  Ordner, geschützt durch einen Schlüssel, der aus Ihrem Master-Passwort
  abgeleitet wird.
- Jedes Gerät liest und schreibt diese Datei in regelmäßigen Abständen.
- Ändern zwei Geräte denselben Eintrag, entscheidet die Anwendung nach der
  jüngsten Bearbeitung; Konflikte, die eine Entscheidung erfordern,
  erscheinen auf einem eigenen Bildschirm.

## Einrichten

Unter **Einstellungen → Sicherung und Synchronisierung →
Synchronisierung**. Geben Sie den Ordner an und legen Sie die Häufigkeit
fest. Wiederholen Sie das auf jedem Gerät, mit **demselben
Master-Passwort** und **demselben Ordner**.

## Was es nicht ist

Das ist **kein** Cloud-Dienst der Anwendung. Es gibt keinen Server von
uns dazwischen, kein Konto, und nichts wird an uns gesendet. Es ist Ihre
Infrastruktur, die Ihre Datei synchronisiert.

Wenn das Ziel ist, einen Tresor robuster zwischen Personen oder
Maschinen zu teilen, siehe [Gemeinsame Datenbank](banco-de-dados).

# Gemeinsame Datenbank

Eine fortgeschrittene Funktion für alle, die den Tresor in einer **von
Ihnen betriebenen Datenbank** halten wollen (SQLite im Netzwerk,
PostgreSQL oder MySQL) statt in einer lokalen Datei.

## Für wen es gedacht ist

- Teams oder Familien, die sich einen Satz Zugangsdaten teilen.
- Alle, die bereits einen Datenbankserver betreiben und lieber dort
  zentralisieren.

Um nur **Ihre eigenen** Geräte abzugleichen, ist die
[Ordner-Synchronisierung](sincronizacao) meist einfacher.

## Wie es funktioniert

- Sie geben Adresse und Zugangsdaten der Datenbank an. Das Serverpasswort
  wird verschlüsselt im lokalen Tresor abgelegt.
- Die Daten bleiben mit Ihrem Master-Passwort verschlüsselt, **bevor** sie
  in die Datenbank gehen — der Datenbankserver sieht nie Passwörter im
  Klartext.
- Dieselbe Merge-Logik wie bei der Synchronisierung (die jüngste
  Bearbeitung gewinnt, Konflikte gehen auf einen Entscheidungsbildschirm)
  hält die Geräte aufeinander abgestimmt.

## Verbinden und trennen

Unter **Einstellungen → Sicherung und Synchronisierung**. Beim Trennen
arbeitet der Tresor wieder nur mit der lokalen Kopie.

> Den Datenbankserver zu schützen (Zugriff, Netzwerk, Sicherungen) liegt
> in Ihrer Verantwortung. Die Anwendung kümmert sich um die
> Verschlüsselung des Inhalts, nicht um die Sicherheit Ihrer
> Infrastruktur.

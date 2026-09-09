# Windows Hello

Erlaubt das **Entsperren des Tresors per Biometrie** (Fingerabdruck,
Gesicht oder Windows-PIN), statt jedes Mal das Master-Passwort zu tippen.

## Aktivieren

Unter **Einstellungen → Sicherheit → Windows Hello aktivieren**. Windows
fragt nach Ihrer Bestätigung, und die Anwendung bietet fortan das
Entsperren per Hello auf dem Startbildschirm an.

## Was sich ändert und was nicht

- Das **Master-Passwort bleibt der eigentliche Schlüssel** zum Tresor.
  Hello gibt nur den Zugang frei, der unter dem Schutz des Systems
  gespeichert ist.
- Heikle Vorgänge (etwa das Ändern des Master-Passworts) fragen weiterhin
  nach dem Master-Passwort.
- Schlägt Hello fehl oder unterstützt das Gerät es nicht, können Sie sich
  immer mit dem Master-Passwort anmelden.

## Geltungsbereich

Die Hello-Anmeldung ist an **Ihr Windows-Konto auf diesem Computer**
gebunden. Sie wandert nicht mit der Tresordatei mit: auf einem anderen
Computer aktivieren Sie Hello erneut (oder nutzen das Master-Passwort).

## Deaktivieren

Auf demselben Bildschirm, *Windows Hello deaktivieren*. Der geschützte
Zugang wird entfernt und der Tresor öffnet wieder nur per Master-Passwort.

# Privatsphäre und Netzwerk

Standardmäßig **greift die Anwendung nicht auf das Internet zu**. Die
folgenden Funktionen sind optional, und Sie schalten jede einzeln ein, im
Wissen, was sie tut.

## Online-Symbole für Dienste

Ist dies aktiv, wird das echte Symbol jeder Seite von einem öffentlichen
Symboldienst geholt, wobei **nur die Domain** gesendet wird (zum Beispiel
`github.com`). Kein Passwort, kein Benutzername und keine Notiz verlässt
den Computer. Heruntergeladene Symbole werden auf der Festplatte
zwischengespeichert. Ist es aus, zeigt der Tresor nur Initialen und rührt
das Netzwerk nie an.

## Nach Updates suchen

Ist dies aktiv, wird die Release-Seite des Projekts abgefragt, um Sie auf
ein Update hinzuweisen. Es wird nichts automatisch
heruntergeladen, und außer dieser Abfrage wird nichts gesendet.
Standardmäßig aus.

## Leak-Prüfung

Auf Anforderung werden Ihre Passwörter mit öffentlichen Leak-Datenbanken
verglichen, mittels **k-Anonymität**: Es wird nur ein Präfix des
Passwort-Hashes gesendet. Siehe [Sicherheitsbericht](seguranca).

## Synchronisierung und Datenbank

Wenn Sie die [Ordner-Synchronisierung](sincronizacao) oder eine
[Datenbank](banco-de-dados) einrichten, geht der Verkehr an **Ihren**
Ordner oder **Ihren** Server — nie an einen Dienst von uns.

## Privatsphäre-Modus

Die Augen-Schaltfläche in der Titelleiste verwischt die sensiblen Werte
auf dem Bildschirm, damit Sie den Tresor neben anderen Menschen öffnen
können, ohne etwas preiszugeben.

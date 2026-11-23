# Updates

Die App **aktualisiert sich selbst**. Sie müssen nichts herunterladen und
nichts anklicken.

## So funktioniert es

1. Beim Öffnen des Tresors und danach alle 6 Stunden fragt die App die
   Release-Seite des Projekts ab.
2. Gibt es eine neue Version, wird sie im Hintergrund heruntergeladen. Bevor
   sie akzeptiert wird, prüft die App die digitale Signatur und den SHA256 der
   Datei; stimmt etwas nicht, wird das Update verworfen.
3. Ist das Update bereit, zeigt die Fußleiste **Update bereit: wird beim
   Sperren des Tresors installiert**.
4. Beim nächsten Sperren des Tresors (mit *Jetzt sperren* oder nach
   Inaktivität) schließt sich die App, installiert die neue Version und
   startet neu, direkt auf dem Entsperrbildschirm. Geben Sie einfach wie
   gewohnt Ihr Master-Passwort ein.

Um sofort zu installieren, verwenden Sie **Jetzt installieren** in der
Fußleiste: Es zeigt, was sich in der Version ändert, und fragt nach einer
Bestätigung, bevor die App geschlossen wird. Das **X** daneben blendet nur den
Hinweis aus; das Update bleibt für das nächste Sperren vorgemerkt.

Es werden keine Tresordaten gesendet. Abfrage und Download sind öffentliche
Lesezugriffe, ohne etwas, das Sie oder Ihren Computer identifiziert. Siehe
auch [Datenschutz und Netzwerk](privacidade-rede).

## Immer aktiv

Es gibt keine Option zum Ausschalten: So erreichen Korrekturen, auch
Sicherheitskorrekturen, alle, ohne dass jemand ans Aktualisieren denken muss.

## Je nach Installationsart

- **Installationsprogramm oder winget**: wie oben beschrieben. Wenn Sie „für
  alle Benutzer" (unter Programme) installiert haben, müssen Sie auf **Jetzt
  installieren** klicken, weil Windows nach Administratorrechten fragt; der
  Hinweis in der Fußleiste sagt Ihnen, wann das der Fall ist.
- **Portable Datei (eingestellt)**: Beim nächsten Update wird die App nur für
  Ihren Benutzer installiert, ohne Administratorrechte, mit demselben Tresor
  und denselben Einstellungen. Danach öffnet die portable Datei nur noch die
  installierte Version und kann gelöscht werden.
- **Microsoft Store**: Der Store aktualisiert, gemäß seiner Einstellung für
  App-Updates. Die App lädt das Update herunter, sobald der Store es anbietet,
  und installiert es beim Sperren des Tresors.
- **Linux, AppImage**: wie unter Windows; die `.AppImage`-Datei wird durch die
  neue ersetzt.
- **Linux, aus dem Quellcode gebaut**: aktualisiert sich nicht selbst.
  Aktualisieren Sie den Code und bauen Sie ihn neu.

Die Befehlszeile `cofre` und der eigenständige Passwortgenerator
(`GeradorDeSenhas-*-win-x64.exe`) sind separate Dateien und aktualisieren
sich **nicht** selbst. Laden Sie die neuen Versionen bei Bedarf von der
Release-Seite herunter.

## Ihre Daten während eines Updates

Tresor, Backups und Einstellungen bleiben unberührt: Sie liegen im
Datenordner der App, und die neue Version öffnet denselben Tresor. Auch das
Deinstallieren der App, einschließlich der Store-Version, löscht den Tresor
nicht. Die heruntergeladene Datei bleibt bis zur Installation im temporären
Ordner des Systems und wird beim nächsten Start der App gelöscht. Halten Sie
zur Sicherheit Ihre [Backups](backup) aktuell.

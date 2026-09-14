# Master-Passwort

Das Master-Passwort ist der einzige Schlüssel zum Tresor. Daraus leitet
die Anwendung — im Arbeitsspeicher — den Schlüssel ab, der Ihre Daten
entschlüsselt. Es wird **nirgends gespeichert**.

## Ohne Wiederherstellungsschlüssel kann es nicht wiederhergestellt werden

Standardmäßig gibt es kein „Passwort vergessen". Wenn Sie das
Master-Passwort verlieren, ohne den **Wiederherstellungsschlüssel** (siehe
unten) aktiviert zu haben, wird der Inhalt des Tresors unzugänglich —
genau so schützt Sie die Verschlüsselung vor Dritten. Deshalb:

- wählen Sie einen **langen Satz** (mehrere Wörter), leicht zu merken und
  schwer zu erraten;
- speichern Sie den **Sicherungs-QR-Code** (Menü *Sicherheit → QR-Code neu
  erzeugen*) und bewahren Sie ihn außerhalb des Computers auf;
- aktivieren Sie den **Wiederherstellungsschlüssel** als zweites
  Sicherheitsnetz;
- optional bewahren Sie eine schriftliche Kopie an einem sicheren
  physischen Ort auf.

## Master-Passwort ändern

Unter **Einstellungen → Sicherheit → Master-Passwort ändern**. Der ganze
Tresor wird mit dem neuen Schlüssel neu verschlüsselt. Wenn Sie eine
Datenbank oder die [Ordner-Synchronisierung](sincronizacao) nutzen,
betrifft die Änderung die anderen Geräte — lesen Sie den
Bestätigungsbildschirm, bevor Sie fortfahren.

> Nachdem die Änderung Erfolg meldet, startet die Anwendung von selbst
> neu. Rühren Sie den Tresor in diesem Zeitraum nicht an.

## Sicherungs-QR-Code

Das ist Ihr Master-Passwort als Bild kodiert, um es erneut zu
importieren, falls Sie das getippte vergessen. Behandeln Sie den QR-Code
mit derselben Sorgfalt wie das Passwort: Wer ihn hat, öffnet Ihren Tresor.

## Wiederherstellungsschlüssel

Unter *Einstellungen → Sicherheit → Wiederherstellungsschlüssel*, oder
schon bei der Tresor-Erstellung. Erzeugt ein 160-Bit-Geheimnis (in
Vierergruppen), mit dem sich das Master-Passwort zurücksetzen lässt, ohne
das alte zu kennen — wer dieses Geheimnis hat, kann das. Notieren Sie es
deshalb und bewahren Sie es **fern vom Computer** auf, mit derselben
Sorgfalt wie das Master-Passwort selbst: Es ist genauso viel wert.

Master-Passwort vergessen und den Schlüssel aktiviert gehabt? Klicken Sie
auf dem Anmeldebildschirm auf „Master-Passwort vergessen", geben Sie das
notierte Geheimnis ein und wählen Sie ein neues Passwort — der Tresor wird
damit neu verschlüsselt. Bei jeder Wiederherstellung und jeder
Master-Passwort-Änderung wird ein neues Geheimnis erzeugt; notieren Sie
immer das aktuellste, das vorherige verliert seine Gültigkeit. Aktivieren,
neu erzeugen oder deaktivieren verlangt immer das aktuelle
Master-Passwort, auch bei bereits entsperrtem Tresor.

## Windows Hello

[Windows Hello](windows-hello) erlaubt das Entsperren per Biometrie
anstatt das Master-Passwort zu tippen — aber das Master-Passwort bleibt
der eigentliche Schlüssel und ist für heikle Vorgänge weiterhin nötig.

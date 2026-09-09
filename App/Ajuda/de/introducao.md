# Einführung

Der **Passwort-Tresor** bewahrt Ihre Zugangsdaten verschlüsselt auf Ihrem
eigenen Computer auf. Es gibt kein Konto, keinen Server und keine
verpflichtende Cloud: Der Tresor ist eine Datei auf Ihrer Festplatte, die
nur durch Ihr Master-Passwort geöffnet wird.

## Wie Ihre Daten geschützt sind

- Alles wird mit **AES-256-GCM** verschlüsselt. Der Schlüssel wird nie auf
  die Festplatte geschrieben: Er wird bei jedem Öffnen des Tresors aus
  Ihrem Master-Passwort abgeleitet.
- Ohne das Master-Passwort ist die Tresordatei unlesbar — auch für
  jemanden mit Zugriff auf Ihren Computer.
- Nichts verlässt Ihren Computer von selbst. Die wenigen Funktionen, die
  das Netzwerk nutzen, sind optional und in
  [Datenschutz und Netzwerk](privacidade-rede) beschrieben.

## Wo der Tresor liegt

Der Tresor und die Einstellungen liegen im Datenordner der Anwendung,
innerhalb Ihres Systembenutzerprofils. Um alles auf einen anderen Computer
zu bringen, siehe [Importieren und Exportieren](importar-exportar) und
[Sicherung und Wiederherstellung](backup).

## Womit anfangen

Wenn Sie zum ersten Mal hier sind, folgen Sie
[Erste Schritte](primeiros-passos). Das Kernstück ist das
[Master-Passwort](senha-mestra) — wählen Sie ein gutes und verlieren Sie
es nicht.

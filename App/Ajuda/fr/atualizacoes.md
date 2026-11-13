# Mises à jour

L'application **se met à jour toute seule**. Vous n'avez rien à télécharger
ni à cliquer.

## Fonctionnement

1. À l'ouverture du coffre, puis toutes les 6 heures, l'application consulte
   la page des versions du projet.
2. S'il y a une nouvelle version, elle est téléchargée en arrière-plan. Avant
   de l'accepter, l'application vérifie la signature numérique et le SHA256
   du fichier ; si quelque chose ne correspond pas, la mise à jour est
   abandonnée.
3. Une fois la mise à jour prête, le bas de la fenêtre affiche **Mise à jour
   prête : elle sera installée au verrouillage du coffre**.
4. Au prochain verrouillage du coffre (avec *Verrouiller maintenant* ou après
   une période d'inactivité), l'application se ferme, installe la nouvelle
   version et se rouvre, directement sur l'écran de déverrouillage. Il suffit
   de saisir le mot de passe maître, comme d'habitude.

Pour l'installer tout de suite, utilisez **Installer maintenant** en bas de la
fenêtre : il montre ce qui change dans la version et demande confirmation
avant de fermer l'application. Le **X** à côté masque seulement l'avis ; la
mise à jour reste prévue pour le prochain verrouillage.

Aucune donnée du coffre n'est envoyée. La consultation et le téléchargement
sont des lectures publiques, sans rien qui vous identifie, vous ou votre
ordinateur. Voyez aussi [Confidentialité et réseau](privacidade-rede).

## Comment la désactiver

Dans *Paramètres → Comportement → Mettre à jour automatiquement*. Désactivée,
l'application ne consulte rien, et une mise à jour déjà téléchargée est
abandonnée. Pour mettre à jour à la main, téléchargez la nouvelle version sur
la page des versions du projet.

## Selon la façon dont vous l'avez installée

- **Installateur ou winget** : comme décrit ci-dessus. Si vous l'avez
  installée « pour tous les utilisateurs » (dans Program Files), vous devez
  cliquer sur **Installer maintenant**, car Windows demande l'autorisation
  d'un administrateur ; l'avis en bas de la fenêtre vous le signale.
- **Exécutable portable** : le fichier lui-même est remplacé par la nouvelle
  version. Il doit se trouver dans un dossier où vous pouvez écrire, comme
  Téléchargements ou le Bureau.
- **Microsoft Store** : c'est le Store qui met à jour, selon son réglage de
  mise à jour des applications. L'application télécharge la mise à jour dès
  que le Store la propose et l'installe au verrouillage du coffre. L'option
  « Mettre à jour automatiquement » n'apparaît pas.
- **Linux, AppImage** : comme sous Windows ; le fichier `.AppImage` est
  remplacé par le nouveau.
- **Linux, paquet `.tar.gz` ou compilé depuis le code source** : ne se met
  pas à jour tout seul. Téléchargez la nouvelle version sur la page des
  versions.

La ligne de commande `cofre` et le générateur de mots de passe autonome
(`GeradorDeSenhas-*-portatil.exe`) sont des fichiers séparés et **ne** se
mettent **pas** à jour tout seuls. Téléchargez les nouvelles versions sur la
page des versions quand vous le souhaitez.

## Vos données pendant la mise à jour

Le coffre, les sauvegardes et les préférences ne sont pas touchés : ils
restent dans le dossier de données de l'application, et la nouvelle version
ouvre le même coffre. Désinstaller l'application, y compris la version du
Store, ne supprime pas non plus le coffre. Le fichier téléchargé reste dans le
dossier temporaire du système jusqu'à son installation et il est supprimé à
l'ouverture suivante de l'application. Par précaution, gardez vos
[sauvegardes](backup) à jour.

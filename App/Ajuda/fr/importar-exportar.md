# Importer et exporter

## Importer depuis un autre gestionnaire

Dans **Paramètres → Sauvegarde et synchronisation → Importer depuis CSV**.
La plupart des gestionnaires exportent un fichier **CSV** ; l'application
reconnaît les formats les plus courants et vous laisse vérifier la
correspondance des colonnes avant de terminer.

> Le CSV exporté par d'autres programmes est en **texte clair**, non
> chiffré. Après l'import, supprimez ce fichier de manière sécurisée.

### Depuis KeePass

Utilisez **Importer depuis KeePass (.kdbx)**, dans la même section, et
saisissez le mot de passe de la base KeePass. Contrairement au CSV, cela
reprend aussi l’historique des mots de passe, les pièces jointes et les
champs personnalisés (ajoutés aux notes). Les groupes deviennent des
étiquettes et la corbeille de KeePass est ignorée.

Les bases au format **KDBX 4** protégées uniquement par un mot de passe
sont prises en charge. Si la base est en KDBX 3.1, l’application explique
comment la convertir dans KeePass ou KeePassXC.

## Exporter votre coffre

L'export produit un fichier **chiffré**, protégé par un mot de passe que
vous définissez sur le moment. Il sert à migrer vers un autre ordinateur
ou à garder une copie de long terme hors du format de
[Sauvegarde](backup).

Pour réimporter, ouvrez l'application sur la machine de destination et
utilisez l'option d'import sur le fichier exporté, en indiquant le mot de
passe utilisé pour l'export.

## Passer à un autre gestionnaire

Dans **Paramètres → Sauvegarde et synchronisation → Exporter sans
chiffrement (CSV / Bitwarden)**. Après avoir confirmé le mot de passe
maître, choisissez le format :

- **CSV** : accepté par Bitwarden et la plupart des gestionnaires.
- **JSON Bitwarden** : inclut aussi l'historique des mots de passe et les
  champs supplémentaires de chaque type d'identifiant.

Les éléments de la corbeille et les pièces jointes ne sont pas inclus.

> Ce fichier est **en clair** : toute personne ou tout programme y ayant
> accès peut lire tous vos mots de passe. Supprimez-le dès que vous avez
> fini de l'importer dans l'autre gestionnaire.

## Passer à un autre ordinateur

1. Exportez le coffre (ou copiez une [Sauvegarde](backup)).
2. Installez l'application sur l'autre ordinateur.
3. Importez le fichier et indiquez le mot de passe.
4. Si vous voulez garder les deux à jour, configurez la
   [Synchronisation par dossier](sincronizacao).

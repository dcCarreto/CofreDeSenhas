# Synchronisation par dossier

Garde le même coffre sur plusieurs de vos appareils via un **fichier
partagé** dans un dossier que vous choisissez — généralement un dossier
d'un service de fichiers que vous utilisez déjà (synchroniser le fichier
lui-même est le travail de ce service).

## Comment ça marche

- L'application écrit un fichier chiffré dans le dossier indiqué, protégé
  par une clé dérivée de votre mot de passe maître.
- Chaque appareil lit et écrit ce fichier périodiquement.
- Quand deux appareils modifient le même identifiant, l'application
  tranche par la modification la plus récente ; les conflits qui exigent
  une décision apparaissent sur un écran dédié.

## Configurer

Dans **Paramètres → Sauvegarde et synchronisation → Synchronisation**.
Indiquez le dossier et définissez la fréquence. Répétez sur chaque
appareil, avec le **même mot de passe maître** et le **même dossier**.

## Ce que ce n'est pas

Ce **n'est pas** un service cloud de l'application. Il n'y a aucun serveur
de notre part au milieu, aucun compte, et rien ne nous est envoyé. C'est
votre infrastructure qui synchronise votre fichier.

Si le but est de partager un coffre entre personnes ou machines de façon
plus robuste, voyez [Base de données partagée](banco-de-dados).

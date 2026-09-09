# Introduction

Le **Coffre de mots de passe** conserve vos identifiants chiffrés sur
votre propre ordinateur. Il n'y a ni compte, ni serveur, ni cloud
obligatoire : le coffre est un fichier sur votre disque, ouvert
uniquement par votre mot de passe maître.

## Comment vos données sont protégées

- Tout est chiffré avec **AES-256-GCM**. La clé n'est jamais écrite sur le
  disque : elle est dérivée de votre mot de passe maître à chaque
  ouverture du coffre.
- Sans le mot de passe maître, le fichier du coffre est illisible — y
  compris pour quelqu'un ayant accès à votre ordinateur.
- Rien ne quitte votre ordinateur de lui-même. Les rares fonctions qui
  utilisent le réseau sont facultatives et décrites dans
  [Confidentialité et réseau](privacidade-rede).

## Où se trouve le coffre

Le coffre et les préférences se trouvent dans le dossier de données de
l'application, dans votre profil utilisateur du système. Pour tout
transférer sur un autre ordinateur, voyez
[Importer et exporter](importar-exportar) et
[Sauvegarde et restauration](backup).

## Par où commencer

Si c'est votre première fois, suivez [Premiers pas](primeiros-passos). La
pièce maîtresse est le [Mot de passe maître](senha-mestra) : choisissez-en
un bon et ne le perdez pas.

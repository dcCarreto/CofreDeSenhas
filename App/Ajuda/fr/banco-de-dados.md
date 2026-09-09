# Base de données partagée

Une fonction avancée pour qui veut garder le coffre dans une **base de
données que vous hébergez** (SQLite en réseau, PostgreSQL ou MySQL) plutôt
que dans un fichier local.

## Pour qui

- Les équipes ou familles qui partagent un jeu d'identifiants.
- Ceux qui font déjà tourner un serveur de base de données et préfèrent
  centraliser là.

Pour synchroniser uniquement **vos propres** appareils, la
[Synchronisation par dossier](sincronizacao) est généralement plus
simple.

## Comment ça marche

- Vous fournissez l'adresse et les identifiants de la base de données. Le
  mot de passe du serveur est stocké chiffré dans le coffre local.
- Les données restent chiffrées avec votre mot de passe maître **avant**
  d'aller vers la base de données — le serveur ne voit jamais de mots de
  passe en clair.
- Le même moteur de fusion que la synchronisation (la modification la plus
  récente l'emporte, les conflits vont à un écran de décision) garde les
  appareils alignés.

## Connecter et déconnecter

Dans **Paramètres → Sauvegarde et synchronisation**. À la déconnexion, le
coffre repasse en fonctionnement avec la seule copie locale.

> Protéger le serveur de base de données (accès, réseau, sauvegardes) est
> votre responsabilité. L'application se charge du chiffrement du contenu,
> pas de la sécurité de votre infrastructure.

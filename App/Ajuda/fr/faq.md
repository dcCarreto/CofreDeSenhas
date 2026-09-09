# Questions fréquentes

## J'ai oublié le mot de passe maître. Et maintenant ?

Il n'y a aucun moyen de le récupérer — ni par nous, ni par personne.
C'est le prix du chiffrement qui protège le coffre des tiers. Si vous avez
enregistré le **QR code de secours**, utilisez-le pour réimporter le mot
de passe. Sans le mot de passe ni le QR, le contenu du coffre est
inaccessible. Voyez [Mot de passe maître](senha-mestra).

## Où sont mes données ?

Dans un fichier chiffré, dans le dossier de données de l'application, à
l'intérieur de votre profil utilisateur du système. Rien n'est envoyé vers
des serveurs de notre part. Détails dans [Introduction](introducao).

## Comment transférer le coffre sur un autre ordinateur ?

Exportez le coffre (fichier chiffré) ou copiez une sauvegarde, installez
l'application sur la machine de destination et importez. Étape par étape
dans [Importer et exporter](importar-exportar).

## Le coffre se synchronise-t-il tout seul dans le cloud ?

Non. Il n'y a aucun cloud de notre part. Vous pouvez configurer la
[Synchronisation par dossier](sincronizacao) ou une
[Base de données](banco-de-dados) que **vous** contrôlez — la
synchronisation passe alors par votre infrastructure.

## Est-ce sûr ? Quel chiffrement est utilisé ?

AES-256-GCM, avec la clé dérivée de votre mot de passe maître à chaque
ouverture et jamais stockée. La robustesse en pratique dépend surtout du
choix d'un bon mot de passe maître.

## J'ai perdu le téléphone avec le QR code. Et Windows Hello ?

[Windows Hello](windows-hello) est lié à votre compte Windows sur cet
ordinateur ; le QR est indépendant. Si vous pouvez encore ouvrir le coffre
(par mot de passe ou par Hello), générez un **nouveau QR code** dans
*Paramètres → Sécurité* et conservez-le.

## Puis-je l'utiliser sans rien installer d'autre que l'application ?

Oui. Le coffre local n'a besoin ni de base de données, ni de serveur, ni
de compte. Les fonctions réseau sont toutes facultatives — voyez
[Confidentialité et réseau](privacidade-rede).

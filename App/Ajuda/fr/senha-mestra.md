# Mot de passe maître

Le mot de passe maître est la seule clé du coffre. À partir de lui,
l'application dérive — en mémoire — la clé qui déchiffre vos données. Il
n'est **stocké nulle part**.

## Sans clé de récupération, il ne peut pas être récupéré

Par défaut, il n'existe pas de « mot de passe oublié ». Si vous perdez le
mot de passe maître sans avoir activé la **clé de récupération** (voir
plus bas), le contenu du coffre devient inaccessible — c'est exactement
ainsi que le chiffrement vous protège des tiers. C'est pourquoi :

- choisissez une **phrase longue** (plusieurs mots), facile à retenir et
  difficile à deviner ;
- enregistrez le **QR code de secours** (menu *Sécurité → Regénérer le
  code QR*) et gardez-le hors de l'ordinateur ;
- activez la **clé de récupération** comme second filet de sécurité ;
- éventuellement, gardez une copie écrite dans un endroit physique sûr.

## Changer le mot de passe maître

Dans **Paramètres → Sécurité → Modifier le mot de passe maître**. Tout le
coffre est rechiffré avec la nouvelle clé. Si vous utilisez une base de
données ou la [Synchronisation par dossier](sincronizacao), le changement
affecte les autres appareils — lisez l'écran de confirmation avant de
continuer.

> Une fois que le changement signale sa réussite, l'application redémarre
> d'elle-même. Ne touchez pas au coffre pendant cet intervalle.

## QR code de secours

C'est votre mot de passe maître encodé en image, pour le réimporter si
vous oubliez celui que vous avez tapé. Traitez le QR avec le même soin que
le mot de passe : quiconque l'a ouvre votre coffre.

## Clé de récupération

Dans *Paramètres → Sécurité → Clé de récupération*, ou dès la création du
coffre. Génère un secret de 160 bits (en groupes de 4 caractères) qui
permet de réinitialiser le mot de passe maître sans connaître l'ancien —
quiconque possède ce secret le peut. Notez-le donc et gardez-le **loin de
l'ordinateur**, avec le même soin que le mot de passe maître lui-même : il
vaut tout autant que lui.

Vous avez oublié le mot de passe maître et aviez activé la clé ? Sur
l'écran de connexion, cliquez sur « Mot de passe maître oublié », saisissez
le secret noté et choisissez un nouveau mot de passe — le coffre est
rechiffré avec celui-ci. Un nouveau secret est généré à chaque
récupération et à chaque changement de mot de passe maître ; notez
toujours le plus récent, le précédent cesse de fonctionner. L'activer, le
régénérer ou le désactiver exige toujours le mot de passe maître actuel,
même coffre déjà déverrouillé.

## Windows Hello

[Windows Hello](windows-hello) permet de déverrouiller par biométrie au
lieu de taper le mot de passe maître — mais le mot de passe maître reste
la vraie clé et demeure nécessaire pour les opérations sensibles.

# Mot de passe maître

Le mot de passe maître est la seule clé du coffre. À partir de lui,
l'application dérive — en mémoire — la clé qui déchiffre vos données. Il
n'est **stocké nulle part**.

## Il ne peut pas être récupéré

Il n'existe pas de « mot de passe oublié ». Si vous perdez le mot de passe
maître, le contenu du coffre devient inaccessible — c'est exactement ainsi
que le chiffrement vous protège des tiers. C'est pourquoi :

- choisissez une **phrase longue** (plusieurs mots), facile à retenir et
  difficile à deviner ;
- enregistrez le **QR code de secours** (menu *Sécurité → Regénérer le
  code QR*) et gardez-le hors de l'ordinateur ;
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

## Windows Hello

[Windows Hello](windows-hello) permet de déverrouiller par biométrie au
lieu de taper le mot de passe maître — mais le mot de passe maître reste
la vraie clé et demeure nécessaire pour les opérations sensibles.

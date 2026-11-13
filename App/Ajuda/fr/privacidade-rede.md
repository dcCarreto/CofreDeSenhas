# Confidentialité et réseau

Par défaut, l'application **n'accède pas à Internet**. Les fonctions
ci-dessous sont facultatives et vous activez chacune en sachant ce qu'elle
fait.

## Icônes en ligne des services

Quand elle est activée, elle récupère l'icône réelle de chaque site auprès
d'un service public d'icônes, en envoyant **seulement le domaine** (par
exemple `github.com`). Aucun mot de passe, identifiant ni note ne quitte
l'ordinateur. Les icônes téléchargées sont mises en cache sur le disque.
Quand elle est désactivée, le coffre n'affiche que des initiales et ne
touche pas au réseau.

## Mettre à jour automatiquement

Activée par défaut. Interroge la page des versions du projet à l'ouverture
du coffre et toutes les 6 heures ; s'il y a une nouvelle version, elle la
télécharge en arrière-plan, vérifie sa signature et son SHA256, puis
l'installe au verrouillage du coffre. L'application se ferme et se rouvre
déjà à jour, sur l'écran de déverrouillage. Pour l'installer tout de suite,
utilisez **Installer maintenant** en bas de la fenêtre. Aucune donnée du
coffre n'est envoyée. Installée depuis le Microsoft Store, c'est le Store
qui gère les mises à jour et l'option n'apparaît pas.

## Vérification des fuites

À la demande, compare vos mots de passe à des bases publiques de fuites en
utilisant la **k-anonymity** : seul un préfixe de l'empreinte du mot de
passe est envoyé. Voyez [Rapport de sécurité](seguranca).

## Synchronisation et base de données

Si vous configurez la [Synchronisation par dossier](sincronizacao) ou une
[Base de données](banco-de-dados), le trafic va vers **votre** dossier ou
**votre** serveur — jamais vers un service de notre part.

## Mode confidentialité

Le bouton en forme d'œil dans la barre de titre floute les valeurs
sensibles à l'écran, pour que vous ouvriez le coffre près d'autres
personnes sans rien exposer.

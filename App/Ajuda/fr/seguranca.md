# Rapport de sécurité et audit

L'application évalue la santé du coffre sans que rien ne quitte
l'ordinateur (sauf la vérification des fuites, décrite ci-dessous).

## Rapport de sécurité

Donne un **score global** et liste les problèmes par type :

- **Mots de passe faibles** : courts ou prévisibles. Remplacez-les avec le
  [Générateur de mots de passe](gerador).
- **Mots de passe réutilisés** : le même mot de passe sur des services
  différents. Le plus grand risque en pratique — corrigez ceux-là en
  premier.
- **Mots de passe anciens** : pas changés depuis longtemps, lorsque
  l'historique d'utilisation est activé.

Cliquer sur un élément filtre la liste principale sur les entrées
concernées.

## Audit du coffre

Un balayage plus détaillé, identifiant par identifiant, avec la raison de
chaque signalement. Utile pour une revue complète de temps en temps.

## Vérification des fuites

Compare vos mots de passe à des bases publiques de fuites connues en
utilisant la **k-anonymity** : seule une partie de l'empreinte (hash) du
mot de passe est envoyée, jamais le mot de passe ni le service. Si une
correspondance apparaît, changez le mot de passe dès que possible.

## Historique du score

Si vous gardez l'historique d'utilisation activé, l'application enregistre
l'évolution du score dans le temps, pour que vous voyiez si le coffre
s'améliore.

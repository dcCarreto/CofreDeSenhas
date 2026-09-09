# Windows Hello

Permet de **déverrouiller le coffre par biométrie** (empreinte, visage ou
code PIN Windows) au lieu de taper le mot de passe maître à chaque fois.

## Comment l'activer

Dans **Paramètres → Sécurité → Activer Windows Hello**. Windows demande
votre vérification et l'application se met à proposer le déverrouillage par
Hello sur l'écran d'ouverture.

## Ce qui change et ce qui ne change pas

- Le **mot de passe maître reste la vraie clé** du coffre. Hello ne fait
  que libérer l'accès conservé sous la protection du système.
- Les opérations sensibles (comme changer le mot de passe maître)
  demandent toujours le mot de passe maître.
- Si Hello échoue ou si l'appareil ne le prend pas en charge, vous pouvez
  toujours vous connecter avec le mot de passe maître.

## Portée

L'identifiant Hello est lié à **votre compte Windows sur cet
ordinateur**. Il ne voyage pas avec le fichier du coffre : sur un autre
ordinateur, vous réactivez Hello (ou utilisez le mot de passe maître).

## Désactiver

Sur le même écran, *Désactiver Windows Hello*. L'accès protégé est retiré
et le coffre se rouvre uniquement par le mot de passe maître.

# Security report and audit

The app evaluates the vault's health without anything leaving the computer
(except the breach check, described below).

## Security report

Gives an **overall score** and lists the problems by type:

- **Weak passwords**: short or predictable. Replace them with the
  [Password generator](gerador).
- **Reused passwords**: the same password across different services. The
  biggest practical risk — fix these first.
- **Old passwords**: not changed in a long time, when usage history is on.

Clicking an item filters the main list to the affected entries.

## Vault audit

A more detailed sweep, credential by credential, with the reason for each
finding. Useful for a full review from time to time.

## Breach check

Compares your passwords against public databases of known breaches using
**k-anonymity**: only a slice of the password's hash is sent, never the
password nor the service. If a match shows up, change the password as soon
as possible.

## Score history

If you keep usage history on, the app records how the score evolves over
time, so you can see whether the vault is improving.

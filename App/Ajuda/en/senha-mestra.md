# Master password

The master password is the vault's only key. From it, the app derives — in
memory — the key that decrypts your data. It is **stored nowhere**.

## It cannot be recovered

There is no "forgot my password". If you lose the master password, the
vault's contents become inaccessible — that is exactly how encryption
protects you from others. So:

- choose a **long phrase** (several words), easy to remember and hard to
  guess;
- save the **backup QR code** (menu *Security → Regenerate QR code*) and
  keep it away from the computer;
- optionally, keep a written copy in a safe physical place.

## Change the master password

In **Settings → Security → Change master password**. The whole vault is
re-encrypted with the new key. If you use a database or
[Folder sync](sincronizacao), the change affects the other devices —
read the confirmation screen before continuing.

> After the change reports success, the app restarts on its own. Don't
> touch the vault during that interval.

## Backup QR code

It is your master password encoded as an image, to re-import in case you
forget the typed one. Treat the QR with the same care as the password:
whoever has it can open your vault.

## Windows Hello

[Windows Hello](windows-hello) lets you unlock with biometrics instead of
typing the master password — but the master password is still the real key
and is still required for sensitive operations.

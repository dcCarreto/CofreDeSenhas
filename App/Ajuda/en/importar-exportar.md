# Import and export

## Import from another manager

In **Settings → Backup & sync → Import from CSV**. Most managers export a
**CSV** file; the app recognizes the most common formats and lets you
check the column mapping before finishing.

> The CSV exported by other programs is **plaintext**, unencrypted. After
> importing, delete that file securely.

## Export your vault

The export produces an **encrypted** file, protected by a password you set
at the time. Use it to migrate to another computer or keep a long-term
copy outside the [Backup](backup) format.

To re-import, open the app on the target machine and use the import option
on the exported file, entering the password used for the export.

## Move to another password manager

In **Settings → Backup & sync → Export unencrypted (CSV / Bitwarden)**.
After confirming the master password, pick the format:

- **CSV**: accepted by Bitwarden and most other managers.
- **Bitwarden JSON**: also carries the password history and the extra
  fields of each credential type.

Trash items and attachments are left out.

> This file is **plain text**: any person or program with access to it can
> read all your passwords. Delete it as soon as you finish importing it
> into the other manager.

## Move to another computer

1. Export the vault (or copy a [Backup](backup)).
2. Install the app on the other computer.
3. Import the file and enter the password.
4. If you want to keep both in sync, set up [Folder sync](sincronizacao).

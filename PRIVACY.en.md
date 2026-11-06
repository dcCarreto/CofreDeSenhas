# Privacy policy

[Versão em português](PRIVACY.md) — the Portuguese version is the reference text.

**Effective:** November 6, 2026
**Applies to:** Cofre de Senhas — the desktop app for Windows and Linux, the
`cofre` command-line tool, and the installers distributed through
[GitHub releases](https://github.com/dcCarreto/CofreDeSenhas/releases) and the
Windows Package Manager (winget, package `dcCarreto.CofreDeSenhas`).
**Responsible party:** Denis Cristino Cantagallo Carreto, author and maintainer.

## Summary

- Cofre de Senhas is a **local** application. Everything you store in it stays on
  your computer, encrypted with a key derived from your master password.
- The author **runs no server** for the app and **does not receive, collect or
  store** any of your data. There is no account, sign-up, telemetry, usage
  analytics, automatic crash reporting or advertising.
- By default, the app **does not access the internet**. The few features that use
  the network are optional, turned on by you, and described below with exactly
  what they send and to whom.

## 1. Data the app stores on your computer

The app stores, on your own computer, the data you type or import into it:

- credentials: service name, username, password, website address, notes, tags
  and per-type extra fields (for example card details, when you enter them);
- two-factor authentication (TOTP) secrets and recovery codes;
- each item's previous-password history;
- attachments (files you associate with an item);
- dates of creation, modification and the last copy of a password, username or
  TOTP code (last-copy tracking can be turned off in settings).

This data lives in the app's data folder:

- Windows: `%APPDATA%\GerenciadorSenhas\`
- Linux: `~/.config/GerenciadorSenhas/`

The vault (`senhas.json.enc`), its backups and every attachment are encrypted
with AES-256-GCM using a key derived from the master password (Argon2id). The
master password is never written to disk: only a salt and a verifier are stored
(`auth.dat`). Without the master password — or the recovery key, if you enabled
it — the content cannot be read, not even by the author. Technical details are in
[docs/SEGURANCA.md](docs/SEGURANCA.md) and the [threat model](THREAT_MODEL.md).

Besides the vault, the same folder may contain:

| File or folder | Content | Encrypted? |
|---|---|---|
| `backups/` | Automatic vault copies. By default up to 10; older ones are deleted automatically. Frequency and count are configurable. | Yes |
| `anexos/` | One file per attachment, with a random name. | Yes |
| `recuperacao.dat` | Only exists if you enable the recovery key. | Yes |
| `biometria.dat` | Only exists if you enable Windows Hello. Biometrics are handled by Windows; the app never accesses fingerprints or face images. | Yes (Windows Hello + DPAPI) |
| `config.json` | Interface preferences and, if you connect an external database, the connection profile (type, server, port, database name and user). The server password, if any, is stored encrypted with the vault key. | Partly |
| `pontuacao-historico.json` | Dates and values of the vault's security score, for the trend chart. Contains no service names or passwords. | No |
| `logs/erros.log` | Diagnostic log (see section 2). | No |
| `icones/` | Website icon cache, only with online icons turned on. Each file name is a hash of the domain. | No |
| `vozes/` | Text-to-speech voice models, only if you download them. | No |

## 2. Diagnostic logs

The app writes unexpected errors to `logs/erros.log` in the data folder, to help
you (or the maintainer, if you choose to share it) understand a problem. Each
line holds the date and time (UTC), the error type and its message.

- The app **never sends** the log anywhere. It only leaves your computer if you
  attach it to a report yourself.
- Passwords, notes, TOTP secrets and decrypted vault content are not written to
  the log. The path of your Windows profile folder, which contains your OS user
  name, is replaced with `%USERPROFILE%`.
- **Exception:** when synchronization with an external database detects a
  conflict (for example, a tampered row), the log records the conflict type, the
  affected item's **service name** and its internal identifier, so you can review
  it later. Review the file before sharing it.
- The file is deleted and restarted once it exceeds about 1 MB.

## 3. Features that use the network

None of the features below works without an action or consent from you. In all
of them, the destination service receives, as with any internet connection, your
IP address and an app identifier (User-Agent). What each service does with that
data is governed by its own privacy policy, not this one.

### 3.1 Breached-password check (Have I Been Pwned)

- **When:** only when you generate or refresh the vault security report.
- **What is sent:** for each password, only the **first 5 characters of its
  SHA-1 hash** (*k-anonymity*) to `api.pwnedpasswords.com`. The password, the
  full hash, the username and the service name never leave the computer. The
  returned hash list is compared locally.
- **Recipient:** Have I Been Pwned's Pwned Passwords service —
  <https://haveibeenpwned.com/Privacy>.

### 3.2 Online service icons

- **When:** only if you turn on "Online icons" in settings. Off by default; the
  app asks for consent before turning it on.
- **What is sent:** only each item's **domain** (for example `github.com`) to
  Google's favicon service (`www.google.com/s2/favicons`). No password, username
  or note is sent.
- **Retention:** downloaded icons are cached in `icones/`. Turning the feature
  off deletes the cache.
- **Recipient:** Google — <https://policies.google.com/privacy>.

### 3.3 Update check and installation

- **When:** only if you turn on "Check for updates" in settings (off by
  default). When on, the app checks for the latest release at startup.
- **What is sent:** a public GitHub API request
  (`api.github.com/repos/dcCarreto/CofreDeSenhas/releases/latest`), with no vault
  data and no identifier of you or your device.
- If you choose "Update now", the installer and verification files
  (`CHECKSUMS.txt` and its signature) are downloaded from GitHub releases.
  Nothing is downloaded or installed without your confirmation.
- **Recipient:** GitHub — <https://docs.github.com/site-policy/privacy-policies/github-general-privacy-statement>.

### 3.4 Text-to-speech voice downloads

- **When:** only when you ask, in the accessibility settings, to download the
  neural voice for a language.
- **What is sent:** only the request for the voice model file, from the sherpa-onnx
  project's public GitHub releases. Speech itself is produced locally; no vault
  text is sent.

### 3.5 Shared-folder synchronization

- **When:** only if you set up a sync folder (for example a OneDrive, Dropbox or
  network folder).
- **What is written:** the app writes to the folder **you** chose a
  `sincronizacao.dat` file, encrypted with AES-256-GCM under a key derived from
  the master password, and, if you turn on "Sync attachments", one encrypted file
  per attachment in `anexos-sincronizados/`. The author has no access to that
  folder.
- **What a third party with folder access sees:** only ciphertext. With synced
  attachments, also the number of attachments, the approximate size of each and
  when they were created or removed.
- Transport and storage are handled by the provider you chose, under its own
  privacy policy.

### 3.6 External database

- **When:** only if you connect the vault to a database (SQLite, PostgreSQL,
  MySQL/MariaDB or SQL Server) that **you** operate or trust.
- **Field visibility:** password, TOTP secret, password history and recovery codes
  are encrypted before leaving the app. **Service name, username, notes and tags
  are stored in plain text** in the database columns, readable by anyone with read
  access to the tables (for example, the database administrator). The database
  also receives the master password's salt and verifier (`CofreDeSenhasAuth`
  table), which lets someone with database access attempt an offline brute-force
  guess of the master password. Attachments never go to the database.
- The connection is always encrypted in transit (TLS). Details are in the
  [threat model](THREAT_MODEL.md).

## 4. What the app does not do

- It sends no telemetry, usage statistics, crash reports or device identifiers.
- It shows no ads and does not sell, rent or share data with anyone — the author
  has no access to any data to share.
- It requires no account or sign-up.
- It does not read data from other apps. Imports (CSV, JSON, KeePass `.kdbx` and
  others) only read the file you pick.

## 5. Clipboard, auto-type and export

- Copied passwords and codes are flagged to stay out of Windows Clipboard History
  and Cloud Clipboard, and the clipboard is cleared automatically after
  30 seconds (configurable).
- Auto-type (Windows) sends the username and password only to the window you
  confirmed.
- `.gsenhas` exports are encrypted with a password you set. CSV or JSON
  (Bitwarden format) exports **are plain text**, produced only after you confirm
  the master password and a warning; the file stays wherever you save it and is
  your responsibility from then on.

## 6. Retention and deletion

Because the data stays on your computer, you control how long it exists:

- **Deleted items** go to the trash (inside the encrypted vault) and stay there
  until you restore them, delete them permanently or empty the trash; there is no
  automatic expiry. After permanent deletion, the item still exists in the
  encrypted backups until those backups are replaced by automatic rotation.
- **"Delete vault"** (in settings, after confirming the master password) deletes
  the vault, backups, attachments, the authentication file, the Windows Hello
  binding, the score history and the saved database and sync profiles.
- **To delete everything**, including logs, icon cache, voices and preferences,
  delete the whole data folder (section 1). On Windows, the uninstaller offers this
  option at the end of uninstallation.
- Data written **outside** the computer by your choice — the sync folder, an
  external database or exported files — is not deleted by the app; remove it
  directly where it lives.

## 7. Your controls

| Feature | Default | Where to change |
|---|---|---|
| Online icons | Off | Settings |
| Check for updates | Off | Settings |
| Breached-password check | On demand only | Security report |
| Last-copy tracking | On (kept inside the encrypted vault) | Settings |
| Folder synchronization | Off | Settings |
| External database | Not connected | Settings |
| Windows Hello | Off | Settings |
| Recovery key | Off | Settings |
| Automatic backups | Weekly, up to 10 copies | Settings |

## 8. Children

The app is not directed at children and collects no data from anyone, of any age.

## 9. Data protection laws (LGPD, GDPR)

The author does not collect or process users' personal data: vault data stays
under your control, on your device or on infrastructure you choose. The
third-party services listed in section 3 process the data they receive under
their own policies.

## 10. Changes to this policy

This policy is kept in the project repository, and every previous version is in
the [file history](https://github.com/dcCarreto/CofreDeSenhas/commits/prod/PRIVACY.en.md).
Material changes — for example, a new feature that uses the network — are recorded
here before or together with the release that introduces them, with the effective
date updated.

## 11. Contact

Questions about this policy:
[open an issue](https://github.com/dcCarreto/CofreDeSenhas/issues). To report a
vulnerability, use the private channel described in [SECURITY.md](SECURITY.md).

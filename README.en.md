# Cofre de Senhas (Password Vault)

[🇧🇷 Português](README.md) | 🇺🇸 English

Cross-platform password manager (Windows and Linux), with a built-in password
generator and an encrypted local vault. The app brings together, in a single
interface, strong password generation and secure credential storage,
protected by a master password and AES-256-GCM encryption. Built in C# with
.NET 10 and Avalonia, sharing a single codebase across platforms.

[![CI](https://github.com/dcCarreto/CofreDeSenhas/actions/workflows/ci.yml/badge.svg)](https://github.com/dcCarreto/CofreDeSenhas/actions/workflows/ci.yml)
![License](https://img.shields.io/badge/license-PolyForm%20Noncommercial-blue)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20%7C%20Linux-0078D6)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)

This is an open-source, free, non-commercial project, distributed under the
PolyForm Noncommercial 1.0.0 license. You're free to use, study, modify and
share it for any non-commercial purpose.

> The project's code, comments and most documentation are in Portuguese —
> the maintainer's native language. This README is translated and kept in
> sync by hand; the docs it links to are noted where they're Portuguese-only.

## Contents

- [About the project and open source](#about-the-project-and-open-source)
- [Overview](#overview)
- [Screenshots](#screenshots)
- [Features](#features)
- [Security model](#security-model)
- [Security policy and vulnerability disclosure](SECURITY.md) (in Portuguese)
- [Code signing policy](#code-signing-policy)
- [Download and installation](#download-and-installation)
- [Project structure](#project-structure)
- [Architecture](#architecture)
- [Technologies](#technologies)
- [Requirements](#requirements)
- [Building and running](#building-and-running)
- [Tests](#tests)
- [Data storage](#data-storage)
- [Contributing](#contributing)
- [License](#license)

## About the project and open source

I've always believed good tools should be within everyone's reach. Cofre de
Senhas was born from that same conviction: an open, free project to use,
examine, modify and distribute for non-commercial purposes, under the
PolyForm Noncommercial 1.0.0 license — no paid tiers, no features locked
behind a subscription, no data collection. The full story is in
[docs/SOBRE.md](docs/SOBRE.md) (in Portuguese).

## Overview

By default, the app works entirely locally: no data is sent to external
servers, except for the optional password breach check, which uses an
anonymity protocol described in the security section. Credentials live in an
encrypted file inside the user's profile, and the encryption key is derived
from the master password at runtime, never written to disk. Optionally, the
vault can connect to a database of your choice (SQLite, PostgreSQL,
MySQL/MariaDB or SQL Server); in that case, the local vault and the database
are synchronized and kept mirrored, always storing the password encrypted.

The main window has a sidebar (vault, favorites, recent items and
categories) and, at the center, the credential list with search, category
filter, audit and per-item actions. The password generator opens in an
on-demand panel and also appears side by side with the unlock form on the
master password screen, letting you create and copy passwords before even
opening the vault.

## Screenshots

The main screen brings together the sidebar, the credential list and the
vault status in the footer, in the default "vault, brushed brass" visual
identity: warm charcoal dark theme, brushed brass as an accent and serif
typography in titles — the Appearance tab in Settings offers a light theme,
other accent colors and a compact density. The images below use fictional
data for demonstration only.

![Cofre de Senhas: sidebar, credential list and footer](docs/captura-cofre.png)

On the master password screen, the generator sits on the left and the unlock
form on the right, with a language selector and, on Windows, the option to
unlock with Windows Hello:

![Master password screen with generator and unlock form](docs/tela-login.png)

## Features

Full list, with every detail and additional screenshots, in
[docs/FUNCIONALIDADES.md](docs/FUNCIONALIDADES.md) (in Portuguese). Summary
by area:

- **Password generator** — configurable passwords and passphrases, real-time
  strength indicator, batch generation, also available before unlocking.
- **Password vault** — credential types (card, Wi-Fi, server, database...),
  TOTP, recovery codes, categories and tags, search and filters, trash,
  security report, password history, encrypted attachments, auto-type
  (Windows) and keyboard shortcuts.
- **Security and authentication** — attempt limit, auto-lock, Windows Hello,
  master password change with re-encryption, a danger zone to clear or
  delete the vault.
- **Backup and recovery** — automatic local backup, export/import
  (`.gsenhas` and CSV from other managers), master password backup QR code,
  optional recovery key.
- **Sync across devices** — shared folder (Dropbox, OneDrive, etc.),
  end-to-end encrypted, no database required.
- **External database** — SQLite, PostgreSQL, MySQL/MariaDB or SQL Server,
  for self-hosted or shared use, with a per-row integrity HMAC.
- **Interface** — borderless window, themes and accent colors, built-in
  manual in six languages, privacy mode, expert-level accessibility
  (color-blind modes, contrast, screen reader with an embedded neural
  voice), same experience on Windows and Linux.
- **Automatic update** — checks the latest release and updates itself
  (installer/portable/AppImage), verifying the file hash before applying it.

## Security model

Full table of algorithms and parameters in
[docs/SEGURANCA.md](docs/SEGURANCA.md) (in Portuguese). Summary:

- Vault encrypted with AES-256-GCM; key derived from the master password
  with Argon2id (64 MiB, 3 iterations) — the master password itself is
  never stored.
- Sensitive comparisons run in constant time; optional password breach check
  (Have I Been Pwned) via k-anonymity, off by default.
- Folder sync and the external database are always encrypted (AES-256-GCM /
  TLS), with a per-credential integrity HMAC on the external database.
- Optional Windows Hello unlock, backed by the TPM; memory is wiped when the
  vault locks or closes.
- Data stays only in the user's profile folder
  (`%APPDATA%\GerenciadorSenhas\` on Windows,
  `~/.config/GerenciadorSenhas/` on Linux), outside the repository.

To report a vulnerability found in the vault, see the [security
policy](SECURITY.md) (in Portuguese). For what the vault's security design
covers, and what's deliberately out of scope, see the [threat
model](THREAT_MODEL.md) (in Portuguese).

## Code signing policy

The project applied for free code signing from the [SignPath
Foundation](https://signpath.org) for the Windows binaries, currently
unsigned — no Authenticode certificate yet (see [Verifying file
integrity](docs/INSTALACAO.md#verificando-a-integridade-dos-arquivos), in
Portuguese). Once the application is approved, Windows releases will
display:

> Free code signing provided by [SignPath.io](https://about.signpath.io),
> certificate by [SignPath Foundation](https://signpath.org)

**Project roles** — maintained by a single person, who holds every role
below:

- Committers and code reviewers: [dcCarreto](https://github.com/dcCarreto)
- Approver for each release to be signed: [dcCarreto](https://github.com/dcCarreto)

**Privacy policy** — this program will not transfer any information to
other networked systems unless specifically requested by the user or the
person installing or operating it. The only two exceptions, both opt-in and
off by default, are described in the [security model](#security-model): the
password breach check (Have I Been Pwned, via k-anonymity) and the
domain-based service icon lookup.

## Download and installation

The simplest way is to download the ready-made executable from the
[releases page](../../releases) — self-contained, no .NET installation
required. See the [code signing policy](#code-signing-policy) above for the
current state of Authenticode signing on Windows.

- **Windows**: download and run `CofreDeSenhas-Setup-*.exe` (installer, no
  admin rights required) or `CofreDeSenhas.exe` (portable, no installer)
  from the [latest release](../../releases/latest).
- **Linux**: download `CofreDeSenhas-*-x86_64.AppImage`, `chmod +x` it and
  run it — no .NET installation required.
- Just want to generate passwords without opening the vault?
  `GeradorDeSenhas-*-win-x64-portatil.exe`, also on the releases page.
- Prefer the terminal? Every release ships `cofre`, a read-only
  command-line tool for automation — see `cofre --help`.

On first use, the app asks you to create a master password: keep it safe,
it's the only way to open the vault. Full walkthrough — Windows installer,
Linux install/uninstall scripts, using `cofre`, verifying file integrity via
hash and attestation — in [docs/INSTALACAO.md](docs/INSTALACAO.md) (in
Portuguese).

## Project structure

```text
CofreDeSenhas.sln
├─ App/                          UI (Avalonia), cross-platform
│  ├─ Janelas/                   App windows and dialogs
│  ├─ Controles/                 Custom UI controls
│  ├─ Infraestrutura/            Theme, preferences, resources and utilities
│  ├─ Ativos/                    App icon
│  └─ distribuicao/              Packaging and install scripts (Linux and Windows)
├─ Gerador/                      Standalone password generator (Avalonia)
├─ Cli/                          `cofre` command-line executable
│                                (read-only; depends only on the domain layer)
├─ GerenciadorDeSenhas/          Domain library
│  ├─ Modelos/                   Entities (Senha, Categoria, SenhaExportada,
│  │                             TipoBanco, ConexaoBanco)
│  ├─ Repositorios/              Credential access (local file, database, and
│  │                             the mirror that keeps both in sync)
│  └─ Servicos/                  Encryption, persistence, authentication,
│                                export, breach checking, database
│                                connections and business rules
├─ GerenciadorDeSenhas.Testes/   Domain tests (xUnit)
├─ App.Testes/                   UI tests (xUnit + Avalonia headless)
└─ Cli.Testes/                   CLI tests (xUnit)
```

The solution keeps the UI (`App` project) separate from the domain logic
(`GerenciadorDeSenhas` project). That keeps business rules and cryptography
independent from the UI layer and lets them be tested in isolation — the CLI
(`Cli`) consumes the same domain layer with no UI dependency.

## Architecture

The app is organized in layers, with no hard dependency on a database or
external services:

- Presentation (`App` project): Avalonia windows and controls, with XAML
  layout and custom controls written in code. Contains no business rules or
  cryptographic operations.
- Domain and services (`GerenciadorDeSenhas` project): master password
  authentication, encryption, persistence, export, auditing and breach
  checking.
- Persistence: by default, data is serialized as JSON and written encrypted
  to a file in the user's profile. Optionally, the vault can be synced and
  mirrored with an external database (SQLite, PostgreSQL, MySQL/MariaDB or
  SQL Server) via ADO.NET, always storing the password encrypted.

The full master password derivation and re-encryption flow (Argon2id,
migrating older PBKDF2 vaults, backup and rollback) is in
[docs/SEGURANCA.md](docs/SEGURANCA.md#fluxo-da-senha-mestra) (in
Portuguese).

## Technologies

- C# and .NET 10.
- Avalonia and XAML for the UI, cross-platform (Windows and Linux).
- Standard library cryptography (`System.Security.Cryptography`):
  AES-256-GCM and PBKDF2-SHA256 (export and migrating older vaults), plus
  Argon2id (`Konscious.Security.Cryptography.Argon2`) for the vault key.
- Serialization with `System.Text.Json`.
- Database access via ADO.NET: Microsoft.Data.Sqlite, Npgsql,
  MySqlConnector and Microsoft.Data.SqlClient.
- QR code generation with the QRCoder library.
- Tests with xUnit.

## Requirements

- Windows 10 or 11, or a Linux distribution with X11 or Wayland.
- To build from source: .NET 10 SDK.
- To run the published binary: nothing beyond the executable itself, which
  is self-contained.

## Building and running

From the repository root, on any platform:

```sh
dotnet run --project App/App.csproj
```

To generate the publishable executable or the Windows installer, see
[docs/BUILD.md](docs/BUILD.md) (in Portuguese).

## Tests

```sh
dotnet test
```

The suite covers unit tests, end-to-end integration tests (creating,
persisting and reloading the vault), security tests (rejecting tampered
data and wrong keys), and performance tests with a large volume of
passwords.

## Data storage

App files live in `%APPDATA%\GerenciadorSenhas\` on Windows and in
`~/.config/GerenciadorSenhas/` on Linux:

- `auth.dat`: master password salt and verifier.
- `senhas.json.enc`: the encrypted vault with all credentials.
- `biometria.dat`: vault key, encrypted and bound to a Windows Hello
  credential, for biometric unlock when enabled on this device.
- `config.json`: UI preferences (such as language and accessibility) and the
  last database connection profile, without the server password.
- `backups/`: vault backup copies.

These files aren't part of the repository and contain sensitive data.

When running from source (`dotnet run`, or a Debug build executable), data
goes to `GerenciadorSenhas.dev/` next to the folder above — never to the
installed app's folder. The test suite uses a disposable temporary folder,
deleted at the end. Only the binary published for a release uses
`GerenciadorSenhas/`.

## Contributing

Contributions are welcome. Feel free to open an issue reporting a problem or
suggesting an improvement, or to send a pull request. A few simple
guidelines:

- Keep the existing code style.
- Whenever possible, back new features with tests.
- Clearly describe what your change does and why.

## License

Distributed under the [PolyForm Noncommercial
1.0.0](https://polyformproject.org/licenses/noncommercial/1.0.0) license.
See the [LICENSE](LICENSE) file for the full terms. In short: you can use,
copy, study, modify and redistribute this software freely for any
**non-commercial** purpose, keeping the copyright notice and these terms.
Commercial use — selling the program, charging for it, or bundling it into
a paid product or service — isn't allowed. The app is free and will stay
that way.

# Cofre de Senhas (Password Vault)

[🇧🇷 Português](README.md) | 🇺🇸 English

Cross-platform password manager (Windows and Linux): built-in password
generator, encrypted local vault (AES-256-GCM), and optional sync across
devices or with an external database. Open source and free —
built in C# with .NET 10 and Avalonia.

[![CI](https://github.com/dcCarreto/CofreDeSenhas/actions/workflows/ci.yml/badge.svg)](https://github.com/dcCarreto/CofreDeSenhas/actions/workflows/ci.yml)
![License](https://img.shields.io/badge/license-MIT-blue)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20%7C%20Linux-0078D6)
![.NET](https://img.shields.io/badge/.NET-10-512BD4)

Distributed under the [MIT](LICENSE) license — free to use, study, modify and
redistribute for any purpose, as long as the copyright notice and the license
are kept with the copies.

> The project's code, comments and linked docs below are in Portuguese — the
> maintainer's native language. This README is translated and kept in sync
> by hand; pages that are Portuguese-only are marked as such.

![Cofre de Senhas: sidebar, credential list and footer](docs/captura-cofre.png)

## Download and install

- **Windows**: download and run `CofreDeSenhas-Setup-*.exe` (installer, no
  admin rights required) or `CofreDeSenhas.exe` (portable) from the
  [latest release](../../releases/latest).
- **Linux**: download `CofreDeSenhas-*-x86_64.AppImage` from the [latest
  release](../../releases/latest), `chmod +x` it and run it.

Full walkthrough, the `cofre` command-line tool and file integrity
verification in [docs/INSTALACAO.md](docs/INSTALACAO.md) (in Portuguese).

## Learn more

- [About the project and open source](docs/SOBRE.md) — motivation and
  philosophy (in Portuguese).
- [Features](docs/FUNCIONALIDADES.md) — full list, by area, with screenshots
  (in Portuguese).
- [Security model](docs/SEGURANCA.md) — cryptographic algorithms and
  parameters used (in Portuguese).
- [Threat model](THREAT_MODEL.md) — what's protected, against what kind of
  attacker, and what's deliberately out of scope (in Portuguese).
- [Security policy](SECURITY.md) — how to report a vulnerability (in
  Portuguese).
- [Download and installation](docs/INSTALACAO.md) — Windows, Linux, the CLI
  and integrity verification (in Portuguese).
- [Architecture and technologies](docs/ARQUITETURA.md) — project structure,
  layers and stack (in Portuguese).
- [Building and testing](docs/DESENVOLVIMENTO.md) — running, testing and
  packaging the executable/installer from source (in Portuguese).
- [Data storage](docs/ARMAZENAMENTO.md) — where and how the vault's files are
  written (in Portuguese).
- [Contributing](CONTRIBUTING.md) (in Portuguese).
- [License](LICENSE) — MIT.

Windows binaries aren't code-signed (Authenticode) yet — see
[SECURITY.md](SECURITY.md#confiança-nos-binários-e-verificação-dos-downloads)
(in Portuguese) for why, and how to verify the authenticity of a download by
other means.

# Arquitetura e tecnologias

[← Voltar ao README](../README.md)

## Estrutura do projeto

```text
CofreDeSenhas.sln
├─ App/                          Interface (Avalonia), multiplataforma
│  ├─ Janelas/                   Telas e diálogos da aplicação
│  ├─ Controles/                 Controles customizados de UI
│  ├─ Infraestrutura/            Tema, preferências, recursos e utilitários
│  ├─ Ativos/                    Ícone do aplicativo
│  └─ distribuicao/              Scripts e atalhos de instalação (Linux e Windows)
├─ Gerador/                      Gerador de senhas standalone (Avalonia)
├─ Cli/                          Executável de linha de comando `cofre`
│                                (somente-leitura; referencia só o domínio)
├─ GerenciadorDeSenhas/          Biblioteca de domínio
│  ├─ Modelos/                   Entidades (Senha, Categoria, SenhaExportada,
│  │                             TipoBanco, ConexaoBanco)
│  ├─ Repositorios/              Acesso às credenciais (arquivo local, banco e
│  │                             espelho que sincroniza os dois)
│  └─ Servicos/                  Criptografia, persistência, autenticação,
│                                exportação, verificação de vazamento, conexão
│                                a banco de dados e regras
├─ GerenciadorDeSenhas.Testes/   Testes de domínio (xUnit)
├─ App.Testes/                   Testes da interface (xUnit + Avalonia headless)
└─ Cli.Testes/                   Testes do CLI (xUnit)
```

A solução separa a interface (projeto `App`) da lógica de domínio (projeto
`GerenciadorDeSenhas`). Isso mantém as regras de negócio e a criptografia
independentes da camada gráfica e permite testá-las de forma isolada — o CLI
(`Cli`) consome o mesmo domínio sem nenhuma dependência de UI.

## Arquitetura

A aplicação é organizada em camadas, sem dependências obrigatórias de banco de
dados ou de serviços externos:

- Apresentação (projeto `App`): janelas e controles em Avalonia, com o layout
  em XAML e os controles customizados desenhados por código. Não contém regra de
  negócio nem operação criptográfica.
- Domínio e serviços (projeto `GerenciadorDeSenhas`): autenticação da senha
  mestra, criptografia, persistência, exportação, auditoria e verificação de
  vazamento.
- Persistência: por padrão, os dados são serializados em JSON e gravados de
  forma criptografada em um arquivo no perfil do usuário. Opcionalmente, o cofre
  pode ser sincronizado e mantido espelhado com um banco de dados externo
  (SQLite, PostgreSQL, MySQL/MariaDB ou SQL Server), acessado por ADO.NET,
  guardando a senha sempre cifrada.

O fluxo completo de derivação e re-criptografia da senha mestra (Argon2id,
migração de cofres antigos em PBKDF2, backup e rollback) está em
[SEGURANCA.md](SEGURANCA.md#fluxo-da-senha-mestra).

## Tecnologias

- C# e .NET 10.
- Avalonia e XAML para a interface, multiplataforma (Windows e Linux).
- Criptografia da biblioteca padrão (`System.Security.Cryptography`):
  AES-256-GCM e PBKDF2-SHA256 (exportação e migração de cofres antigos), mais
  Argon2id (`Konscious.Security.Cryptography.Argon2`) para a chave do cofre.
- Serialização com `System.Text.Json`.
- Acesso a banco de dados por ADO.NET: Microsoft.Data.Sqlite, Npgsql,
  MySqlConnector e Microsoft.Data.SqlClient.
- Geração de QR code com a biblioteca QRCoder.
- Testes com xUnit.

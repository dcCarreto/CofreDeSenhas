# Sincronização por pasta

Mantém o mesmo cofre em vários dispositivos seus através de um **arquivo
compartilhado** numa pasta que você escolhe — normalmente uma pasta de um
serviço de arquivos que você já usa (a sincronização do arquivo em si fica
por conta desse serviço).

## Como funciona

- O aplicativo grava um arquivo cifrado na pasta indicada, protegido por
  uma chave derivada da sua senha mestra.
- Cada dispositivo lê e escreve nesse arquivo periodicamente.
- Quando dois dispositivos mudam a mesma credencial, o aplicativo resolve
  pelo registro de edição mais recente; conflitos que precisam de decisão
  aparecem numa tela própria.

## Configurar

Em **Configurações → Backup e sincronização → Sincronização**. Aponte a
pasta e defina a frequência. Repita em cada dispositivo, usando a **mesma
senha mestra** e a **mesma pasta**.

## O que não é

Isto **não é** um serviço de nuvem do aplicativo. Não há servidor nosso no
meio, nenhuma conta, e nada é enviado para nós. É a sua infraestrutura
sincronizando o seu arquivo.

Se o objetivo é compartilhar um cofre entre pessoas ou máquinas de forma
mais robusta, veja [Banco de dados compartilhado](banco-de-dados).

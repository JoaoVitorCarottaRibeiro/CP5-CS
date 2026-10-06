# Checkpoint 2 — ExpenseHub

Checkpoint de C# em grupos de até 3 pessoas para construção de uma Application Programming Interface (API) corporativa de reembolsos.

O prazo de entrega é **13 de outubro de 2026**. O grupo deverá implementar autenticação, autorização, fluxo de aprovação e reprovação, pagamento simulado, histórico e testes unitários.

## Criar seu repositório

1. Clique em **Use this template**.
2. Selecione **Create a new repository**.
3. Crie um repositório **público** em uma das contas do grupo.
4. Adicione os demais integrantes como colaboradores.
5. Clone o repositório.

Não use fork. As issues permanecem neste repositório original como especificação comum da turma.

Os commits serão utilizados para avaliar a participação. Todos os membros do grupo
devem possuir mais de um commit no repositório.

## Fluxo de trabalho

Para cada issue:

1. leia os critérios no repositório original;
2. crie uma branch com o identificador, por exemplo `i06-ownership`;
3. implemente e valide a feature;
4. abra uma pull request no seu próprio repositório;
5. use um título como `I06 — Ownership e matriz de acesso`;
6. adicione na descrição uma referência completa, como `Racass/checkpoint-csharpracass-expensehub#6`;
7. não use `Closes`, `Fixes` ou `Resolves`, pois a issue original deve permanecer aberta;
8. conclua a auto-revisão e faça o merge.

## Estrutura inicial

```text
sources/
├── ExpenseHub.slnx
├── ExpenseHub.Api/
└── ExpenseHub.UnitTests/
```

A solução começa sem Identity, banco, domínio ou testes funcionais. Toda implementação avaliada deve ser criada por você.

## Comandos

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

O endpoint inicial `GET /health` existe apenas para confirmar que a aplicação inicia.

## Documentação

- [Enunciado](docs/ENUNCIADO.md)
- [Requisitos e contratos](docs/REQUISITOS.md)
- [Rubrica](docs/RUBRICA.md)
- [Matriz de autorização](docs/MATRIZ-AUTORIZACAO.md)
- [Processo no GitHub](docs/PROCESSO-GITHUB.md)
- [Uso de Inteligência Artificial](docs/USO-DE-IA.md)
- [Regras do pipeline de qualidade](docs/code-quality-rules.md)

## Banco de dados

Você pode utilizar Microsoft SQL Server LocalDB, Oracle Database, SQLite ou outro provider relacional compatível com Entity Framework Core.

A escolha não gera pontos. Documente no README do seu repositório:

- provider e pacote utilizado;
- configuração necessária;
- criação ou atualização do banco;
- como iniciar a aplicação.

Não versione senhas, tokens ou connection strings sensíveis.

## Testes

Somente testes unitários escritos por você entram na nota. Testes de integração, end-to-end ou de interface são permitidos, mas opcionais e sem pontuação.

Os testes unitários devem executar sem banco, rede ou serviço externo.

## Entrega

Entregue:

- URL do repositório público;
- commit Secure Hash Algorithm (SHA) final;
- integração contínua executada;
- documentação atualizada.

O projeto deve compilar sem erros e ser entregue sem warnings para receber a pontuação integral de Qualidade de Código.

## Configuração do projeto (nosso grupo)

### Provider e pacotes

Banco relacional: **SQLite**, acessado via Entity Framework Core. A escolha não
pontua; foi adotada por não exigir servidor externo nem credenciais, o que mantém
o repositório livre de segredos e simplifica a execução por todos os integrantes.

Pacotes principais (projeto `ExpenseHub.Api`):

- `Microsoft.EntityFrameworkCore.Sqlite`
- `Microsoft.EntityFrameworkCore.Design`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `System.IdentityModel.Tokens.Jwt`

### Segredos (fora do repositório)

A chave de assinatura do JWT e a senha inicial do Admin **não são versionadas**.
Em desenvolvimento, use os User Secrets do projeto:

```shell
cd sources/ExpenseHub.Api
dotnet user-secrets set "Jwt:Key" "<uma-chave-longa-e-aleatoria-com-32-ou-mais-caracteres>"
dotnet user-secrets set "Seed:AdminPassword" "<senha-forte-do-admin>"
```

Em integração contínua, os mesmos valores podem ser fornecidos por variáveis de
ambiente (`Jwt__Key` e `Seed__AdminPassword`). A senha do Admin deve atender à
política do Identity: mínimo de seis caracteres, com maiúscula, minúscula, dígito
e símbolo. O e-mail do Admin pode ser ajustado em `appsettings.json`
(`Seed:AdminEmail`); o padrão é `admin@expensehub.local`.

### Banco de dados

O banco (`expensehub.db`) é criado automaticamente na primeira execução: a
aplicação aplica as migrations no startup e executa o seed idempotente (as cinco
roles e a única conta Admin). O arquivo `.db` é ignorado pelo Git.

Para criar ou atualizar o banco manualmente, se preferir:

```shell
dotnet ef database update --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

A ferramenta `dotnet-ef` pode ser instalada com `dotnet tool install --global dotnet-ef`.

### Como iniciar

```shell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet run --project ./sources/ExpenseHub.Api/ExpenseHub.Api.csproj
```

Endpoints disponíveis nesta etapa:

- `GET /health` — confirma que a aplicação inicia.
- `POST /register` — cadastra um usuário (nunca aceita role).
- `POST /login` — autentica com e-mail e senha e devolve um token bearer (JWT).
- `GET /me` — rota protegida; devolve o usuário autenticado e suas roles.
- `GET /api/admin/users` — lista usuários; exclusivo de Admin.
- `PUT /api/admin/users/{id}/roles` — atribui ou remove roles; exclusivo de Admin.

As rotas administrativas exigem um token de um usuário com a role `Admin`. Sem token
a resposta é `401`; autenticado sem a role necessária é `403`. Apenas roles
conhecidas são aceitas, e o Admin não pode remover a própria role Admin. Após uma
alteração de roles, o usuário afetado deve autenticar novamente para que o novo
token reflita as roles.

Para autenticar como Admin, use a conta criada pelo seed (e-mail de
`Seed:AdminEmail`, senha de `Seed:AdminPassword`). Os reembolsos chegam nas issues
seguintes.

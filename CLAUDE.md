# CLAUDE.md — ExpenseHub

## Visão geral

ExpenseHub é uma API REST de reembolsos feita em ASP.NET Core, Entity Framework
Core, ASP.NET Core Identity e SQLite. Funcionários registram despesas, aprovadores
analisam, o Financeiro paga e auditores consultam o histórico.

## Integrantes

- Arthur Bueno de Oliveira — RM558396
- João Vitor Carotta Ribeiro — RM555187
- Victor Magdaleno Marcos — RM556729

## Estado do projeto

As issues funcionais I01–I10 estão implementadas na cópia local:

- fundação, EF Core, Identity, JWT, cadastro e administração de roles;
- criação, edição, envio, listagem e consulta de reembolsos;
- autorização por role, ownership, estado e visibilidade;
- aprovação e reprovação com justificativa;
- pagamento e consulta do histórico;
- testes unitários isolados;
- respostas separadas dos DTOs de entrada para atender ao pipeline de qualidade.

A branch local `i07-i10-finalizacao` parte de `origin/i04-i06-expenses`. As
alterações finais estão deliberadamente sem commit e sem push para que a equipe
revise e publique com a autoria correta.

## Regras de domínio essenciais

```text
Draft --submit--> Submitted --approve--> Approved --pay--> Paid
                         \--reject-------------------------> Rejected
```

- Somente `Employee` cria, edita e envia os próprios reembolsos.
- Somente `Approver` aprova ou reprova um `Submitted` de outra pessoa.
- Reprovação exige justificativa entre 10 e 500 caracteres.
- Somente `Finance` paga um `Approved` de outra pessoa.
- `Auditor` possui acesso de leitura a todos os reembolsos e históricos.
- `Admin` não recebe acesso funcional a reembolsos apenas por ser Admin.
- Recurso fora do escopo de leitura retorna `404`; falta de permissão retorna
  `403`; transição incompatível ou repetida retorna `409`.
- Proprietário, estado, ator e timestamps vêm exclusivamente do servidor.

## Organização técnica

- `sources/ExpenseHub.Api/Dtos`: somente modelos de entrada com DataAnnotations.
- `sources/ExpenseHub.Api/Responses`: modelos devolvidos pela API.
- `sources/ExpenseHub.Api/Services/ExpensePolicy.cs`: matriz de autorização pura.
- `sources/ExpenseHub.Api/Services/ExpenseService.cs`: regras e persistência.
- `sources/ExpenseHub.Api/Endpoints/ExpenseEndpoints.cs`: rotas minimal API.
- `sources/ExpenseHub.UnitTests`: testes de políticas, validações e serviço.

Toda transição, seu histórico e o eventual pagamento são gravados no mesmo
`SaveChangesAsync`. O horário vem de `TimeProvider`.

## Configuração local

```powershell
cd sources/ExpenseHub.Api
dotnet user-secrets set "Jwt:Key" "<chave-com-32-ou-mais-caracteres>"
dotnet user-secrets set "Seed:AdminPassword" "<senha-forte>"
```

Não versionar chaves, senhas, tokens, bancos SQLite nem artefatos de build.

## Validação antes do push

Na raiz do repositório:

```powershell
dotnet restore ./sources/ExpenseHub.slnx
dotnet build ./sources/ExpenseHub.slnx
dotnet test ./sources/ExpenseHub.slnx
dotnet format ./sources/ExpenseHub.slnx --verify-no-changes --severity warn
pwsh ./scripts/Invoke-CodeQuality.ps1
```

Última validação local desta cópia: build com 0 warnings e 0 erros, 37 testes
aprovados e relatório de qualidade 100/100. O Gitleaks não estava instalado no
ambiente de preparação; execute o pipeline sem `-SkipGitleaks` antes da entrega.

## Processo de Git

Não commitar diretamente na `main`. Revise as alterações, separe commits coesos
entre os integrantes e abra PRs no repositório do grupo. Ao referenciar as issues
originais, use `Racass/checkpoint-csharpracass-expensehub#N` sem `Closes`, `Fixes`
ou `Resolves`. Preserve mais de um commit real por integrante.

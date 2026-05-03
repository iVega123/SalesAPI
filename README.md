# SalesAPI — API de Gestão Comercial em .NET 10

API de vendas/estoque/financeiro construída com **.NET 10**, **Clean Architecture**, **EF Core 10 + PostgreSQL** e testes em duas camadas: unitários (xUnit) e BDD com **Reqnroll** (Gherkin pt-BR).

## Stack

| Item | Versão |
|---|---|
| .NET / C# | 10.0 / 14 |
| EF Core | 10.0.7 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 |
| PostgreSQL | 17 |
| Hangfire (jobs agendados) | 1.8.23 |
| MailKit (e-mail) | 4.16.0 |
| ClosedXML (exportação Excel) | 0.105.0 |
| QuestPDF (geração de PDF) | 2026.2.4 |
| Serilog | 10.0.0 |
| xUnit | 2.9.2 |
| Reqnroll | 3.3.4 |
| FluentAssertions | 7.0.0 |

## Estrutura

```
SalesAPI/
├── docker-compose.yml
├── global.json                         # SDK pinado em 10.0
├── SalesAPI.sln
├── src/
│   ├── SalesAPI/                       # Host — ASP.NET Core (Controllers, Program.cs)
│   ├── SalesAPI.Application/           # Casos de uso, DTOs, serviços, interfaces
│   ├── SalesAPI.Domain/                # Entidades e regras de domínio
│   └── SalesAPI.Infrastructure/        # EF Core, repositórios, migrations
└── tests/
    ├── SalesAPI.UnitTests/             # xUnit + EF InMemory
    ├── SalesAPI.BDD.Shared/            # WebApplicationFactory compartilhada
    ├── SalesAPI.BDD.Clientes/          # Cenários BDD — Clientes
    ├── SalesAPI.BDD.Produtos/          # Cenários BDD — Produtos
    └── SalesAPI.BDD.Vendas/            # Cenários BDD — Vendas
```

## Rodando

```bash
# 1) Sobe o Postgres
docker compose up -d

# 2) Aplica migrations
dotnet ef database update \
  --project src/SalesAPI.Infrastructure \
  --startup-project src/SalesAPI

# 3) Roda a API
dotnet run --project src/SalesAPI
# Swagger em http://localhost:5000/swagger
```

## Testes

```bash
# Todos
dotnet test

# Só unitários
dotnet test tests/SalesAPI.UnitTests

# BDD por domínio
dotnet test tests/SalesAPI.BDD.Clientes
dotnet test tests/SalesAPI.BDD.Produtos
dotnet test tests/SalesAPI.BDD.Vendas
```

Os testes BDD usam **EF InMemory** dentro do `WebApplicationFactory` — não precisam do Postgres rodando.

## Endpoints

| Área | Rota base |
|---|---|
| Autenticação (JWT) | `/api/auth` |
| Usuários | `/api/usuarios` |
| Clientes | `/api/clientes` |
| Produtos | `/api/produtos` |
| Categorias | `/api/categorias` |
| Marcas | `/api/marcas` |
| Fornecedores | `/api/fornecedores` |
| Estoque | `/api/estoque` |
| Vendas | `/api/vendas` |
| Vendas PDV | `/api/vendas-pdv` |
| Compras | `/api/compras` |
| Caixa | `/api/caixa` |
| Financeiro | `/api/financeiro` |
| Fiscal (NF-e) | `/api/fiscal` |
| Relatórios | `/api/relatorios` |
| Automação (jobs) | `/api/automacao` |

## Regras de negócio cobertas pelos testes

- Venda válida reduz estoque e calcula total
- Venda bloqueada com estoque insuficiente (estoque permanece intacto)
- Venda bloqueada para cliente ou produto inexistente
- Itens duplicados no mesmo pedido são agrupados
- Produto novo começa com estoque 0

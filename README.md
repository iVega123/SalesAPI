# SalesAPI — MVP CRUD em .NET 10 com BDD

API simples de Vendas/Produtos/Estoque/Clientes feita com **.NET 10 (LTS)**, **EF Core 10** + **PostgreSQL** e testes em duas camadas: unitários (xUnit) e de integração com **BDD via Reqnroll** (sucessor do SpecFlow), em Gherkin pt-BR.

## Stack

| Item | Versão |
|---|---|
| .NET / C# | 10.0 / 14 |
| EF Core | 10.0.1 |
| Npgsql.EntityFrameworkCore.PostgreSQL | 10.0.1 |
| PostgreSQL | 17 |
| xUnit | 2.9.2 |
| Reqnroll | 3.3.4 |
| FluentAssertions | 7.0.0 |

## Estrutura

```
SalesAPI/
├── docker-compose.yml          # PostgreSQL 17
├── global.json                 # SDK pinado em 10.0
├── SalesAPI.sln
├── src/SalesAPI/               # API ASP.NET Core
│   ├── Controllers/            # Clientes, Produtos, Estoque, Vendas
│   ├── Models/                 # Entidades EF
│   ├── DTOs/                   # Records de request/response
│   ├── Data/AppDbContext.cs
│   └── Services/VendaService.cs
└── tests/
    ├── SalesAPI.UnitTests/             # xUnit + EF InMemory (foco em regras de venda)
    └── SalesAPI.IntegrationTests/      # Reqnroll + WebApplicationFactory
        ├── Features/  *.feature        # Cenários em pt-BR
        ├── Steps/     *.cs             # Step definitions
        └── Support/   TestWebApplicationFactory.cs
```

## Rodando

```bash
# 1) Sobe o Postgres
docker compose up -d

# 2) Aplica migrations (gera o schema)
cd src/SalesAPI
dotnet ef migrations add Inicial
dotnet ef database update

# 3) Roda a API
dotnet run
# Swagger em http://localhost:5000/swagger
```

## Testes

```bash
# Tudo
dotnet test

# Só unitários
dotnet test tests/SalesAPI.UnitTests

# Só BDD (integração)
dotnet test tests/SalesAPI.IntegrationTests
```

Os testes de integração usam **EF InMemory** dentro do `WebApplicationFactory`, então **não precisam do Postgres rodando** — só a API em si precisa.

## Endpoints

| Verbo | Rota | Descrição |
|---|---|---|
| GET/POST/PUT/DELETE | `/api/clientes` | CRUD de clientes |
| GET/POST/PUT/DELETE | `/api/produtos` | CRUD de produtos |
| GET/PUT | `/api/estoque/{produtoId}` | Consultar e ajustar estoque |
| GET/POST | `/api/vendas` | Listar e registrar vendas (baixa estoque automaticamente) |

## Regras de negócio cobertas pelos testes

- Venda válida reduz estoque e calcula total
- Venda não pode ser feita com estoque insuficiente (e o estoque permanece intacto)
- Venda não pode ser feita para cliente inexistente
- Venda não pode ser feita com produto inexistente
- Itens duplicados no mesmo request são agrupados
- Produto novo começa com estoque 0

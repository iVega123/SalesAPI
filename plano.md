# Plano MVP ERP de Vendas + PDV (.NET)

## Stack Tecnológica
- **Backend:** ASP.NET Core 10 Web API, Entity Framework Core 10, PostgreSQL 17
- **Auth:** JWT Bearer + BCrypt para senhas
- **Logging:** Serilog (Console + File)
- **Jobs:** Hangfire (Fase 3+)
- **Frontend:** A definir (MAUI / Blazor / React)

---

## Fase 1 — Cadastros Base ✅ Em andamento

### 1. Usuários e Permissões
| Tabela | Descrição |
|---|---|
| `perfis` | Perfis de acesso (Admin, Vendedor, Estoquista…) |
| `permissoes` | Granular por perfil: recurso + ação (Visualizar/Criar/Editar/Excluir) |
| `usuarios` | Credenciais, perfil, status, último acesso |
| `logs_sistema` | Auditoria de todas as ações |

**Endpoints:**
- `POST /api/auth/login` — autenticar, retorna JWT
- `POST /api/auth/registrar` — criar usuário (admin only em prod)
- `GET/POST/PUT/DELETE /api/usuarios` — CRUD de usuários
- `GET/POST/PUT/DELETE /api/perfis` — CRUD de perfis

### 2. Cadastro de Produtos
| Tabela | Descrição |
|---|---|
| `categorias` | Árvore de categorias de produto |
| `marcas` | Marcas/fabricantes |
| `produtos` | Ficha completa: SKU, barras, preço, tributação, status |
| `produtos_variacoes` | Variações (tamanho, cor, modelo, material) com SKU/preço próprio |
| `produtos_precos_historico` | Histórico de mudanças de preço |

**Campos produto:** `codigo_interno`, `sku`, `codigo_barras`, `nome`, `descricao`, `categoria_id`, `marca_id`, `unidade`, `preco_custo`, `preco_venda`, `estoque_minimo`, `status`, `imagem_url`, `ncm`, `cfop`, `cst`, `origem`, `aliquota`

**Endpoints:**
- `GET/POST/PUT/DELETE /api/produtos`
- `GET /api/produtos/{id}/variacoes`
- `POST /api/produtos/{id}/variacoes`
- `GET /api/produtos/{id}/historico-precos`
- `GET/POST/PUT/DELETE /api/categorias`
- `GET/POST/PUT/DELETE /api/marcas`

### 3. Cadastro de Clientes
| Tabela | Descrição |
|---|---|
| `clientes` | PF/PJ, contato, limite de crédito, pontos de fidelidade |
| `enderecos_clientes` | Múltiplos endereços por cliente (tipo: residencial/comercial/entrega) |
| `credito_clientes` | Limite e saldo disponível em tempo real |

**Endpoints:**
- `GET/POST/PUT/DELETE /api/clientes`
- `GET/POST/PUT/DELETE /api/clientes/{id}/enderecos`

### 4. Cadastro de Fornecedores
| Tabela | Descrição |
|---|---|
| `fornecedores` | Dados empresariais, condições comerciais |
| `contatos_fornecedores` | Múltiplos contatos por fornecedor |
| `historico_compras_fornecedor` | Resumo de compras realizadas |

**Endpoints:**
- `GET/POST/PUT/DELETE /api/fornecedores`
- `GET/POST/PUT/DELETE /api/fornecedores/{id}/contatos`

---

## Fase 2 — Operações Comerciais

### 5. Controle de Estoque
| Tabela | Descrição |
|---|---|
| `estoques` | Quantidade atual, reservada, mínima por produto |
| `movimentacoes_estoque` | Entrada, saída, ajuste, transferência, perda |
| `inventarios` | Sessões de inventário físico |
| `ajustes_estoque` | Ajustes com motivo e aprovação |

**Endpoints:**
- `GET /api/estoque/{produtoId}` — saldo atual
- `POST /api/estoque/entrada` / `saida` / `ajuste`
- `POST /api/inventarios` — iniciar inventário
- `GET /api/estoque/alertas` — produtos abaixo do mínimo

### 6. Compras
| Tabela | Descrição |
|---|---|
| `compras` | Pedido/recebimento, XML NF-e, frete, impostos |
| `itens_compra` | Produtos, quantidades, custos |
| `recebimentos_compra` | Conferência de recebimento parcial/total |
| `contas_pagar` | Parcelas geradas automaticamente |

**Endpoints:**
- `GET/POST/PUT /api/compras`
- `POST /api/compras/{id}/receber`
- `POST /api/compras/importar-xml`

### 7. Vendas
| Tabela | Descrição |
|---|---|
| `vendas` | Pedido/orçamento, desconto, comissão |
| `itens_venda` | Produtos, quantidades, preços na data |
| `parcelas_venda` | Parcelamento com vencimentos |
| `comissoes` | Comissão por vendedor/produto |

**Endpoints:**
- `GET/POST/PUT /api/vendas`
- `GET/POST /api/vendas/{id}/parcelas`

---

## Fase 3 — PDV + Financeiro

### 8. PDV (Frente de Caixa)
| Tabela | Descrição |
|---|---|
| `caixa` | Abertura/fechamento de caixa por operador |
| `movimentos_caixa` | Sangrias, suprimentos, pagamentos |
| `vendas_pdv` | Vendas rápidas com múltiplas formas de pagamento |
| `itens_pdv` | Itens do carrinho PDV |

**Features:** Leitura de código de barras, NFC-e, troco, desconto, cancelamento, operadores.

### 9. Financeiro
- Contas a pagar/receber
- Fluxo de caixa
- Conciliação bancária
- DRE simplificado

---

## Fase 4 — Relatórios + Fiscal + Automação

### 10. Relatórios
- Vendas por período, produto, vendedor, cliente
- Estoque atual, giro, curva ABC
- Financeiro: DRE, fluxo de caixa
- Exportação PDF e Excel

### 11. Fiscal
- Emissão NF-e / NFC-e
- SPED Fiscal
- Cálculo automático de impostos (ICMS, PIS, COFINS, IPI)

### 12. Automação (Hangfire)
- Alertas de estoque mínimo (e-mail/push)
- Boletos de cobrança automáticos
- Backup de dados agendado
- Relatórios periódicos por e-mail

---

## Regras Transversais
- Todas as exclusões são lógicas (`deleted_at` ou `status = Inativo`) para entidades com histórico financeiro
- Auditoria automática via `LogSistema` em todas as operações de escrita
- Soft delete em Produtos com vendas associadas
- Histórico de preço registrado automaticamente ao alterar `preco_venda`
- Estoque atualizado automaticamente em Compras/Vendas (via service)
- JWT com expiração configurável; refresh token na Fase 3

---

## Status das Fases

| Fase | Status | Previsão |
|---|---|---|
| Fase 1 — Cadastros Base | ✅ Concluída | — |
| Fase 2 — Estoque, Compras, Vendas | ✅ Concluída | — |
| Fase 3 — PDV, Caixa, Financeiro | ✅ Concluída | — |
| Fase 4 — Relatórios, Fiscal, Automação | ⏳ Pendente | — |

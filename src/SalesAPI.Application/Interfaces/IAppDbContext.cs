using Microsoft.EntityFrameworkCore;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Interfaces;

public interface IAppDbContext
{
    // Auth
    DbSet<Usuario> Usuarios { get; }
    DbSet<Perfil> Perfis { get; }
    DbSet<Permissao> Permissoes { get; }
    DbSet<LogSistema> LogsSistema { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // Produtos
    DbSet<Produto> Produtos { get; }
    DbSet<Categoria> Categorias { get; }
    DbSet<Marca> Marcas { get; }
    DbSet<ProdutoVariacao> ProdutosVariacoes { get; }
    DbSet<ProdutoPrecoHistorico> ProdutosPrecosHistorico { get; }

    // Estoque
    DbSet<Estoque> Estoques { get; }
    DbSet<MovimentacaoEstoque> MovimentacoesEstoque { get; }
    DbSet<Inventario> Inventarios { get; }
    DbSet<AjusteEstoque> AjustesEstoque { get; }

    // Clientes
    DbSet<Cliente> Clientes { get; }
    DbSet<EnderecoCliente> EnderecosClientes { get; }
    DbSet<CreditoCliente> CreditoClientes { get; }

    // Fornecedores
    DbSet<Fornecedor> Fornecedores { get; }
    DbSet<ContatoFornecedor> ContatosFornecedores { get; }
    DbSet<HistoricoCompraFornecedor> HistoricoComprasFornecedor { get; }

    // Compras
    DbSet<Compra> Compras { get; }
    DbSet<ItemCompra> ItensCompra { get; }
    DbSet<RecebimentoCompra> RecebimentosCompra { get; }
    DbSet<ItemRecebimento> ItensRecebimento { get; }
    DbSet<ContaPagar> ContasPagar { get; }

    // Vendas
    DbSet<Venda> Vendas { get; }
    DbSet<ItemVenda> ItensVenda { get; }
    DbSet<ParcelaVenda> ParcelasVenda { get; }
    DbSet<Comissao> Comissoes { get; }

    // PDV
    DbSet<Caixa> Caixas { get; }
    DbSet<MovimentoCaixa> MovimentosCaixa { get; }
    DbSet<VendaPdv> VendasPdv { get; }
    DbSet<ItemPdv> ItensPdv { get; }
    DbSet<PagamentoPdv> PagamentosPdv { get; }

    // Financeiro
    DbSet<ContaBancaria> ContasBancarias { get; }
    DbSet<ContaReceber> ContasReceber { get; }
    DbSet<LancamentoFinanceiro> LancamentosFinanceiros { get; }

    // Fiscal
    DbSet<NotaFiscal> NotasFiscais { get; }
    DbSet<ItemNotaFiscal> ItensNotaFiscal { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Microsoft.EntityFrameworkCore.ChangeTracking.EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;
}

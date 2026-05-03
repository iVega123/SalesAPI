using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Infrastructure.Data;

public class AppDbContext : DbContext, IAppDbContext
{
    private const string Decimal18x2 = "numeric(18,2)";
    private const string Decimal5x2 = "numeric(5,2)";

    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // Auth
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<Perfil> Perfis => Set<Perfil>();
    public DbSet<Permissao> Permissoes => Set<Permissao>();
    public DbSet<LogSistema> LogsSistema => Set<LogSistema>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Produtos
    public DbSet<Produto> Produtos => Set<Produto>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Marca> Marcas => Set<Marca>();
    public DbSet<ProdutoVariacao> ProdutosVariacoes => Set<ProdutoVariacao>();
    public DbSet<ProdutoPrecoHistorico> ProdutosPrecosHistorico => Set<ProdutoPrecoHistorico>();

    // Estoque
    public DbSet<Estoque> Estoques => Set<Estoque>();
    public DbSet<MovimentacaoEstoque> MovimentacoesEstoque => Set<MovimentacaoEstoque>();
    public DbSet<Inventario> Inventarios => Set<Inventario>();
    public DbSet<AjusteEstoque> AjustesEstoque => Set<AjusteEstoque>();

    // Clientes
    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<EnderecoCliente> EnderecosClientes => Set<EnderecoCliente>();
    public DbSet<CreditoCliente> CreditoClientes => Set<CreditoCliente>();

    // Fornecedores
    public DbSet<Fornecedor> Fornecedores => Set<Fornecedor>();
    public DbSet<ContatoFornecedor> ContatosFornecedores => Set<ContatoFornecedor>();
    public DbSet<HistoricoCompraFornecedor> HistoricoComprasFornecedor => Set<HistoricoCompraFornecedor>();

    // Compras
    public DbSet<Compra> Compras => Set<Compra>();
    public DbSet<ItemCompra> ItensCompra => Set<ItemCompra>();
    public DbSet<RecebimentoCompra> RecebimentosCompra => Set<RecebimentoCompra>();
    public DbSet<ItemRecebimento> ItensRecebimento => Set<ItemRecebimento>();
    public DbSet<ContaPagar> ContasPagar => Set<ContaPagar>();

    // Vendas
    public DbSet<Venda> Vendas => Set<Venda>();
    public DbSet<ItemVenda> ItensVenda => Set<ItemVenda>();
    public DbSet<ParcelaVenda> ParcelasVenda => Set<ParcelaVenda>();
    public DbSet<Comissao> Comissoes => Set<Comissao>();

    // PDV
    public DbSet<Caixa> Caixas => Set<Caixa>();
    public DbSet<MovimentoCaixa> MovimentosCaixa => Set<MovimentoCaixa>();
    public DbSet<VendaPdv> VendasPdv => Set<VendaPdv>();
    public DbSet<ItemPdv> ItensPdv => Set<ItemPdv>();
    public DbSet<PagamentoPdv> PagamentosPdv => Set<PagamentoPdv>();

    // Financeiro
    public DbSet<ContaBancaria> ContasBancarias => Set<ContaBancaria>();
    public DbSet<ContaReceber> ContasReceber => Set<ContaReceber>();
    public DbSet<LancamentoFinanceiro> LancamentosFinanceiros => Set<LancamentoFinanceiro>();

    // Fiscal
    public DbSet<NotaFiscal> NotasFiscais => Set<NotaFiscal>();
    public DbSet<ItemNotaFiscal> ItensNotaFiscal => Set<ItemNotaFiscal>();

    EntityEntry<TEntity> IAppDbContext.Entry<TEntity>(TEntity entity) => base.Entry(entity);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Perfil
        modelBuilder.Entity<Perfil>(e =>
        {
            e.ToTable("perfis");
        });

        // Permissao
        modelBuilder.Entity<Permissao>(e =>
        {
            e.ToTable("permissoes");
            e.HasOne(p => p.Perfil)
             .WithMany(pf => pf.Permissoes)
             .HasForeignKey(p => p.PerfilId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Usuario
        modelBuilder.Entity<Usuario>(e =>
        {
            e.ToTable("usuarios");
            e.HasIndex(u => u.Email).IsUnique();
            e.HasOne(u => u.Perfil)
             .WithMany(p => p.Usuarios)
             .HasForeignKey(u => u.PerfilId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // LogSistema
        modelBuilder.Entity<LogSistema>(e =>
        {
            e.ToTable("logs_sistema");
            e.HasOne(l => l.Usuario)
             .WithMany(u => u.Logs)
             .HasForeignKey(l => l.UsuarioId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Categoria
        modelBuilder.Entity<Categoria>(e =>
        {
            e.ToTable("categorias");
        });

        // Marca
        modelBuilder.Entity<Marca>(e =>
        {
            e.ToTable("marcas");
        });

        // Produto
        modelBuilder.Entity<Produto>(e =>
        {
            e.ToTable("produtos");
            e.HasIndex(p => p.CodigoBarras).IsUnique().HasFilter("codigo_barras IS NOT NULL");
            e.HasIndex(p => p.SKU).IsUnique().HasFilter("sku IS NOT NULL");
            e.Property(p => p.PrecoCusto).HasColumnType(Decimal18x2);
            e.Property(p => p.PrecoVenda).HasColumnType(Decimal18x2);
            e.Property(p => p.Aliquota).HasColumnType(Decimal5x2);
            e.HasOne(p => p.Categoria)
             .WithMany(c => c.Produtos)
             .HasForeignKey(p => p.CategoriaId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Marca)
             .WithMany(m => m.Produtos)
             .HasForeignKey(p => p.MarcaId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(p => p.Estoque)
             .WithOne(s => s.Produto!)
             .HasForeignKey<Estoque>(s => s.ProdutoId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.Variacoes)
             .WithOne(v => v.Produto)
             .HasForeignKey(v => v.ProdutoId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(p => p.PrecosHistorico)
             .WithOne(h => h.Produto)
             .HasForeignKey(h => h.ProdutoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ProdutoVariacao
        modelBuilder.Entity<ProdutoVariacao>(e =>
        {
            e.ToTable("produtos_variacoes");
            e.Property(v => v.Preco).HasColumnType(Decimal18x2);
        });

        // ProdutoPrecoHistorico
        modelBuilder.Entity<ProdutoPrecoHistorico>(e =>
        {
            e.ToTable("produtos_precos_historico");
            e.Property(h => h.PrecoCusto).HasColumnType(Decimal18x2);
            e.Property(h => h.PrecoVenda).HasColumnType(Decimal18x2);
        });

        // Estoque
        modelBuilder.Entity<Estoque>(e =>
        {
            e.ToTable("estoques");
            e.HasIndex(s => s.ProdutoId).IsUnique();
        });

        // MovimentacaoEstoque
        modelBuilder.Entity<MovimentacaoEstoque>(e =>
        {
            e.ToTable("movimentacoes_estoque");
            e.HasOne(m => m.Produto)
             .WithMany()
             .HasForeignKey(m => m.ProdutoId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(m => m.Usuario)
             .WithMany()
             .HasForeignKey(m => m.UsuarioId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // Inventario
        modelBuilder.Entity<Inventario>(e =>
        {
            e.ToTable("inventarios");
            e.HasOne(i => i.Usuario)
             .WithMany()
             .HasForeignKey(i => i.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(i => i.Ajustes)
             .WithOne(a => a.Inventario)
             .HasForeignKey(a => a.InventarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // AjusteEstoque
        modelBuilder.Entity<AjusteEstoque>(e =>
        {
            e.ToTable("ajustes_estoque");
            e.Ignore(a => a.Diferenca);
            e.HasOne(a => a.Produto)
             .WithMany()
             .HasForeignKey(a => a.ProdutoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Cliente
        modelBuilder.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.HasIndex(c => c.Email).IsUnique();
            e.HasIndex(c => c.CPF).IsUnique().HasFilter("cpf IS NOT NULL");
            e.HasIndex(c => c.CNPJ).IsUnique().HasFilter("cnpj IS NOT NULL");
            e.Property(c => c.LimiteCredito).HasColumnType(Decimal18x2);
            e.HasMany(c => c.Enderecos)
             .WithOne(e => e.Cliente)
             .HasForeignKey(e => e.ClienteId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(c => c.Credito)
             .WithOne(cr => cr.Cliente)
             .HasForeignKey<CreditoCliente>(cr => cr.ClienteId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // EnderecoCliente
        modelBuilder.Entity<EnderecoCliente>(e =>
        {
            e.ToTable("enderecos_clientes");
        });

        // CreditoCliente
        modelBuilder.Entity<CreditoCliente>(e =>
        {
            e.ToTable("credito_clientes");
            e.Property(c => c.Limite).HasColumnType(Decimal18x2);
            e.Property(c => c.Utilizado).HasColumnType(Decimal18x2);
            e.Ignore(c => c.Disponivel);
        });

        // Fornecedor
        modelBuilder.Entity<Fornecedor>(e =>
        {
            e.ToTable("fornecedores");
            e.HasIndex(f => f.CNPJ).IsUnique();
            e.HasMany(f => f.Contatos)
             .WithOne(c => c.Fornecedor)
             .HasForeignKey(c => c.FornecedorId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(f => f.HistoricoCompras)
             .WithOne(h => h.Fornecedor)
             .HasForeignKey(h => h.FornecedorId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ContatoFornecedor
        modelBuilder.Entity<ContatoFornecedor>(e =>
        {
            e.ToTable("contatos_fornecedores");
        });

        // HistoricoCompraFornecedor
        modelBuilder.Entity<HistoricoCompraFornecedor>(e =>
        {
            e.ToTable("historico_compras_fornecedor");
            e.Property(h => h.Valor).HasColumnType(Decimal18x2);
        });

        // Compra
        modelBuilder.Entity<Compra>(e =>
        {
            e.ToTable("compras");
            e.Property(c => c.Subtotal).HasColumnType(Decimal18x2);
            e.Property(c => c.Frete).HasColumnType(Decimal18x2);
            e.Property(c => c.Impostos).HasColumnType(Decimal18x2);
            e.Property(c => c.Total).HasColumnType(Decimal18x2);
            e.HasOne(c => c.Fornecedor)
             .WithMany()
             .HasForeignKey(c => c.FornecedorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(c => c.Usuario)
             .WithMany()
             .HasForeignKey(c => c.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Itens)
             .WithOne(i => i.Compra)
             .HasForeignKey(i => i.CompraId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Recebimentos)
             .WithOne(r => r.Compra)
             .HasForeignKey(r => r.CompraId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.ContasPagar)
             .WithOne(cp => cp.Compra)
             .HasForeignKey(cp => cp.CompraId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemCompra
        modelBuilder.Entity<ItemCompra>(e =>
        {
            e.ToTable("itens_compra");
            e.Ignore(i => i.Subtotal);
            e.Property(i => i.PrecoUnitario).HasColumnType(Decimal18x2);
            e.HasOne(i => i.Produto)
             .WithMany()
             .HasForeignKey(i => i.ProdutoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // RecebimentoCompra
        modelBuilder.Entity<RecebimentoCompra>(e =>
        {
            e.ToTable("recebimentos_compra");
            e.HasOne(r => r.Usuario)
             .WithMany()
             .HasForeignKey(r => r.UsuarioId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(r => r.Itens)
             .WithOne(i => i.Recebimento)
             .HasForeignKey(i => i.RecebimentoId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemRecebimento
        modelBuilder.Entity<ItemRecebimento>(e =>
        {
            e.ToTable("itens_recebimento");
            e.HasOne(i => i.ItemCompra)
             .WithMany()
             .HasForeignKey(i => i.ItemCompraId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ContaPagar
        modelBuilder.Entity<ContaPagar>(e =>
        {
            e.ToTable("contas_pagar");
            e.Property(c => c.Valor).HasColumnType(Decimal18x2);
        });

        // Venda
        modelBuilder.Entity<Venda>(e =>
        {
            e.ToTable("vendas");
            e.Property(v => v.Subtotal).HasColumnType(Decimal18x2);
            e.Property(v => v.Desconto).HasColumnType(Decimal18x2);
            e.Property(v => v.Total).HasColumnType(Decimal18x2);
            e.HasOne(v => v.Cliente)
             .WithMany()
             .HasForeignKey(v => v.ClienteId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Vendedor)
             .WithMany()
             .HasForeignKey(v => v.VendedorId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(v => v.Itens)
             .WithOne(i => i.Venda!)
             .HasForeignKey(i => i.VendaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(v => v.Parcelas)
             .WithOne(p => p.Venda)
             .HasForeignKey(p => p.VendaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(v => v.Comissoes)
             .WithOne(c => c.Venda)
             .HasForeignKey(c => c.VendaId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemVenda
        modelBuilder.Entity<ItemVenda>(e =>
        {
            e.ToTable("itens_venda");
            e.Ignore(i => i.Subtotal);
            e.Property(i => i.PrecoUnitario).HasColumnType(Decimal18x2);
            e.HasOne(i => i.Produto)
             .WithMany()
             .HasForeignKey(i => i.ProdutoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ParcelaVenda
        modelBuilder.Entity<ParcelaVenda>(e =>
        {
            e.ToTable("parcelas_venda");
            e.Property(p => p.Valor).HasColumnType(Decimal18x2);
        });

        // Comissao
        modelBuilder.Entity<Comissao>(e =>
        {
            e.ToTable("comissoes");
            e.Property(c => c.PercentualComissao).HasColumnType(Decimal5x2);
            e.Property(c => c.ValorComissao).HasColumnType(Decimal18x2);
            e.HasOne(c => c.Vendedor)
             .WithMany()
             .HasForeignKey(c => c.VendedorId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Caixa
        modelBuilder.Entity<Caixa>(e =>
        {
            e.ToTable("caixas");
            e.Property(c => c.SaldoInicial).HasColumnType(Decimal18x2);
            e.Property(c => c.SaldoFinal).HasColumnType(Decimal18x2);
            e.HasOne(c => c.Operador)
             .WithMany()
             .HasForeignKey(c => c.OperadorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasMany(c => c.Movimentos)
             .WithOne(m => m.Caixa)
             .HasForeignKey(m => m.CaixaId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(c => c.Vendas)
             .WithOne(v => v.Caixa)
             .HasForeignKey(v => v.CaixaId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // MovimentoCaixa
        modelBuilder.Entity<MovimentoCaixa>(e =>
        {
            e.ToTable("movimentos_caixa");
            e.Property(m => m.Valor).HasColumnType(Decimal18x2);
            e.HasOne(m => m.Operador)
             .WithMany()
             .HasForeignKey(m => m.OperadorId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // VendaPdv
        modelBuilder.Entity<VendaPdv>(e =>
        {
            e.ToTable("vendas_pdv");
            e.Property(v => v.Subtotal).HasColumnType(Decimal18x2);
            e.Property(v => v.Desconto).HasColumnType(Decimal18x2);
            e.Property(v => v.Total).HasColumnType(Decimal18x2);
            e.Property(v => v.TotalPago).HasColumnType(Decimal18x2);
            e.Property(v => v.Troco).HasColumnType(Decimal18x2);
            e.HasOne(v => v.Operador)
             .WithMany()
             .HasForeignKey(v => v.OperadorId)
             .OnDelete(DeleteBehavior.Restrict);
            e.HasOne(v => v.Cliente)
             .WithMany()
             .HasForeignKey(v => v.ClienteId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(v => v.Itens)
             .WithOne(i => i.VendaPdv)
             .HasForeignKey(i => i.VendaPdvId)
             .OnDelete(DeleteBehavior.Cascade);
            e.HasMany(v => v.Pagamentos)
             .WithOne(p => p.VendaPdv)
             .HasForeignKey(p => p.VendaPdvId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemPdv
        modelBuilder.Entity<ItemPdv>(e =>
        {
            e.ToTable("itens_pdv");
            e.Ignore(i => i.Subtotal);
            e.Property(i => i.PrecoUnitario).HasColumnType(Decimal18x2);
            e.Property(i => i.Desconto).HasColumnType(Decimal18x2);
            e.HasOne(i => i.Produto)
             .WithMany()
             .HasForeignKey(i => i.ProdutoId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // PagamentoPdv
        modelBuilder.Entity<PagamentoPdv>(e =>
        {
            e.ToTable("pagamentos_pdv");
            e.Property(p => p.Valor).HasColumnType(Decimal18x2);
            e.Property(p => p.Troco).HasColumnType(Decimal18x2);
        });

        // ContaBancaria
        modelBuilder.Entity<ContaBancaria>(e =>
        {
            e.ToTable("contas_bancarias");
            e.Property(c => c.SaldoInicial).HasColumnType(Decimal18x2);
            e.HasMany(c => c.Lancamentos)
             .WithOne(l => l.ContaBancaria)
             .HasForeignKey(l => l.ContaBancariaId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ContaReceber
        modelBuilder.Entity<ContaReceber>(e =>
        {
            e.ToTable("contas_receber");
            e.Property(c => c.Valor).HasColumnType(Decimal18x2);
            e.HasOne(c => c.Cliente)
             .WithMany()
             .HasForeignKey(c => c.ClienteId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.Venda)
             .WithMany()
             .HasForeignKey(c => c.VendaId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(c => c.VendaPdv)
             .WithMany()
             .HasForeignKey(c => c.VendaPdvId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // LancamentoFinanceiro
        modelBuilder.Entity<LancamentoFinanceiro>(e =>
        {
            e.ToTable("lancamentos_financeiros");
            e.Property(l => l.Valor).HasColumnType(Decimal18x2);
            e.HasOne(l => l.Compra)
             .WithMany()
             .HasForeignKey(l => l.CompraId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(l => l.Venda)
             .WithMany()
             .HasForeignKey(l => l.VendaId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(l => l.VendaPdv)
             .WithMany()
             .HasForeignKey(l => l.VendaPdvId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(l => l.ContaPagar)
             .WithMany()
             .HasForeignKey(l => l.ContaPagarId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(l => l.ContaReceber)
             .WithMany()
             .HasForeignKey(l => l.ContaReceberId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // RefreshToken
        modelBuilder.Entity<RefreshToken>(e =>
        {
            e.ToTable("refresh_tokens");
            e.HasIndex(r => r.Token).IsUnique();
            e.HasOne(r => r.Usuario)
             .WithMany()
             .HasForeignKey(r => r.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // NotaFiscal
        modelBuilder.Entity<NotaFiscal>(e =>
        {
            e.ToTable("notas_fiscais");
            e.HasIndex(n => n.ChaveAcesso).IsUnique().HasFilter("chave_acesso IS NOT NULL");
            e.Property(n => n.ValorProdutos).HasColumnType(Decimal18x2);
            e.Property(n => n.ValorFrete).HasColumnType(Decimal18x2);
            e.Property(n => n.ValorDesconto).HasColumnType(Decimal18x2);
            e.Property(n => n.ValorICMS).HasColumnType(Decimal18x2);
            e.Property(n => n.ValorPIS).HasColumnType(Decimal18x2);
            e.Property(n => n.ValorCOFINS).HasColumnType(Decimal18x2);
            e.Property(n => n.ValorIPI).HasColumnType(Decimal18x2);
            e.Property(n => n.ValorTotal).HasColumnType(Decimal18x2);
            e.HasOne(n => n.Venda)
             .WithMany()
             .HasForeignKey(n => n.VendaId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(n => n.VendaPdv)
             .WithMany()
             .HasForeignKey(n => n.VendaPdvId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasOne(n => n.Cliente)
             .WithMany()
             .HasForeignKey(n => n.ClienteId)
             .OnDelete(DeleteBehavior.SetNull);
            e.HasMany(n => n.Itens)
             .WithOne(i => i.NotaFiscal)
             .HasForeignKey(i => i.NotaFiscalId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ItemNotaFiscal
        modelBuilder.Entity<ItemNotaFiscal>(e =>
        {
            e.ToTable("itens_nota_fiscal");
            e.Property(i => i.ValorUnitario).HasColumnType(Decimal18x2);
            e.Property(i => i.ValorDesconto).HasColumnType(Decimal18x2);
            e.Property(i => i.ValorBruto).HasColumnType(Decimal18x2);
            e.Property(i => i.AliquotaICMS).HasColumnType(Decimal5x2);
            e.Property(i => i.ValorICMS).HasColumnType(Decimal18x2);
            e.Property(i => i.AliquotaPIS).HasColumnType(Decimal5x2);
            e.Property(i => i.ValorPIS).HasColumnType(Decimal18x2);
            e.Property(i => i.AliquotaCOFINS).HasColumnType(Decimal5x2);
            e.Property(i => i.ValorCOFINS).HasColumnType(Decimal18x2);
            e.Property(i => i.AliquotaIPI).HasColumnType(Decimal5x2);
            e.Property(i => i.ValorIPI).HasColumnType(Decimal18x2);
            e.HasOne(i => i.Produto)
             .WithMany()
             .HasForeignKey(i => i.ProdutoId)
             .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

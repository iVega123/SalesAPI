using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Caixa
{
    public int Id { get; set; }

    public int OperadorId { get; set; }
    public Usuario? Operador { get; set; }

    public DateTime DataAbertura { get; set; } = DateTime.UtcNow;

    public DateTime? DataFechamento { get; set; }

    public decimal SaldoInicial { get; set; }

    public decimal? SaldoFinal { get; set; }

    public StatusCaixa Status { get; set; } = StatusCaixa.Aberto;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public List<MovimentoCaixa> Movimentos { get; set; } = new();

    public List<VendaPdv> Vendas { get; set; } = new();
}

public class MovimentoCaixa
{
    public int Id { get; set; }

    public int CaixaId { get; set; }
    public Caixa? Caixa { get; set; }

    public TipoMovimentoCaixa Tipo { get; set; }

    public decimal Valor { get; set; }

    [MaxLength(300)]
    public string? Descricao { get; set; }

    public int? OperadorId { get; set; }
    public Usuario? Operador { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class VendaPdv
{
    public int Id { get; set; }

    public int CaixaId { get; set; }
    public Caixa? Caixa { get; set; }

    public int OperadorId { get; set; }
    public Usuario? Operador { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public StatusVendaPdv Status { get; set; } = StatusVendaPdv.Finalizada;

    public DateTime DataVenda { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }

    public decimal Desconto { get; set; }

    public decimal Total { get; set; }

    public decimal TotalPago { get; set; }

    public decimal Troco { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public List<ItemPdv> Itens { get; set; } = new();

    public List<PagamentoPdv> Pagamentos { get; set; } = new();
}

public class ItemPdv
{
    public int Id { get; set; }

    public int VendaPdvId { get; set; }
    public VendaPdv? VendaPdv { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public int Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public decimal Desconto { get; set; }

    public decimal Subtotal => (PrecoUnitario * Quantidade) - Desconto;
}

public class PagamentoPdv
{
    public int Id { get; set; }

    public int VendaPdvId { get; set; }
    public VendaPdv? VendaPdv { get; set; }

    public FormaPagamento Forma { get; set; }

    public decimal Valor { get; set; }

    public decimal Troco { get; set; }
}

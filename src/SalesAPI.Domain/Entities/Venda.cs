using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Venda
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? VendedorId { get; set; }
    public Usuario? Vendedor { get; set; }

    public StatusVenda Status { get; set; } = StatusVenda.Confirmada;

    public DateTime DataVenda { get; set; } = DateTime.UtcNow;

    public decimal Subtotal { get; set; }

    public decimal Desconto { get; set; }

    public decimal Total { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public List<ItemVenda> Itens { get; set; } = new();

    public List<ParcelaVenda> Parcelas { get; set; } = new();

    public List<Comissao> Comissoes { get; set; } = new();
}

public class ItemVenda
{
    public int Id { get; set; }

    public int VendaId { get; set; }
    public Venda? Venda { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public int Quantidade { get; set; }

    public decimal PrecoUnitario { get; set; }

    public decimal Subtotal => Quantidade * PrecoUnitario;
}

public class ParcelaVenda
{
    public int Id { get; set; }

    public int VendaId { get; set; }
    public Venda? Venda { get; set; }

    public int Numero { get; set; }

    public int TotalParcelas { get; set; }

    public decimal Valor { get; set; }

    public DateTime Vencimento { get; set; }

    public DateTime? PagamentoEm { get; set; }

    public StatusParcela Status { get; set; } = StatusParcela.Pendente;

    [MaxLength(300)]
    public string? Observacoes { get; set; }
}

public class Comissao
{
    public int Id { get; set; }

    public int VendaId { get; set; }
    public Venda? Venda { get; set; }

    public int VendedorId { get; set; }
    public Usuario? Vendedor { get; set; }

    public decimal PercentualComissao { get; set; }

    public decimal ValorComissao { get; set; }

    public bool Paga { get; set; }

    public DateTime? PagaEm { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

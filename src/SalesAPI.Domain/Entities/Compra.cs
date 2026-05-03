using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Compra
{
    public int Id { get; set; }

    public int FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    public StatusCompra Status { get; set; } = StatusCompra.Rascunho;

    public DateTime DataCompra { get; set; } = DateTime.UtcNow;

    public DateTime? DataPrevisaoEntrega { get; set; }

    public decimal Subtotal { get; set; }

    public decimal Frete { get; set; }

    public decimal Impostos { get; set; }

    public decimal Total { get; set; }

    [MaxLength(100)]
    public string? NumeroNF { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public List<ItemCompra> Itens { get; set; } = new();

    public List<RecebimentoCompra> Recebimentos { get; set; } = new();

    public List<ContaPagar> ContasPagar { get; set; } = new();

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

public class ItemCompra
{
    public int Id { get; set; }

    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public int Quantidade { get; set; }

    public int QuantidadeRecebida { get; set; }

    public decimal PrecoUnitario { get; set; }

    public decimal Subtotal => PrecoUnitario * Quantidade;
}

public class RecebimentoCompra
{
    public int Id { get; set; }

    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public DateTime DataRecebimento { get; set; } = DateTime.UtcNow;

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public List<ItemRecebimento> Itens { get; set; } = new();
}

public class ItemRecebimento
{
    public int Id { get; set; }

    public int RecebimentoId { get; set; }
    public RecebimentoCompra? Recebimento { get; set; }

    public int ItemCompraId { get; set; }
    public ItemCompra? ItemCompra { get; set; }

    public int QuantidadeRecebida { get; set; }
}

public class ContaPagar
{
    public int Id { get; set; }

    public int CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int Parcela { get; set; }

    public int TotalParcelas { get; set; }

    public decimal Valor { get; set; }

    public DateTime Vencimento { get; set; }

    public DateTime? PagamentoEm { get; set; }

    public StatusContaPagar Status { get; set; } = StatusContaPagar.Pendente;

    [MaxLength(300)]
    public string? Observacoes { get; set; }
}

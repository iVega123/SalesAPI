using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Produto
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string? CodigoInterno { get; set; }

    [MaxLength(100)]
    public string? SKU { get; set; }

    [MaxLength(50)]
    public string? CodigoBarras { get; set; }

    [Required, MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Descricao { get; set; } = string.Empty;

    public int? CategoriaId { get; set; }
    public Categoria? Categoria { get; set; }

    public int? MarcaId { get; set; }
    public Marca? Marca { get; set; }

    [MaxLength(20)]
    public string Unidade { get; set; } = "UN";

    public decimal PrecoCusto { get; set; }

    public decimal PrecoVenda { get; set; }

    public int EstoqueMinimo { get; set; } = 0;

    public StatusProduto Status { get; set; } = StatusProduto.Ativo;

    [MaxLength(500)]
    public string? ImagemUrl { get; set; }

    [MaxLength(10)]
    public string? NCM { get; set; }

    [MaxLength(10)]
    public string? CFOP { get; set; }

    [MaxLength(10)]
    public string? CST { get; set; }

    [MaxLength(2)]
    public string? Origem { get; set; }

    public decimal Aliquota { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public Estoque? Estoque { get; set; }
    public ICollection<ProdutoVariacao> Variacoes { get; set; } = [];
    public ICollection<ProdutoPrecoHistorico> PrecosHistorico { get; set; } = [];
}

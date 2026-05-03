using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class ProdutoVariacao
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    [MaxLength(50)]
    public string? Tamanho { get; set; }

    [MaxLength(50)]
    public string? Cor { get; set; }

    [MaxLength(100)]
    public string? Modelo { get; set; }

    [MaxLength(100)]
    public string? Material { get; set; }

    [MaxLength(100)]
    public string? SKU { get; set; }

    public decimal Preco { get; set; }

    public int EstoqueQtd { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

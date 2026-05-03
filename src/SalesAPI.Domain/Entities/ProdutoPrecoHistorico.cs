using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class ProdutoPrecoHistorico
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public decimal PrecoCusto { get; set; }

    public decimal PrecoVenda { get; set; }

    [MaxLength(300)]
    public string? Motivo { get; set; }

    public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
}

using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Estoque
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    [Range(0, int.MaxValue)]
    public int Quantidade { get; set; }

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

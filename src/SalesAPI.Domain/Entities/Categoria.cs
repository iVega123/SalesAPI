using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Categoria
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Descricao { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<Produto> Produtos { get; set; } = [];
}

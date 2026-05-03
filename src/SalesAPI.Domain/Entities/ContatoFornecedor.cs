using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class ContatoFornecedor
{
    public int Id { get; set; }

    public int FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    [Required, MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Cargo { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    public bool Principal { get; set; } = false;
}

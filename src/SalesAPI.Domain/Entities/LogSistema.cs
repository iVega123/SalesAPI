using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class LogSistema
{
    public long Id { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    [Required, MaxLength(100)]
    public string Acao { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Recurso { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Detalhes { get; set; }

    [MaxLength(45)]
    public string? EnderecoIP { get; set; }

    public DateTime DataHora { get; set; } = DateTime.UtcNow;
}

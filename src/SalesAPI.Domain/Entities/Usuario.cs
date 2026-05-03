using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Usuario
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    [Required, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string SenhaHash { get; set; } = string.Empty;

    public int PerfilId { get; set; }
    public Perfil? Perfil { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime? UltimoAcesso { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<LogSistema> Logs { get; set; } = [];
}

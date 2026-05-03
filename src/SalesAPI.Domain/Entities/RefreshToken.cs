using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class RefreshToken
{
    public int Id { get; set; }

    [MaxLength(500)]
    public required string Token { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public DateTime ExpiraEm { get; set; }

    public bool Revogado { get; set; }
}

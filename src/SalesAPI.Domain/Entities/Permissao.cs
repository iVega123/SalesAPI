using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Permissao
{
    public int Id { get; set; }

    public int PerfilId { get; set; }
    public Perfil? Perfil { get; set; }

    [Required, MaxLength(100)]
    public string Recurso { get; set; } = string.Empty;

    public AcaoPermissao Acao { get; set; }
}

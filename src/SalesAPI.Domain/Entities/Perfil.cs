using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Perfil
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string Nome { get; set; } = string.Empty;

    [MaxLength(300)]
    public string Descricao { get; set; } = string.Empty;

    public bool Ativo { get; set; } = true;

    public ICollection<Permissao> Permissoes { get; set; } = [];
    public ICollection<Usuario> Usuarios { get; set; } = [];
}

using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Inventario
{
    public int Id { get; set; }

    [MaxLength(300)]
    public string? Descricao { get; set; }

    public DateTime IniciadoEm { get; set; } = DateTime.UtcNow;

    public DateTime? FinalizadoEm { get; set; }

    public int UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public bool Finalizado { get; set; }

    public List<AjusteEstoque> Ajustes { get; set; } = new();
}

public class AjusteEstoque
{
    public int Id { get; set; }

    public int InventarioId { get; set; }
    public Inventario? Inventario { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public int QuantidadeContada { get; set; }

    public int QuantidadeSistema { get; set; }

    public int Diferenca => QuantidadeContada - QuantidadeSistema;

    [MaxLength(500)]
    public string? Motivo { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class MovimentacaoEstoque
{
    public int Id { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    public TipoMovimentacao Tipo { get; set; }

    public int Quantidade { get; set; }

    public int QuantidadeAnterior { get; set; }

    public int QuantidadeResultante { get; set; }

    [MaxLength(500)]
    public string? Motivo { get; set; }

    public int? UsuarioId { get; set; }
    public Usuario? Usuario { get; set; }

    public int? CompraId { get; set; }
    public int? VendaId { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

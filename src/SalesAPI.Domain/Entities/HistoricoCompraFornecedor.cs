using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class HistoricoCompraFornecedor
{
    public int Id { get; set; }

    public int FornecedorId { get; set; }
    public Fornecedor? Fornecedor { get; set; }

    public DateTime DataCompra { get; set; }

    [MaxLength(50)]
    public string? NumeroDocumento { get; set; }

    public decimal Valor { get; set; }

    [MaxLength(500)]
    public string? Descricao { get; set; }

    public DateTime RegistradoEm { get; set; } = DateTime.UtcNow;
}

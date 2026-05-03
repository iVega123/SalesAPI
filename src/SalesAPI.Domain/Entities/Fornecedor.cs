using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Fornecedor
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string RazaoSocial { get; set; } = string.Empty;

    [MaxLength(200)]
    public string NomeFantasia { get; set; } = string.Empty;

    [Required, MaxLength(18)]
    public string CNPJ { get; set; } = string.Empty;

    [MaxLength(30)]
    public string? IE { get; set; }

    [MaxLength(150)]
    public string? Email { get; set; }

    [MaxLength(20)]
    public string? Telefone { get; set; }

    [MaxLength(200)]
    public string? Site { get; set; }

    [MaxLength(300)]
    public string? CondicoesPagamento { get; set; }

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public StatusFornecedor Status { get; set; } = StatusFornecedor.Ativo;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<ContatoFornecedor> Contatos { get; set; } = [];
    public ICollection<HistoricoCompraFornecedor> HistoricoCompras { get; set; } = [];
}

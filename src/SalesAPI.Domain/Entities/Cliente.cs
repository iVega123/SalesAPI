using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class Cliente
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string Nome { get; set; } = string.Empty;

    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Fisica;

    [MaxLength(14)]
    public string? CPF { get; set; }

    [MaxLength(18)]
    public string? CNPJ { get; set; }

    [Required, EmailAddress, MaxLength(150)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Telefone { get; set; }

    public DateTime? DataNascimento { get; set; }

    public decimal LimiteCredito { get; set; } = 0;

    public int PontosFidelidade { get; set; } = 0;

    public StatusCliente Status { get; set; } = StatusCliente.Ativo;

    [MaxLength(500)]
    public string? Observacoes { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public ICollection<EnderecoCliente> Enderecos { get; set; } = [];
    public CreditoCliente? Credito { get; set; }
}

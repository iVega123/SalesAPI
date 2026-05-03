using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class EnderecoCliente
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [Required, MaxLength(200)]
    public string Logradouro { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Numero { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Complemento { get; set; }

    [Required, MaxLength(100)]
    public string Bairro { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string Cidade { get; set; } = string.Empty;

    [Required, MaxLength(2)]
    public string Estado { get; set; } = string.Empty;

    [Required, MaxLength(9)]
    public string CEP { get; set; } = string.Empty;

    public TipoEndereco Tipo { get; set; } = TipoEndereco.Residencial;

    public bool Principal { get; set; } = false;
}

namespace SalesAPI.Domain.Entities;

public class CreditoCliente
{
    public int Id { get; set; }

    public int ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public decimal Limite { get; set; }

    public decimal Utilizado { get; set; }

    public decimal Disponivel => Limite - Utilizado;

    public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;
}

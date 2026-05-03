using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class ContaBancaria
{
    public int Id { get; set; }

    [MaxLength(150)]
    public required string Nome { get; set; }

    [MaxLength(100)]
    public string? Banco { get; set; }

    [MaxLength(20)]
    public string? Agencia { get; set; }

    [MaxLength(30)]
    public string? Conta { get; set; }

    public TipoConta Tipo { get; set; } = TipoConta.Corrente;

    public decimal SaldoInicial { get; set; }

    public bool Ativo { get; set; } = true;

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public List<LancamentoFinanceiro> Lancamentos { get; set; } = new();
}

public class ContaReceber
{
    public int Id { get; set; }

    [MaxLength(300)]
    public required string Descricao { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    public int? VendaId { get; set; }
    public Venda? Venda { get; set; }

    public int? VendaPdvId { get; set; }
    public VendaPdv? VendaPdv { get; set; }

    public decimal Valor { get; set; }

    public DateTime Vencimento { get; set; }

    public DateTime? PagamentoEm { get; set; }

    public StatusContaReceber Status { get; set; } = StatusContaReceber.Pendente;

    [MaxLength(500)]
    public string? Observacoes { get; set; }
}

public class LancamentoFinanceiro
{
    public int Id { get; set; }

    [MaxLength(300)]
    public required string Descricao { get; set; }

    public TipoLancamento Tipo { get; set; }

    public decimal Valor { get; set; }

    public DateTime DataLancamento { get; set; } = DateTime.UtcNow;

    [MaxLength(100)]
    public string? Categoria { get; set; }

    public int? ContaBancariaId { get; set; }
    public ContaBancaria? ContaBancaria { get; set; }

    public int? CompraId { get; set; }
    public Compra? Compra { get; set; }

    public int? VendaId { get; set; }
    public Venda? Venda { get; set; }

    public int? VendaPdvId { get; set; }
    public VendaPdv? VendaPdv { get; set; }

    public int? ContaPagarId { get; set; }
    public ContaPagar? ContaPagar { get; set; }

    public int? ContaReceberId { get; set; }
    public ContaReceber? ContaReceber { get; set; }

    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
}

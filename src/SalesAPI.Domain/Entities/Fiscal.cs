using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Domain.Entities;

public class NotaFiscal
{
    public int Id { get; set; }

    public TipoNotaFiscal Tipo { get; set; } = TipoNotaFiscal.NFe;

    [MaxLength(9)]
    public string? NumeroNota { get; set; }

    [MaxLength(3)]
    public string? Serie { get; set; }

    [MaxLength(44)]
    public string? ChaveAcesso { get; set; }

    public StatusNotaFiscal Status { get; set; } = StatusNotaFiscal.Rascunho;

    public int? VendaId { get; set; }
    public Venda? Venda { get; set; }

    public int? VendaPdvId { get; set; }
    public VendaPdv? VendaPdv { get; set; }

    public int? ClienteId { get; set; }
    public Cliente? Cliente { get; set; }

    [MaxLength(200)]
    public string? NomeDestinatario { get; set; }

    [MaxLength(18)]
    public string? CpfCnpjDestinatario { get; set; }

    [MaxLength(300)]
    public string? EnderecoDestinatario { get; set; }

    public decimal ValorProdutos { get; set; }
    public decimal ValorFrete { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorICMS { get; set; }
    public decimal ValorPIS { get; set; }
    public decimal ValorCOFINS { get; set; }
    public decimal ValorIPI { get; set; }
    public decimal ValorTotal { get; set; }

    public string? XmlGerado { get; set; }

    [MaxLength(500)]
    public string? MotivoCancel { get; set; }

    [MaxLength(500)]
    public string? MensagemErro { get; set; }

    public DateTime? DataEmissao { get; set; }
    public DateTime? DataCancelamento { get; set; }
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

    public List<ItemNotaFiscal> Itens { get; set; } = new();
}

public class ItemNotaFiscal
{
    public int Id { get; set; }

    public int NotaFiscalId { get; set; }
    public NotaFiscal? NotaFiscal { get; set; }

    public int ProdutoId { get; set; }
    public Produto? Produto { get; set; }

    [MaxLength(150)]
    public string ProdutoNome { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Unidade { get; set; } = "UN";

    public int Quantidade { get; set; }

    public decimal ValorUnitario { get; set; }
    public decimal ValorDesconto { get; set; }
    public decimal ValorBruto { get; set; }

    [MaxLength(10)]
    public string? NCM { get; set; }

    [MaxLength(10)]
    public string? CFOP { get; set; }

    [MaxLength(10)]
    public string? CST { get; set; }

    [MaxLength(2)]
    public string? Origem { get; set; }

    public decimal AliquotaICMS { get; set; }
    public decimal ValorICMS { get; set; }

    public decimal AliquotaPIS { get; set; }
    public decimal ValorPIS { get; set; }

    public decimal AliquotaCOFINS { get; set; }
    public decimal ValorCOFINS { get; set; }

    public decimal AliquotaIPI { get; set; }
    public decimal ValorIPI { get; set; }
}

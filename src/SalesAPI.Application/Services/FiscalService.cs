using System.Text;
using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SalesAPI.Application.DTOs;
using SalesAPI.Application.Interfaces;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.Services;

public interface IFiscalService
{
    Task<NotaFiscalResponse> EmitirNfeAsync(EmitirNfeRequest request, CancellationToken ct = default);
    Task<string> GerarXmlNfeAsync(int notaId, CancellationToken ct = default);
    Task<NotaFiscalResponse> CancelarNfeAsync(int notaId, CancelarNfeRequest request, CancellationToken ct = default);
    Task<List<NotaFiscalResponse>> ListarNotasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default);
    Task<NotaFiscalResponse?> ObterNotaAsync(int notaId, CancellationToken ct = default);
    CalcularImpostosResponse CalcularImpostos(CalcularImpostosRequest request, Produto produto);
    Task<byte[]> GerarSpedFiscalAsync(int ano, int mes, CancellationToken ct = default);
}

public class FiscalService : IFiscalService
{
    private readonly IAppDbContext _db;
    private readonly IConfiguration _config;

    private decimal AliquotaPIS => decimal.Parse(_config["Fiscal:AliquotaPIS"] ?? "0.65");
    private decimal AliquotaCOFINS => decimal.Parse(_config["Fiscal:AliquotaCOFINS"] ?? "3.00");

    public FiscalService(IAppDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    // ── Emitir NF-e ──────────────────────────────────────────────────────────

    public async Task<NotaFiscalResponse> EmitirNfeAsync(EmitirNfeRequest request, CancellationToken ct = default)
    {
        if (request.VendaId == null && request.VendaPdvId == null)
            throw new InvalidOperationException("Informe VendaId ou VendaPdvId para emitir a nota.");

        List<ItemNotaFiscal> itens;
        decimal desconto, frete = 0;
        string? nomeDestinatario = request.NomeDestinatario;
        string? cpfCnpj = request.CpfCnpjDestinatario;
        string? endereco = request.EnderecoDestinatario;

        if (request.VendaId.HasValue)
        {
            var venda = await _db.Vendas
                .Include(v => v.Itens).ThenInclude(i => i.Produto)
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == request.VendaId.Value, ct)
                ?? throw new InvalidOperationException($"Venda {request.VendaId} não encontrada.");

            if (venda.Status != StatusVenda.Confirmada)
                throw new InvalidOperationException("Só é possível emitir NF-e para vendas confirmadas.");

            desconto = venda.Desconto;
            nomeDestinatario ??= venda.Cliente?.Nome;
            cpfCnpj ??= venda.Cliente?.CPF ?? venda.Cliente?.CNPJ;
            itens = venda.Itens.Select(i => CriarItemNota(i.Produto!, i.Quantidade, i.PrecoUnitario, 0)).ToList();
        }
        else
        {
            var vendaPdv = await _db.VendasPdv
                .Include(v => v.Itens).ThenInclude(i => i.Produto)
                .Include(v => v.Cliente)
                .FirstOrDefaultAsync(v => v.Id == request.VendaPdvId!.Value, ct)
                ?? throw new InvalidOperationException($"Venda PDV {request.VendaPdvId} não encontrada.");

            if (vendaPdv.Status != StatusVendaPdv.Finalizada)
                throw new InvalidOperationException("Só é possível emitir NFC-e para vendas finalizadas.");

            desconto = vendaPdv.Desconto;
            nomeDestinatario ??= vendaPdv.Cliente?.Nome ?? "Consumidor Final";
            cpfCnpj ??= vendaPdv.Cliente?.CPF ?? vendaPdv.Cliente?.CNPJ;
            itens = vendaPdv.Itens.Select(i => CriarItemNota(i.Produto!, i.Quantidade, i.PrecoUnitario, i.Desconto)).ToList();
        }

        var nota = new NotaFiscal
        {
            Tipo = request.Tipo,
            Serie = request.Serie,
            Status = StatusNotaFiscal.Emitida,
            VendaId = request.VendaId,
            VendaPdvId = request.VendaPdvId,
            ClienteId = request.ClienteId,
            NomeDestinatario = nomeDestinatario,
            CpfCnpjDestinatario = cpfCnpj,
            EnderecoDestinatario = endereco,
            ValorProdutos = itens.Sum(i => i.ValorBruto),
            ValorFrete = frete,
            ValorDesconto = desconto,
            ValorICMS = itens.Sum(i => i.ValorICMS),
            ValorPIS = itens.Sum(i => i.ValorPIS),
            ValorCOFINS = itens.Sum(i => i.ValorCOFINS),
            ValorIPI = itens.Sum(i => i.ValorIPI),
            DataEmissao = DateTime.UtcNow,
            Itens = itens
        };

        nota.ValorTotal = nota.ValorProdutos + nota.ValorFrete + nota.ValorIPI - nota.ValorDesconto;
        nota.NumeroNota = await ProximoNumeroNotaAsync(request.Tipo, request.Serie, ct);
        nota.ChaveAcesso = GerarChaveAcesso(nota);
        nota.XmlGerado = GerarXml(nota);

        _db.NotasFiscais.Add(nota);
        await _db.SaveChangesAsync(ct);

        return await ObterNotaAsync(nota.Id, ct) ?? throw new InvalidOperationException("Erro ao recuperar nota emitida.");
    }

    public async Task<string> GerarXmlNfeAsync(int notaId, CancellationToken ct = default)
    {
        var nota = await _db.NotasFiscais
            .Include(n => n.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(n => n.Id == notaId, ct)
            ?? throw new InvalidOperationException($"Nota {notaId} não encontrada.");

        return GerarXml(nota);
    }

    public async Task<NotaFiscalResponse> CancelarNfeAsync(int notaId, CancelarNfeRequest request, CancellationToken ct = default)
    {
        var nota = await _db.NotasFiscais
            .Include(n => n.Itens).ThenInclude(i => i.Produto)
            .FirstOrDefaultAsync(n => n.Id == notaId, ct)
            ?? throw new InvalidOperationException($"Nota {notaId} não encontrada.");

        if (nota.Status == StatusNotaFiscal.Cancelada)
            throw new InvalidOperationException("Nota já cancelada.");

        if (nota.Status != StatusNotaFiscal.Emitida)
            throw new InvalidOperationException("Apenas notas emitidas podem ser canceladas.");

        if (nota.DataEmissao.HasValue && DateTime.UtcNow - nota.DataEmissao.Value > TimeSpan.FromHours(24))
            throw new InvalidOperationException("O prazo de cancelamento (24h) foi excedido.");

        nota.Status = StatusNotaFiscal.Cancelada;
        nota.MotivoCancel = request.Motivo;
        nota.DataCancelamento = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return MapNota(nota);
    }

    public async Task<List<NotaFiscalResponse>> ListarNotasAsync(DateTime inicio, DateTime fim, CancellationToken ct = default)
    {
        var fimDia = fim.Date.AddDays(1).AddTicks(-1);

        var notas = await _db.NotasFiscais
            .Include(n => n.Itens).ThenInclude(i => i.Produto)
            .AsNoTracking()
            .Where(n => n.CriadoEm >= inicio && n.CriadoEm <= fimDia)
            .OrderByDescending(n => n.CriadoEm)
            .ToListAsync(ct);

        return notas.Select(MapNota).ToList();
    }

    public async Task<NotaFiscalResponse?> ObterNotaAsync(int notaId, CancellationToken ct = default)
    {
        var nota = await _db.NotasFiscais
            .Include(n => n.Itens).ThenInclude(i => i.Produto)
            .AsNoTracking()
            .FirstOrDefaultAsync(n => n.Id == notaId, ct);

        return nota is null ? null : MapNota(nota);
    }

    // ── Cálculo de Impostos ───────────────────────────────────────────────────

    public CalcularImpostosResponse CalcularImpostos(CalcularImpostosRequest request, Produto produto)
    {
        var valorBruto = request.ValorUnitario * request.Quantidade;
        var valorDesconto = request.Desconto;
        var baseCalculo = valorBruto - valorDesconto;

        var aliquotaICMS = produto.Aliquota;
        var valorICMS = Math.Round(baseCalculo * aliquotaICMS / 100, 2);

        var aliquotaPIS = AliquotaPIS;
        var valorPIS = Math.Round(baseCalculo * aliquotaPIS / 100, 2);

        var aliquotaCOFINS = AliquotaCOFINS;
        var valorCOFINS = Math.Round(baseCalculo * aliquotaCOFINS / 100, 2);

        var aliquotaIPI = 0m;
        var valorIPI = 0m;

        var totalImpostos = valorICMS + valorPIS + valorCOFINS + valorIPI;

        return new CalcularImpostosResponse(
            valorBruto,
            valorDesconto,
            baseCalculo,
            aliquotaICMS,
            valorICMS,
            aliquotaPIS,
            valorPIS,
            aliquotaCOFINS,
            valorCOFINS,
            aliquotaIPI,
            valorIPI,
            totalImpostos,
            baseCalculo + valorIPI);
    }

    // ── SPED Fiscal ───────────────────────────────────────────────────────────

    public async Task<byte[]> GerarSpedFiscalAsync(int ano, int mes, CancellationToken ct = default)
    {
        var inicio = new DateTime(ano, mes, 1, 0, 0, 0, DateTimeKind.Utc);
        var fim = inicio.AddMonths(1).AddTicks(-1);

        var notas = await _db.NotasFiscais
            .Include(n => n.Itens).ThenInclude(i => i.Produto)
            .AsNoTracking()
            .Where(n => n.Status == StatusNotaFiscal.Emitida
                     && n.DataEmissao >= inicio && n.DataEmissao <= fim)
            .ToListAsync(ct);

        var cnpj = _config["Fiscal:CNPJ"] ?? "00000000000000";
        var razaoSocial = _config["Fiscal:RazaoSocial"] ?? "EMPRESA";
        var ie = _config["Fiscal:IE"] ?? "";

        var sb = new StringBuilder();

        sb.AppendLine($"|0000|010|0|{inicio:ddMMyyyy}|{fim:ddMMyyyy}|{razaoSocial}|{cnpj}|{ie}|MG|3106200|1|{razaoSocial}|");
        sb.AppendLine("|0001|0|");
        sb.AppendLine("|0990|3|");
        sb.AppendLine("|C001|0|");

        var linhasC = 2;
        foreach (var nota in notas)
        {
            sb.AppendLine($"|C100|1|1|{nota.ChaveAcesso}|55|{nota.Serie}|{nota.NumeroNota}|" +
                $"{nota.DataEmissao:ddMMyyyy}|{nota.DataEmissao:ddMMyyyy}|" +
                $"{nota.NomeDestinatario ?? "CONSUMIDOR FINAL"}|{nota.CpfCnpjDestinatario ?? ""}|" +
                $"|{nota.ValorTotal:F2}|{nota.ValorProdutos:F2}|{nota.ValorDesconto:F2}|" +
                $"{nota.ValorICMS:F2}|{nota.ValorPIS:F2}|{nota.ValorCOFINS:F2}||");
            linhasC++;

            foreach (var (item, i) in nota.Itens.Select((it, idx) => (it, idx + 1)))
            {
                sb.AppendLine($"|C170|{i}|{item.ProdutoNome}|{item.NCM ?? ""}|" +
                    $"{item.Quantidade}|{item.Unidade}|{item.ValorUnitario:F2}|{item.ValorBruto:F2}|" +
                    $"{item.ValorDesconto:F2}|{item.CFOP ?? ""}|{item.CST ?? ""}|" +
                    $"{item.AliquotaICMS:F2}|{item.ValorICMS:F2}|" +
                    $"{item.AliquotaPIS:F2}|{item.ValorPIS:F2}|" +
                    $"{item.AliquotaCOFINS:F2}|{item.ValorCOFINS:F2}|");
                linhasC++;
            }

            sb.AppendLine($"|C190|{nota.Itens.FirstOrDefault()?.CST ?? ""}|{nota.Itens.FirstOrDefault()?.CFOP ?? ""}|" +
                $"{nota.Itens.FirstOrDefault()?.Unidade ?? "UN"}|" +
                $"{nota.ValorProdutos:F2}|{nota.ValorDesconto:F2}|{nota.ValorICMS:F2}||||||");
            linhasC++;
        }

        sb.AppendLine($"|C990|{linhasC + 1}|");
        sb.AppendLine("|9001|0|");
        sb.AppendLine($"|9900|0000|1|");
        sb.AppendLine($"|9900|0001|1|");
        sb.AppendLine($"|9900|C001|1|");
        sb.AppendLine($"|9900|C990|1|");
        sb.AppendLine($"|9900|9001|1|");
        sb.AppendLine($"|9900|9900|1|");
        sb.AppendLine("|9990|8|");
        sb.AppendLine("|9999|99|");

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    // ── Helpers Privados ──────────────────────────────────────────────────────

    private ItemNotaFiscal CriarItemNota(Produto produto, int quantidade, decimal precoUnitario, decimal descontoItem)
    {
        var baseCalculo = (precoUnitario * quantidade) - descontoItem;
        var aliquotaICMS = produto.Aliquota;
        var aliquotaPIS = AliquotaPIS;
        var aliquotaCOFINS = AliquotaCOFINS;

        return new ItemNotaFiscal
        {
            ProdutoId = produto.Id,
            ProdutoNome = produto.Nome,
            Unidade = produto.Unidade ?? "UN",
            Quantidade = quantidade,
            ValorUnitario = precoUnitario,
            ValorDesconto = descontoItem,
            ValorBruto = precoUnitario * quantidade,
            NCM = produto.NCM,
            CFOP = produto.CFOP,
            CST = produto.CST,
            Origem = produto.Origem,
            AliquotaICMS = aliquotaICMS,
            ValorICMS = Math.Round(baseCalculo * aliquotaICMS / 100, 2),
            AliquotaPIS = aliquotaPIS,
            ValorPIS = Math.Round(baseCalculo * aliquotaPIS / 100, 2),
            AliquotaCOFINS = aliquotaCOFINS,
            ValorCOFINS = Math.Round(baseCalculo * aliquotaCOFINS / 100, 2),
            AliquotaIPI = 0,
            ValorIPI = 0
        };
    }

    private async Task<string> ProximoNumeroNotaAsync(TipoNotaFiscal tipo, string serie, CancellationToken ct)
    {
        var ultimo = await _db.NotasFiscais
            .AsNoTracking()
            .Where(n => n.Tipo == tipo && n.Serie == serie && n.NumeroNota != null)
            .OrderByDescending(n => n.NumeroNota)
            .Select(n => n.NumeroNota)
            .FirstOrDefaultAsync(ct);

        var proximo = ultimo != null && long.TryParse(ultimo, out var num) ? num + 1 : 1;
        return proximo.ToString().PadLeft(9, '0');
    }

    private static string GerarChaveAcesso(NotaFiscal nota)
    {
        var data = nota.DataEmissao ?? DateTime.UtcNow;
        var modelo = nota.Tipo == TipoNotaFiscal.NFe ? "55" : "65";
        var random = new Random().Next(100000000, 999999999);
        return $"31{data:yyMM}00000000000000{modelo}{nota.Serie?.PadLeft(3, '0') ?? "001"}" +
               $"{nota.NumeroNota?.PadLeft(9, '0') ?? "000000001"}1{random:D9}";
    }

    private string GerarXml(NotaFiscal nota)
    {
        var cnpjEmitente = _config["Fiscal:CNPJ"] ?? "00000000000000";
        var razaoSocial = _config["Fiscal:RazaoSocial"] ?? "EMPRESA";
        var ie = _config["Fiscal:IE"] ?? "";
        var crt = _config["Fiscal:CRT"] ?? "1";

        var ns = "http://www.portalfiscal.inf.br/nfe";
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(XName.Get("nfeProc", ns),
                new XAttribute("versao", "4.00"),
                new XElement(XName.Get("NFe", ns),
                    new XElement(XName.Get("infNFe", ns),
                        new XAttribute("versao", "4.00"),
                        new XAttribute("Id", $"NFe{nota.ChaveAcesso}"),
                        new XElement(XName.Get("ide", ns),
                            new XElement(XName.Get("cUF", ns), "31"),
                            new XElement(XName.Get("cNF", ns), nota.ChaveAcesso?.Substring(35, 8) ?? "00000000"),
                            new XElement(XName.Get("natOp", ns), "VENDA"),
                            new XElement(XName.Get("mod", ns), nota.Tipo == TipoNotaFiscal.NFe ? "55" : "65"),
                            new XElement(XName.Get("serie", ns), nota.Serie ?? "001"),
                            new XElement(XName.Get("nNF", ns), nota.NumeroNota ?? "1"),
                            new XElement(XName.Get("dhEmi", ns), (nota.DataEmissao ?? DateTime.UtcNow).ToString("yyyy-MM-ddTHH:mm:sszzz")),
                            new XElement(XName.Get("tpNF", ns), "1"),
                            new XElement(XName.Get("idDest", ns), "1"),
                            new XElement(XName.Get("tpImp", ns), "1"),
                            new XElement(XName.Get("tpEmis", ns), "1"),
                            new XElement(XName.Get("tpAmb", ns), "2"),
                            new XElement(XName.Get("finNFe", ns), "1"),
                            new XElement(XName.Get("indFinal", ns), "1"),
                            new XElement(XName.Get("indPres", ns), "1")
                        ),
                        new XElement(XName.Get("emit", ns),
                            new XElement(XName.Get("CNPJ", ns), cnpjEmitente),
                            new XElement(XName.Get("xNome", ns), razaoSocial),
                            new XElement(XName.Get("IE", ns), ie),
                            new XElement(XName.Get("CRT", ns), crt)
                        ),
                        new XElement(XName.Get("dest", ns),
                            nota.CpfCnpjDestinatario?.Length == 11
                                ? new XElement(XName.Get("CPF", ns), nota.CpfCnpjDestinatario)
                                : nota.CpfCnpjDestinatario != null
                                    ? new XElement(XName.Get("CNPJ", ns), nota.CpfCnpjDestinatario.Replace(".", "").Replace("/", "").Replace("-", ""))
                                    : new XElement(XName.Get("CPF", ns), ""),
                            new XElement(XName.Get("xNome", ns), nota.NomeDestinatario ?? "CONSUMIDOR FINAL")
                        ),
                        new XElement(XName.Get("det", ns),
                            nota.Itens.Select((item, idx) =>
                                new XElement(XName.Get("det", ns),
                                    new XAttribute("nItem", idx + 1),
                                    new XElement(XName.Get("prod", ns),
                                        new XElement(XName.Get("cProd", ns), item.ProdutoId.ToString()),
                                        new XElement(XName.Get("xProd", ns), item.ProdutoNome),
                                        new XElement(XName.Get("NCM", ns), item.NCM ?? ""),
                                        new XElement(XName.Get("CFOP", ns), item.CFOP ?? "5102"),
                                        new XElement(XName.Get("uCom", ns), item.Unidade),
                                        new XElement(XName.Get("qCom", ns), item.Quantidade),
                                        new XElement(XName.Get("vUnCom", ns), item.ValorUnitario.ToString("F10")),
                                        new XElement(XName.Get("vProd", ns), item.ValorBruto.ToString("F2")),
                                        new XElement(XName.Get("vDesc", ns), item.ValorDesconto.ToString("F2"))
                                    ),
                                    new XElement(XName.Get("imposto", ns),
                                        new XElement(XName.Get("ICMS", ns),
                                            new XElement(XName.Get("ICMS00", ns),
                                                new XElement(XName.Get("orig", ns), item.Origem ?? "0"),
                                                new XElement(XName.Get("CST", ns), item.CST ?? "00"),
                                                new XElement(XName.Get("pICMS", ns), item.AliquotaICMS.ToString("F2")),
                                                new XElement(XName.Get("vICMS", ns), item.ValorICMS.ToString("F2"))
                                            )
                                        ),
                                        new XElement(XName.Get("PIS", ns),
                                            new XElement(XName.Get("PISAliq", ns),
                                                new XElement(XName.Get("CST", ns), "01"),
                                                new XElement(XName.Get("pPIS", ns), item.AliquotaPIS.ToString("F2")),
                                                new XElement(XName.Get("vPIS", ns), item.ValorPIS.ToString("F2"))
                                            )
                                        ),
                                        new XElement(XName.Get("COFINS", ns),
                                            new XElement(XName.Get("COFINSAliq", ns),
                                                new XElement(XName.Get("CST", ns), "01"),
                                                new XElement(XName.Get("pCOFINS", ns), item.AliquotaCOFINS.ToString("F2")),
                                                new XElement(XName.Get("vCOFINS", ns), item.ValorCOFINS.ToString("F2"))
                                            )
                                        )
                                    )
                                )
                            )
                        ),
                        new XElement(XName.Get("total", ns),
                            new XElement(XName.Get("ICMSTot", ns),
                                new XElement(XName.Get("vBC", ns), (nota.ValorProdutos - nota.ValorDesconto).ToString("F2")),
                                new XElement(XName.Get("vICMS", ns), nota.ValorICMS.ToString("F2")),
                                new XElement(XName.Get("vProd", ns), nota.ValorProdutos.ToString("F2")),
                                new XElement(XName.Get("vFrete", ns), nota.ValorFrete.ToString("F2")),
                                new XElement(XName.Get("vDesc", ns), nota.ValorDesconto.ToString("F2")),
                                new XElement(XName.Get("vPIS", ns), nota.ValorPIS.ToString("F2")),
                                new XElement(XName.Get("vCOFINS", ns), nota.ValorCOFINS.ToString("F2")),
                                new XElement(XName.Get("vNF", ns), nota.ValorTotal.ToString("F2"))
                            )
                        )
                    )
                )
            )
        );

        return doc.ToString();
    }

    // ── Mapper ────────────────────────────────────────────────────────────────

    private static NotaFiscalResponse MapNota(NotaFiscal n) => new(
        n.Id,
        n.Tipo,
        n.NumeroNota,
        n.Serie,
        n.ChaveAcesso,
        n.Status,
        n.VendaId,
        n.VendaPdvId,
        n.ClienteId,
        n.NomeDestinatario,
        n.CpfCnpjDestinatario,
        n.ValorProdutos,
        n.ValorFrete,
        n.ValorDesconto,
        n.ValorICMS,
        n.ValorPIS,
        n.ValorCOFINS,
        n.ValorIPI,
        n.ValorTotal,
        n.MensagemErro,
        n.DataEmissao,
        n.DataCancelamento,
        n.CriadoEm,
        n.Itens.Select(MapItem).ToList());

    private static ItemNotaFiscalResponse MapItem(ItemNotaFiscal i) => new(
        i.Id, i.ProdutoId, i.ProdutoNome, i.Unidade, i.Quantidade,
        i.ValorUnitario, i.ValorDesconto, i.ValorBruto,
        i.NCM, i.CFOP, i.CST,
        i.AliquotaICMS, i.ValorICMS,
        i.AliquotaPIS, i.ValorPIS,
        i.AliquotaCOFINS, i.ValorCOFINS,
        i.AliquotaIPI, i.ValorIPI);
}

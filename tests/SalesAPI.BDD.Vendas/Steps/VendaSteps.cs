using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using SalesAPI.Application.DTOs;

namespace SalesAPI.BDD.Vendas.Steps;

[Binding]
public class VendaSteps
{
    private readonly ScenarioContext _ctx;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public VendaSteps(ScenarioContext ctx)
    {
        _ctx = ctx;
        _client = ctx.Get<HttpClient>();
    }

    [When(@"o cliente ""(.*)"" compra (\d+) unidades do produto ""(.*)""")]
    public async Task QuandoClienteCompra(string nomeCliente, int quantidade, string nomeProduto)
    {
        var clienteId = (int)_ctx[$"cliente_id_{nomeCliente}"]!;
        var produtoId = (int)_ctx[$"produto_id_{nomeProduto}"]!;

        var req = new VendaRequest(
            ClienteId: clienteId,
            VendedorId: null,
            Desconto: 0m,
            Observacoes: null,
            NumeroParcelas: 1,
            PrimeiroVencimento: DateTime.Today.AddDays(30),
            Itens: [new ItemVendaRequest(produtoId, quantidade)]);

        var resp = await _client.PostAsJsonAsync("/api/vendas", req);
        _ctx["ultima_resposta"] = resp;
    }

    [When(@"eu tento vender (\d+) unidade(?:s)? do produto ""(.*)"" para o cliente de id (\d+)")]
    public async Task QuandoTentoVenderParaClienteInexistente(int quantidade, string nomeProduto, int clienteId)
    {
        var produtoId = (int)_ctx[$"produto_id_{nomeProduto}"]!;

        var req = new VendaRequest(
            ClienteId: clienteId,
            VendedorId: null,
            Desconto: 0m,
            Observacoes: null,
            NumeroParcelas: 1,
            PrimeiroVencimento: DateTime.Today.AddDays(30),
            Itens: [new ItemVendaRequest(produtoId, quantidade)]);

        var resp = await _client.PostAsJsonAsync("/api/vendas", req);
        _ctx["ultima_resposta"] = resp;
    }

    [Then(@"o total da venda deve ser (.*)")]
    public async Task EntaoTotalDeveSer(decimal totalEsperado)
    {
        var resp = _ctx.Get<HttpResponseMessage>("ultima_resposta");
        var venda = await resp.Content.ReadFromJsonAsync<VendaResponse>(JsonOpts);
        venda!.Total.Should().Be(totalEsperado);
    }
}

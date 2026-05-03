using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using SalesAPI.Application.DTOs;
using SalesAPI.Domain.Entities;

namespace SalesAPI.BDD.Produtos.Steps;

[Binding]
public class ProdutoSteps
{
    private readonly ScenarioContext _ctx;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProdutoSteps(ScenarioContext ctx)
    {
        _ctx = ctx;
        _client = ctx.Get<HttpClient>();
    }

    [When(@"eu cadastro o produto ""(.*)"" com preço (.*)")]
    public async Task QuandoCadastroProduto(string nome, decimal preco)
    {
        var req = new ProdutoRequest(
            Nome: nome,
            Descricao: null,
            CodigoInterno: null,
            SKU: null,
            CodigoBarras: null,
            CategoriaId: null,
            MarcaId: null,
            Unidade: "UN",
            PrecoCusto: preco,
            PrecoVenda: preco,
            EstoqueMinimo: 0,
            Status: StatusProduto.Ativo,
            ImagemUrl: null,
            NCM: null,
            CFOP: null,
            CST: null,
            Origem: null,
            Aliquota: 0m);

        var resp = await _client.PostAsJsonAsync("/api/produtos", req);
        _ctx["ultima_resposta"] = resp;

        if (resp.IsSuccessStatusCode)
        {
            var produto = await resp.Content.ReadFromJsonAsync<ProdutoResponse>(JsonOpts);
            _ctx[$"produto_id_{nome}"] = produto!.Id;
        }
    }

    [When(@"eu ajusto o estoque do produto ""(.*)"" para (\d+) unidades")]
    public async Task QuandoAjustoEstoque(string nome, int quantidade)
    {
        var produtoId = (int)_ctx[$"produto_id_{nome}"]!;
        var ajuste = new AjusteEstoqueMovRequest(
            ProdutoId: produtoId,
            NovaQuantidade: quantidade,
            Motivo: null);

        var resp = await _client.PostAsJsonAsync("/api/estoque/ajuste", ajuste);
        _ctx["ultima_resposta"] = resp;
    }

    [Then(@"o produto deve ter estoque (\d+)")]
    public async Task EntaoProdutoDeveTerEstoque(int esperado)
    {
        var resp = _ctx.Get<HttpResponseMessage>("ultima_resposta");
        var produto = await resp.Content.ReadFromJsonAsync<ProdutoResponse>(JsonOpts);
        produto!.QuantidadeEstoque.Should().Be(esperado);
    }
}

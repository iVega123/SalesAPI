using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using SalesAPI.Application.DTOs;
using SalesAPI.Domain.Entities;

namespace SalesAPI.BDD.Shared.Steps;

[Binding]
public class ProdutoSetupSteps
{
    private readonly ScenarioContext _ctx;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ProdutoSetupSteps(ScenarioContext ctx)
    {
        _ctx = ctx;
        _client = ctx.Get<HttpClient>();
    }

    [Given(@"que existe o produto ""(.*)"" com preço (.*)")]
    public async Task DadoQueExisteProduto(string nome, decimal preco)
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
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var produto = await resp.Content.ReadFromJsonAsync<ProdutoResponse>(JsonOpts);
        _ctx[$"produto_id_{nome}"] = produto!.Id;
    }

    [Given(@"que existe o produto ""(.*)"" com preço (.*) e estoque (\d+)")]
    public async Task DadoQueExisteProdutoComEstoque(string nome, decimal preco, int estoque)
    {
        await DadoQueExisteProduto(nome, preco);
        var produtoId = (int)_ctx[$"produto_id_{nome}"]!;

        var ajuste = new AjusteEstoqueMovRequest(
            ProdutoId: produtoId,
            NovaQuantidade: estoque,
            Motivo: "Setup de cenário BDD");

        var resp = await _client.PostAsJsonAsync("/api/estoque/ajuste", ajuste);
        resp.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Then(@"o produto ""(.*)"" deve ter estoque (\d+)")]
    public async Task EntaoProdutoNomeadoDeveTerEstoque(string nome, int esperado)
    {
        var produtoId = (int)_ctx[$"produto_id_{nome}"]!;
        var resp = await _client.GetAsync($"/api/estoque/{produtoId}");
        resp.StatusCode.Should().Be(HttpStatusCode.OK);

        var estoque = await resp.Content.ReadFromJsonAsync<EstoqueResponse>(JsonOpts);
        estoque!.Quantidade.Should().Be(esperado);
    }
}

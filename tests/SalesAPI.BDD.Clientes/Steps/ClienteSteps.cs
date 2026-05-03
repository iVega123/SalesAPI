using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using SalesAPI.Application.DTOs;
using SalesAPI.Domain.Entities;

namespace SalesAPI.BDD.Clientes.Steps;

[Binding]
public class ClienteSteps
{
    private readonly ScenarioContext _ctx;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ClienteSteps(ScenarioContext ctx)
    {
        _ctx = ctx;
        _client = ctx.Get<HttpClient>();
    }

    [When(@"eu cadastro o cliente ""(.*)"" com email ""(.*)"" e cpf ""(.*)""")]
    public async Task QuandoCadastroCliente(string nome, string email, string cpf)
    {
        var req = new ClienteRequest(
            Nome: nome,
            TipoPessoa: TipoPessoa.Fisica,
            CPF: cpf,
            CNPJ: null,
            Email: email,
            Telefone: null,
            DataNascimento: null,
            LimiteCredito: 0m,
            Observacoes: null);

        var resp = await _client.PostAsJsonAsync("/api/clientes", req);
        _ctx["ultima_resposta"] = resp;
    }

    [When(@"eu listo os clientes")]
    public async Task QuandoListoClientes()
    {
        var resp = await _client.GetAsync("/api/clientes");
        _ctx["ultima_resposta"] = resp;
    }

    [Then(@"o cliente cadastrado deve ter nome ""(.*)""")]
    public async Task EntaoClienteDeveTerNome(string nomeEsperado)
    {
        var resp = _ctx.Get<HttpResponseMessage>("ultima_resposta");
        var cliente = await resp.Content.ReadFromJsonAsync<ClienteResponse>(JsonOpts);
        cliente!.Nome.Should().Be(nomeEsperado);
    }

    [Then(@"a lista deve conter (\d+) clientes")]
    public async Task EntaoListaDeveConterClientes(int quantidade)
    {
        var resp = _ctx.Get<HttpResponseMessage>("ultima_resposta");
        var clientes = await resp.Content.ReadFromJsonAsync<List<ClienteResponse>>(JsonOpts);
        clientes!.Should().HaveCount(quantidade);
    }
}

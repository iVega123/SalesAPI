using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Reqnroll;
using SalesAPI.Application.DTOs;
using SalesAPI.Domain.Entities;

namespace SalesAPI.BDD.Shared.Steps;

[Binding]
public class ClienteSetupSteps
{
    private readonly ScenarioContext _ctx;
    private readonly HttpClient _client;
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public ClienteSetupSteps(ScenarioContext ctx)
    {
        _ctx = ctx;
        _client = ctx.Get<HttpClient>();
    }

    [Given(@"que existe o cliente ""(.*)"" com email ""(.*)"" e cpf ""(.*)""")]
    public async Task DadoQueExisteCliente(string nome, string email, string cpf)
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
        resp.StatusCode.Should().Be(HttpStatusCode.Created);

        var cliente = await resp.Content.ReadFromJsonAsync<ClienteResponse>(JsonOpts);
        _ctx[$"cliente_id_{nome}"] = cliente!.Id;
    }
}

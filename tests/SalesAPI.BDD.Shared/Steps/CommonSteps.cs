using FluentAssertions;
using Reqnroll;

namespace SalesAPI.BDD.Shared.Steps;

[Binding]
public class CommonSteps
{
    private readonly ScenarioContext _ctx;

    public CommonSteps(ScenarioContext ctx) => _ctx = ctx;

    [Then(@"a resposta deve ser status (\d+)")]
    public void EntaoRespostaDeveSerStatus(int statusCode)
    {
        var resp = _ctx.Get<HttpResponseMessage>("ultima_resposta");
        ((int)resp.StatusCode).Should().Be(statusCode);
    }

    [Then(@"a mensagem de erro deve conter ""(.*)""")]
    public async Task EntaoMensagemErroDeveConter(string trecho)
    {
        var resp = _ctx.Get<HttpResponseMessage>("ultima_resposta");
        var corpo = await resp.Content.ReadAsStringAsync();
        corpo.Should().Contain(trecho);
    }
}

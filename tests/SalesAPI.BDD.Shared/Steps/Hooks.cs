using Reqnroll;
using SalesAPI.BDD.Shared.Support;

namespace SalesAPI.BDD.Shared.Steps;

[Binding]
public class Hooks
{
    private readonly ScenarioContext _ctx;
    private TestWebApplicationFactory? _factory;

    public Hooks(ScenarioContext ctx) => _ctx = ctx;

    [BeforeScenario]
    public void Antes()
    {
        _factory = new TestWebApplicationFactory();
        _ctx.Set(_factory);
        _ctx.Set(_factory.CreateClient());
    }

    [AfterScenario]
    public void Depois()
    {
        _ctx.Get<HttpClient>()?.Dispose();
        _factory?.Dispose();
    }
}

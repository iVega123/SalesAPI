using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SalesAPI.Application.DTOs;
using Xunit;
using SalesAPI.Application.Interfaces;
using SalesAPI.Application.Services;
using SalesAPI.Domain.Entities;
using SalesAPI.Infrastructure.Data;

namespace SalesAPI.UnitTests;

public class VendaServiceTests
{
    private static AppDbContext NovoContexto()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static VendaService NovoService(IAppDbContext db)
        => new(db, new EstoqueService(db));

    private static async Task SemearAsync(AppDbContext db)
    {
        var cliente = new Cliente { Nome = "João", Email = "joao@x.com", CPF = "12345678900" };
        var produto = new Produto
        {
            Nome = "Caneta",
            Descricao = "Caneta azul",
            PrecoVenda = 5.50m,
            Unidade = "UN",
            Estoque = new Estoque { Quantidade = 10 }
        };
        db.Clientes.Add(cliente);
        db.Produtos.Add(produto);
        await db.SaveChangesAsync();
    }

    private static VendaRequest PedidoSimples(int clienteId, int produtoId, int quantidade)
        => new(
            ClienteId: clienteId,
            VendedorId: null,
            Desconto: 0m,
            Observacoes: null,
            NumeroParcelas: 1,
            PrimeiroVencimento: DateTime.Today.AddDays(30),
            Itens: [new ItemVendaRequest(produtoId, quantidade)]);

    [Fact]
    public async Task CriarAsync_DeveRegistrarVenda_E_ReduzirEstoque()
    {
        await using var db = NovoContexto();
        await SemearAsync(db);
        var service = NovoService(db);

        var venda = await service.CriarAsync(PedidoSimples(1, 1, 3));

        venda.Should().NotBeNull();
        venda.Total.Should().Be(16.50m);
        venda.Itens.Should().HaveCount(1);
        venda.Itens[0].Quantidade.Should().Be(3);
        venda.Itens[0].PrecoUnitario.Should().Be(5.50m);

        var estoque = await db.Estoques.AsNoTracking().FirstAsync();
        estoque.Quantidade.Should().Be(7);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarErro_QuandoEstoqueInsuficiente()
    {
        await using var db = NovoContexto();
        await SemearAsync(db);
        var service = NovoService(db);

        var act = async () => await service.CriarAsync(PedidoSimples(1, 1, 99));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Estoque insuficiente*");

        var estoque = await db.Estoques.AsNoTracking().FirstAsync();
        estoque.Quantidade.Should().Be(10);
    }

    [Fact]
    public async Task CriarAsync_DeveLancarErro_QuandoClienteNaoExiste()
    {
        await using var db = NovoContexto();
        await SemearAsync(db);
        var service = NovoService(db);

        var act = async () => await service.CriarAsync(PedidoSimples(999, 1, 1));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Cliente 999 não encontrado*");
    }

    [Fact]
    public async Task CriarAsync_DeveLancarErro_QuandoProdutoNaoExiste()
    {
        await using var db = NovoContexto();
        await SemearAsync(db);
        var service = NovoService(db);

        var act = async () => await service.CriarAsync(PedidoSimples(1, 42, 1));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Produto*não encontrado*");
    }

    [Fact]
    public async Task CriarAsync_DeveAgrupar_QuandoMesmoProdutoVemDuplicado()
    {
        await using var db = NovoContexto();
        await SemearAsync(db);
        var service = NovoService(db);
        var req = new VendaRequest(
            ClienteId: 1,
            VendedorId: null,
            Desconto: 0m,
            Observacoes: null,
            NumeroParcelas: 1,
            PrimeiroVencimento: DateTime.Today.AddDays(30),
            Itens:
            [
                new ItemVendaRequest(1, 2),
                new ItemVendaRequest(1, 3)
            ]);

        var venda = await service.CriarAsync(req);

        venda.Itens.Should().HaveCount(1);
        venda.Itens[0].Quantidade.Should().Be(5);
        venda.Total.Should().Be(27.50m);

        var estoque = await db.Estoques.AsNoTracking().FirstAsync();
        estoque.Quantidade.Should().Be(5);
    }

    [Fact]
    public async Task ObterAsync_DeveRetornarNull_QuandoVendaNaoExiste()
    {
        await using var db = NovoContexto();
        var service = NovoService(db);

        var resultado = await service.ObterAsync(123);

        resultado.Should().BeNull();
    }
}

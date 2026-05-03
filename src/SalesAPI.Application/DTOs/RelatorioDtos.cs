namespace SalesAPI.Application.DTOs;

public record RelatorioFiltro(DateTime Inicio, DateTime Fim);

public record RelatorioVendasResponse(
    DateTime Inicio,
    DateTime Fim,
    int TotalVendas,
    decimal ReceitaBruta,
    decimal TotalDescontos,
    decimal ReceitaLiquida,
    int TotalItens,
    List<VendasDiaItem> PorDia);

public record VendasDiaItem(DateTime Data, int Vendas, decimal Total);

public record VendasPorProdutoItem(
    int ProdutoId,
    string ProdutoNome,
    int QuantidadeVendida,
    decimal ReceitaTotal,
    decimal CustoTotal,
    decimal LucroBruto);

public record VendasPorVendedorItem(
    int VendedorId,
    string VendedorNome,
    int TotalVendas,
    decimal ReceitaTotal,
    decimal TotalComissoes);

public record VendasPorClienteItem(
    int ClienteId,
    string ClienteNome,
    int TotalVendas,
    decimal TotalGasto,
    DateTime UltimaCompra);

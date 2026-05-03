using System.ComponentModel.DataAnnotations;
using SalesAPI.Domain.Entities;

namespace SalesAPI.Application.DTOs;

public record EstoqueAjusteRequest([Range(0, int.MaxValue)] int Quantidade);
public record EstoqueResponse(int ProdutoId, string ProdutoNome, int Quantidade, DateTime AtualizadoEm);

public record EntradaEstoqueRequest(
    [Required] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade,
    [MaxLength(500)] string? Motivo,
    int? CompraId);

public record SaidaEstoqueRequest(
    [Required] int ProdutoId,
    [Range(1, int.MaxValue)] int Quantidade,
    [MaxLength(500)] string? Motivo);

public record AjusteEstoqueMovRequest(
    [Required] int ProdutoId,
    [Range(0, int.MaxValue)] int NovaQuantidade,
    [MaxLength(500)] string? Motivo);

public record MovimentacaoEstoqueResponse(
    int Id,
    int ProdutoId,
    string ProdutoNome,
    TipoMovimentacao Tipo,
    int Quantidade,
    int QuantidadeAnterior,
    int QuantidadeResultante,
    string? Motivo,
    DateTime CriadoEm);

public record AlertaEstoqueResponse(
    int ProdutoId,
    string ProdutoNome,
    int QuantidadeAtual,
    int EstoqueMinimo);

public record IniciarInventarioRequest(
    [MaxLength(300)] string? Descricao,
    [Required] int UsuarioId);

public record RegistrarAjusteRequest(
    [Required] int ProdutoId,
    [Range(0, int.MaxValue)] int QuantidadeContada,
    [MaxLength(500)] string? Motivo);

public record InventarioResponse(
    int Id,
    string? Descricao,
    DateTime IniciadoEm,
    DateTime? FinalizadoEm,
    bool Finalizado,
    List<AjusteEstoqueResponse> Ajustes);

public record AjusteEstoqueResponse(
    int ProdutoId,
    string ProdutoNome,
    int QuantidadeContada,
    int QuantidadeSistema,
    int Diferenca,
    string? Motivo);

public record EstoqueAtualItem(
    int ProdutoId,
    string ProdutoNome,
    string? SKU,
    string? Categoria,
    int Quantidade,
    int EstoqueMinimo,
    bool AbaixoMinimo,
    decimal PrecoCusto,
    decimal PrecoVenda,
    decimal ValorEstoque);

public record GiroEstoqueItem(
    int ProdutoId,
    string ProdutoNome,
    int TotalEntradas,
    int TotalSaidas,
    int EstoqueAtual,
    decimal GiroMedio);

public record CurvaAbcItem(
    int Posicao,
    int ProdutoId,
    string ProdutoNome,
    decimal ReceitaTotal,
    decimal PercentualReceita,
    decimal PercentualAcumulado,
    ClassificacaoAbc Classificacao);

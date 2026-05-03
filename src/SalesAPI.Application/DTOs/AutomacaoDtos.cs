using System.ComponentModel.DataAnnotations;

namespace SalesAPI.Application.DTOs;

public record ConfigurarAlertaEstoqueRequest(bool Ativo, [EmailAddress] string? EmailDestino);

public record JobStatusResponse(string JobId, string Estado, string? Mensagem, DateTime? ProximaExecucao);

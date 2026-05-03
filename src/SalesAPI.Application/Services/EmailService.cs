using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace SalesAPI.Application.Services;

public interface IEmailService
{
    Task EnviarAsync(string destinatario, string assunto, string corpo, CancellationToken ct = default);
    Task EnviarComAnexoAsync(string destinatario, string assunto, string corpo, byte[] anexo, string nomeArquivo, string tipoMime, CancellationToken ct = default);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task EnviarAsync(string destinatario, string assunto, string corpo, CancellationToken ct = default)
    {
        await EnviarInternoAsync(destinatario, assunto, corpo, null, null, null, ct);
    }

    public async Task EnviarComAnexoAsync(string destinatario, string assunto, string corpo, byte[] anexo, string nomeArquivo, string tipoMime, CancellationToken ct = default)
    {
        await EnviarInternoAsync(destinatario, assunto, corpo, anexo, nomeArquivo, tipoMime, ct);
    }

    private async Task EnviarInternoAsync(string destinatario, string assunto, string corpo, byte[]? anexo, string? nomeArquivo, string? tipoMime, CancellationToken ct)
    {
        var host = _config["Email:Host"] ?? "smtp.gmail.com";
        var port = int.Parse(_config["Email:Port"] ?? "587");
        var usuario = _config["Email:Usuario"] ?? "";
        var senha = _config["Email:Senha"] ?? "";
        var remetente = _config["Email:Remetente"] ?? usuario;
        var nomeRemetente = _config["Email:NomeRemetente"] ?? "SalesAPI";

        if (string.IsNullOrWhiteSpace(usuario) || string.IsNullOrWhiteSpace(senha))
        {
            _logger.LogWarning("Email não configurado. Pulando envio para {Destinatario}: {Assunto}", destinatario, assunto);
            return;
        }

        try
        {
            var mensagem = new MimeMessage();
            mensagem.From.Add(new MailboxAddress(nomeRemetente, remetente));
            mensagem.To.Add(MailboxAddress.Parse(destinatario));
            mensagem.Subject = assunto;

            var builder = new BodyBuilder { HtmlBody = corpo };

            if (anexo != null && nomeArquivo != null && tipoMime != null)
                builder.Attachments.Add(nomeArquivo, anexo, ContentType.Parse(tipoMime));

            mensagem.Body = builder.ToMessageBody();

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls, ct);
            await client.AuthenticateAsync(usuario, senha, ct);
            await client.SendAsync(mensagem, ct);
            await client.DisconnectAsync(true, ct);

            _logger.LogInformation("Email enviado para {Destinatario}: {Assunto}", destinatario, assunto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao enviar email para {Destinatario}: {Assunto}", destinatario, assunto);
            throw;
        }
    }
}

namespace ExpenseHub.Api.Configuration;

/// <summary>
/// Opcoes de geracao e validacao do token JWT, vinculadas a secao "Jwt" da
/// configuracao. A chave de assinatura nunca e versionada no repositorio.
/// </summary>
internal sealed class JwtOptions
{
    /// <summary>Nome da secao de configuracao.</summary>
    internal const string SectionName = "Jwt";

    /// <summary>Emissor do token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audiencia do token.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>Chave simetrica de assinatura (segredo).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Validade do token em minutos.</summary>
    public int ExpiresMinutes { get; set; } = 60;
}

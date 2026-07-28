using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;

namespace tms_template_net8.Auth.Services;

/// <summary>
/// Result of validating an access token beyond a simple valid/invalid split.
/// </summary>
public enum AuthTokenValidationKind
{
    /// <summary>Token is valid and within lifetime.</summary>
    Valid,

    /// <summary>Lifetime validation failed (typically expired).</summary>
    Expired,

    /// <summary>Malformed, bad signature, wrong issuer/audience, or other error.</summary>
    Invalid
}

/// <summary>
/// Validates JWT access tokens issued by the standalone auth service (same signing key / issuer / audience as configured).
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Validates the access token and distinguishes expiry from other failures (bad signature, wrong issuer, etc.).
    /// </summary>
    (ClaimsPrincipal? principal, AuthTokenValidationKind kind) ValidateTokenWithKind(string token);
}

/// <summary>
/// Validates JWTs using the configured RSA public (or private) key and Jwt issuer/audience settings.
/// </summary>
public class TokenService : ITokenService
{
    private readonly RSA _rsa;
    private readonly IConfiguration _configuration;

    public TokenService(RSA rsa, IConfiguration configuration)
    {
        _rsa = rsa;
        _configuration = configuration;
    }

    public (ClaimsPrincipal? principal, AuthTokenValidationKind kind) ValidateTokenWithKind(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, GetValidationParameters(), out var validatedToken);
            if (validatedToken is not JwtSecurityToken)
                return (null, AuthTokenValidationKind.Invalid);

            return (principal, AuthTokenValidationKind.Valid);
        }
        catch (SecurityTokenExpiredException)
        {
            return (null, AuthTokenValidationKind.Expired);
        }
        catch (SecurityTokenNotYetValidException)
        {
            return (null, AuthTokenValidationKind.Invalid);
        }
        catch (Exception)
        {
            return (null, AuthTokenValidationKind.Invalid);
        }
    }

    private TokenValidationParameters GetValidationParameters()
    {
        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new RsaSecurityKey(_rsa),
            ValidIssuer = _configuration["Auth:Issuer"] ?? "authapi",
            ValidAudience = _configuration["Auth:Audience"] ?? "authapi-client",
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            CryptoProviderFactory = new CryptoProviderFactory { CacheSignatureProviders = false }
        };
    }
}

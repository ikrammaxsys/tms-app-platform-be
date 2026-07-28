using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;

namespace tms_template_net8.Auth.Security;

public static class RsaKeyLoader
{
    private const string PublicPemApiPath = "/api/index/public.pem";

    /// <summary>
    /// Downloads the RSA public PEM from the auth API and writes it to <c>Jwt:RsaKeyPath</c>.
    /// Skips when <c>Auth:BaseUrl</c> or <c>Jwt:RsaKeyPath</c> is missing. On failure, keeps any existing local PEM.
    /// </summary>
    public static async Task SyncPublicPemFromAuthAsync(IConfiguration config, IWebHostEnvironment env)
    {
        var authBaseUrl = config["Auth:BaseUrl"]?.Trim();
        var pemPath = config["Jwt:RsaKeyPath"]?.Trim();
        if (string.IsNullOrWhiteSpace(authBaseUrl) || string.IsNullOrWhiteSpace(pemPath))
            return;

        var fullPemPath = ResolvePemFilePath(pemPath, env);
        var requestUrl = authBaseUrl.TrimEnd('/') + PublicPemApiPath;

        try
        {
            using var client = new HttpClient();
            var pemContent = await client.GetStringAsync(requestUrl);
            if (string.IsNullOrWhiteSpace(pemContent))
                return;

            var directory = Path.GetDirectoryName(fullPemPath);
            if (!string.IsNullOrWhiteSpace(directory))
                Directory.CreateDirectory(directory);

            await File.WriteAllTextAsync(fullPemPath, pemContent);
        }
        catch
        {
            // Keep existing local PEM as fallback when remote fetch fails.
        }
    }

    /// <summary>
    /// Load RSA key from a PEM file (Jwt:RsaKeyPath).
    /// </summary>
    public static RSA LoadRsaKey(IConfiguration config, IWebHostEnvironment env)
    {
        var path = config["Jwt:RsaKeyPath"];
        if (string.IsNullOrWhiteSpace(path))
            throw new InvalidOperationException(
                "JWT RS256 requires Jwt:RsaKeyPath (path to PEM file).");

        var fullPath = ResolvePemFilePath(path, env);
        var rsaKeyPem = File.ReadAllText(fullPath);
        var rsaKey = RSA.Create();
        rsaKey.ImportFromPem(rsaKeyPem);
        return rsaKey;
    }

    private static string ResolvePemFilePath(string path, IWebHostEnvironment env) =>
        Path.IsPathRooted(path) ? path : Path.Combine(env.ContentRootPath ?? ".", path);
}

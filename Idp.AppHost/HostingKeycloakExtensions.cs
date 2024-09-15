using System.Diagnostics;
using System.IO.Hashing;
using System.Text;

namespace Aspire.Hosting;

/// <summary>
/// Original src code:
/// https://github.com/dotnet/aspire-samples/blob/b741f5e78a86539bc9ab12cd7f4a5afea7aa54c4/samples/Keycloak/Keycloak.AppHost/HostingExtensions.cs
/// </summary>
public static class HostingKeycloakExtensions
{
    /// <summary>
    /// Injects the ASP.NET Core HTTPS developer certificate into the resource via the specified environment variables when
    /// <paramref name="builder"/>.<see cref="IResourceBuilder{T}.ApplicationBuilder">ApplicationBuilder</see>.
    /// <see cref="IDistributedApplicationBuilder.ExecutionContext">ExecutionContext</see>.<see cref="DistributedApplicationExecutionContext.IsRunMode">IsRunMode</see><c> == true</c>.<br/>
    /// If the resource is a <see cref="ContainerResource"/>, the certificate files will be bind mounted into the container.
    /// </summary>
    /// <remarks>
    /// This method <strong>does not</strong> configure an HTTPS endpoint on the resource. Use <see cref="ResourceBuilderExtensions.WithHttpsEndpoint{TResource}"/> to configure an HTTPS endpoint.
    /// </remarks>
    public static IResourceBuilder<TResource> RunKeycloakWithHttpsDevCertificate<TResource>(this IResourceBuilder<TResource> builder, string certFileEnv, string certKeyFileEnv)
        where TResource : IResourceWithEnvironment
    {
        const string DEV_CERT_DIR = "/dev-certs";

        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Export the ASP.NET Core HTTPS development certificate & private key to PEM files, bind mount them into the container
            // and configure it to use them via the specified environment variables.
            var (certPath, _) = ExportKeycloakDevCertificate(builder.ApplicationBuilder);
            var bindSource = Path.GetDirectoryName(certPath) ?? throw new UnreachableException();

            if (builder.Resource is ContainerResource containerResource)
            {
                builder.ApplicationBuilder.CreateResourceBuilder(containerResource)
                    .WithBindMount(bindSource, DEV_CERT_DIR, isReadOnly: true);
            }

            builder
                .WithEnvironment(certFileEnv, $"{DEV_CERT_DIR}/dev-cert.pem")
                .WithEnvironment(certKeyFileEnv, $"{DEV_CERT_DIR}/dev-cert.key");
        }

        return builder;
    }

    /// <summary>
    /// Configures the Keycloak container to use the ASP.NET Core HTTPS development certificate created by <c>dotnet dev-certs</c> when
    /// <paramref name="builder"/><c>.ExecutionContext.IsRunMode == true</c>.
    /// </summary>
    /// <remarks>
    /// See <see href="https://learn.microsoft.com/dotnet/core/tools/dotnet-dev-certs">https://learn.microsoft.com/dotnet/core/tools/dotnet-dev-certs</see>
    /// for more information on the <c>dotnet dev-certs</c> tool.<br/>
    /// See <see href="https://learn.microsoft.com/aspnet/core/security/enforcing-ssl#trust-the-aspnet-core-https-development-certificate-on-windows-and-macos">
    /// https://learn.microsoft.com/aspnet/core/security/enforcing-ssl</see>
    /// for more information on the ASP.NET Core HTTPS development certificate.
    /// </remarks>
    public static IResourceBuilder<KeycloakResource> RunKeycloakWithHttpsDevCertificate(this IResourceBuilder<KeycloakResource> builder, int port = 8081, int targetPort = 8443)
    {
        if (builder.ApplicationBuilder.ExecutionContext.IsRunMode)
        {
            // Mount the ASP.NET Core HTTPS development certificate in the Keycloak container and configure Keycloak to it
            // via the KC_HTTPS_CERTIFICATE_FILE and KC_HTTPS_CERTIFICATE_KEY_FILE environment variables.
            builder
                .RunKeycloakWithHttpsDevCertificate("KC_HTTPS_CERTIFICATE_FILE", "KC_HTTPS_CERTIFICATE_KEY_FILE")
                .WithHttpsEndpoint(port: port, targetPort: targetPort)
                .WithEnvironment("KC_HOSTNAME", "localhost")
                // Without disabling HTTP/2 you can hit HTTP 431 Header too large errors in Keycloak.
                // Related issues:
                // https://github.com/keycloak/keycloak/discussions/10236
                // https://github.com/keycloak/keycloak/issues/13933
                // https://github.com/quarkusio/quarkus/issues/33692
                .WithEnvironment("QUARKUS_HTTP_HTTP2", "false");
        }

        return builder;
    }

    private static (string, string) ExportKeycloakDevCertificate(IDistributedApplicationBuilder builder)
    {
        // Exports the ASP.NET Core HTTPS development certificate & private key to PEM files using 'dotnet dev-certs https' to a temporary
        // directory and returns the path.
        // TODO: Check if we're running on a platform that already has the cert and key exported to a file (e.g. macOS) and just use those intead.
        var appNameHashBytes = XxHash64.Hash(Encoding.Unicode.GetBytes(builder.Environment.ApplicationName).AsSpan());
        var appNameHash = BitConverter.ToString(appNameHashBytes).Replace("-", "").ToLowerInvariant();
        var tempDir = Path.Combine(Path.GetTempPath(), $"aspire.{appNameHash}");
        var certExportPath = Path.Combine(tempDir, "dev-cert.pem");
        var certKeyExportPath = Path.Combine(tempDir, "dev-cert.key");

        if (File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            // Certificate already exported, return the path.
            return (certExportPath, certKeyExportPath);
        }
        else if (Directory.Exists(tempDir))
        {
            Directory.Delete(tempDir, recursive: true);
        }

        Directory.CreateDirectory(tempDir);

        var exportProcess = Process.Start("dotnet", $"dev-certs https --export-path \"{certExportPath}\" --format Pem --no-password");

        var exited = exportProcess.WaitForExit(TimeSpan.FromSeconds(5));
        if (exited && File.Exists(certExportPath) && File.Exists(certKeyExportPath))
        {
            return (certExportPath, certKeyExportPath);
        }
        else if (exportProcess.HasExited && exportProcess.ExitCode != 0)
        {
            throw new InvalidOperationException($"HTTPS dev certificate export failed with exit code {exportProcess.ExitCode}");
        }
        else if (!exportProcess.HasExited)
        {
            exportProcess.Kill(true);
            throw new InvalidOperationException("HTTPS dev certificate export timed out");
        }

        throw new InvalidOperationException("HTTPS dev certificate export failed for an unknown reason");
    }
}
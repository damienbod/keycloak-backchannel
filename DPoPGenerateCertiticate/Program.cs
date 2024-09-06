using CertificateManager;
using CertificateManager.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace DPoPGenerateCertiticate;

class Program
{
    static CreateCertificates? _cc;
    static ImportExportCertificate? _iec;
    static void Main(string[] args)
    {
        var sp = new ServiceCollection()
           .AddCertificateManager()
           .BuildServiceProvider();

        _cc = sp.GetService<CreateCertificates>()!;
        _iec = sp.GetService<ImportExportCertificate>()!;

        var rsaCert = CreateRsaCertificate("localhost", 10);
        var ecdsaCert = CreateECDsaCertificate("localhost", 10);

        var pemPublicRsaKey = _iec.PemExportPublicKeyCertificate(rsaCert);
        File.WriteAllText("rsa256-public.pem", pemPublicRsaKey);

        using (RSA? rsa = rsaCert.GetRSAPrivateKey())
        {
            var pemPrivateRsaKey = rsa!.ExportRSAPrivateKeyPem();
            File.WriteAllText("rsa256-private.pem", pemPrivateRsaKey);
        }

        var pemPublicKey = _iec.PemExportPublicKeyCertificate(ecdsaCert);
        File.WriteAllText("ecdsa384-public.pem", pemPublicKey);

        using (ECDsa? ecdsa = ecdsaCert.GetECDsaPrivateKey())
        {
            var pemPrivateKey = ecdsa!.ExportECPrivateKeyPem();
            File.WriteAllText("ecdsa384-private.pem", pemPrivateKey);
        }

        Console.WriteLine("created, keys are in the bin folder");
    }

    public static X509Certificate2 CreateRsaCertificate(string dnsName, int validityPeriodInYears)
    {
        var basicConstraints = new BasicConstraints
        {
            CertificateAuthority = false,
            HasPathLengthConstraint = false,
            PathLengthConstraint = 0,
            Critical = false
        };

        var subjectAlternativeName = new SubjectAlternativeName
        {
            DnsName = new List<string>
            {
                dnsName,
            }
        };

        var x509KeyUsageFlags = X509KeyUsageFlags.DigitalSignature;

        // only if certification authentication is used
        var enhancedKeyUsages = new OidCollection
        {
            OidLookup.ClientAuthentication,
            OidLookup.ServerAuthentication 
            // OidLookup.CodeSigning,
            // OidLookup.SecureEmail,
            // OidLookup.TimeStamping  
        };

        var certificate = _cc!.NewRsaSelfSignedCertificate(
            new DistinguishedName { CommonName = dnsName },
            basicConstraints,
            new ValidityPeriod
            {
                ValidFrom = DateTimeOffset.UtcNow,
                ValidTo = DateTimeOffset.UtcNow.AddYears(validityPeriodInYears)
            },
            subjectAlternativeName,
            enhancedKeyUsages,
            x509KeyUsageFlags,
            new RsaConfiguration
            {
                KeySize = 2048,
                HashAlgorithmName = HashAlgorithmName.SHA256
            });

        return certificate;
    }

    public static X509Certificate2 CreateECDsaCertificate(string dnsName, int validityPeriodInYears)
    {
        var basicConstraints = new BasicConstraints
        {
            CertificateAuthority = false,
            HasPathLengthConstraint = false,
            PathLengthConstraint = 0,
            Critical = false
        };

        var subjectAlternativeName = new SubjectAlternativeName
        {
            DnsName = new List<string>
            {
                dnsName,
            }
        };

        var x509KeyUsageFlags = X509KeyUsageFlags.DigitalSignature;

        // only if certification authentication is used
        var enhancedKeyUsages = new OidCollection {
            OidLookup.ClientAuthentication,
            OidLookup.ServerAuthentication 
            // OidLookup.CodeSigning,
            // OidLookup.SecureEmail,
            // OidLookup.TimeStamping 
        };

        var certificate = _cc!.NewECDsaSelfSignedCertificate(
            new DistinguishedName { CommonName = dnsName },
            basicConstraints,
            new ValidityPeriod
            {
                ValidFrom = DateTimeOffset.UtcNow,
                ValidTo = DateTimeOffset.UtcNow.AddYears(validityPeriodInYears)
            },
            subjectAlternativeName,
            enhancedKeyUsages,
            x509KeyUsageFlags,
            new ECDsaConfiguration
            {
                KeySize = 384,
                HashAlgorithmName = HashAlgorithmName.SHA384
            });

        return certificate;
    }
}

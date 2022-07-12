using LetsEncrypt.Logic.Providers.CertificateStores;
using LetsEncrypt.Logic.Providers.ChallengeResponders;
using LetsEncrypt.Logic.Providers.TargetResources;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.Logic.Config;

public interface IRenewalOptionLoader
{
    /// <summary>
    /// Given a configuration object parses the challenge responder section based on its type and returns a statically typed version.
    /// </summary>
    Task<IChallengeResponder> ParseChallengeResponderAsync(CertificateRenewalOptions cfg, CancellationToken cancellationToken);

    /// <summary>
    /// Given a configuration object parses the certificate store section based on its type and returns a statically typed version.
    /// </summary>
    ICertificateStore ParseCertificateStore(CertificateRenewalOptions cfg);

    /// <summary>
    /// Given a configuration object parses the target resource section based on its type and returns a statically typed version.
    /// </summary>
    ITargetResource ParseTargetResource(CertificateRenewalOptions cfg);
}

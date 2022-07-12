using LetsEncrypt.Func.Config;
using LetsEncrypt.Logic;
using LetsEncrypt.Logic.Config;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.Func.Functions;

public class AutoRenewal
{
    private readonly IRenewalService _renewalService;
    private readonly ILogger _logger;
    private readonly IConfigurationLoader _configurationLoader;

    public AutoRenewal(
        IConfigurationLoader configurationLoader,
        IRenewalService renewalService,
        ILogger<AutoRenewal> logger)
    {
        _configurationLoader = configurationLoader;
        _renewalService = renewalService;
        _logger = logger;
    }

    /// <summary>
    /// Wrapper function that allows manual execution via http with optional override parameters.
    /// </summary>
    [Function("execute")]
    public async Task<HttpResponseData> ExecuteManuallyAsync(
        [HttpTrigger(AuthorizationLevel.Function, "POST", Route = "")] HttpRequestData req,
        FunctionContext functionContext)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var cancellationToken = cts.Token;

        using var reader = new StreamReader(req.Body);
        var body = await reader.ReadToEndAsync();
        var overrides = JsonConvert.DeserializeObject<Overrides>(body) ?? Overrides.None;

        try
        {
            await RenewAsync(overrides, functionContext, cancellationToken);
            return req.CreateResponse(HttpStatusCode.Accepted);
        }
        catch (Exception)
        {
            var response = req.CreateResponse(HttpStatusCode.BadRequest);
            using var writer = new StreamWriter(response.Body, Encoding.UTF8, leaveOpen: true);
            await writer.WriteAsync(JsonConvert.SerializeObject(new
            {
                message = "Certificate renewal failed, check appinsights for details"
            }));
            return response;
        }
    }

    /// <summary>
    /// Time triggered function that reads config files from storage
    /// and renews certificates accordingly if needed.
    /// </summary>
    [Function("renew")]
    public async Task RenewAsync(
        [TimerTrigger(Schedule.Daily, RunOnStartup = true)] TimerInfo timer,
        FunctionContext functionContext)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(10));
        var cancellationToken = cts.Token;

        await RenewAsync(null, functionContext, cancellationToken);
    }

    private async Task RenewAsync(
        Overrides overrides,
        FunctionContext functionContext,
        CancellationToken cancellationToken)
    {
        if (overrides != null && overrides.DomainsToUpdate == null)
        {
            // users could pass null parameter
            overrides.DomainsToUpdate = new string[0];
        }
        var configurations = await _configurationLoader.LoadConfigFilesAsync(functionContext, cancellationToken);
        var stopwatch = new Stopwatch();
        // with lots of certificate renewals this could run into function timeout (10mins)
        // with 30 days to expiry (default setting) this isn't a big problem as next day all unfinished renewals are continued
        // user will only get email <= 14 days before expiry so acceptable for now
        var errors = new List<Exception>();
        foreach ((var name, var config) in configurations)
        {
            using (_logger.BeginScope($"Working on certificates from {name}"))
            {
                foreach (var cert in config.Certificates)
                {
                    stopwatch.Restart();
                    var hostNames = string.Join(";", cert.HostNames);
                    cert.Overrides = overrides ?? Overrides.None;
                    try
                    {
                        var result = await _renewalService.RenewCertificateAsync(config.Acme, cert, cancellationToken);
                        switch (result)
                        {
                            case RenewalResult.NoChange:
                                _logger.LogInformation($"Certificate renewal skipped for: {hostNames} (no change required yet)");
                                break;
                            case RenewalResult.Success:
                                _logger.LogInformation($"Certificate renewal succeeded for: {hostNames}");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(result.ToString());
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"Certificate renewal failed for: {hostNames}!");
                        errors.Add(e);
                    }
                    _logger.LogInformation($"Renewing certificates for {hostNames} took: {stopwatch.Elapsed}");
                }
            }
        }
        if (!configurations.Any())
        {
            _logger.LogWarning("No configurations where processed, refere to the sample on how to set up configs!");
        }
        if (errors.Any())
            throw new AggregateException("Failed to process all certificates", errors);
    }
}

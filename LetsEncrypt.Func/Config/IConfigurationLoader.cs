using LetsEncrypt.Logic.Config;
using Microsoft.Azure.Functions.Worker;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LetsEncrypt.Func.Config;

public interface IConfigurationLoader
{
    Task<IEnumerable<(string configName, Configuration)>> LoadConfigFilesAsync(FunctionContext functionContext, CancellationToken cancellationToken);
}

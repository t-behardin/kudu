using Kudu.Contracts.Settings;
using Kudu.Core.SourceControl;
using Kudu.Contracts.Tracing;

namespace Kudu.Core.Deployment
{
    public interface ISiteBuilderFactory
    {
        ISiteBuilder CreateBuilder(ITracer tracer, ILogger logger, IDeploymentSettingsManager settings, IRepository fileFinder, DeploymentInfoBase deploymentInfo);
    }
}

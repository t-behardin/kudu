﻿using System;
using System.IO;
using Kudu.Contracts.Deployment;
#if NETFRAMEWORK
using System.Web.Script.Serialization;
#endif
using Kudu.Core.Deployment;
using Kudu.Core.Infrastructure;
using Kudu.Core.Tracing;

namespace Kudu.Core.Helpers
{
    public class DeploymentCompletedInfo
    {
        public const string LatestDeploymentFile = "LatestDeployment.json";

        public string TimeStamp { get; set; }
        public string SiteName { get; set; }
        public string RequestId { get; set; }
        public string Kind { get; set; }
        public string Status { get; set; }
        public string Details { get; set; }

        public static void Persist(string requestId, IDeploymentStatusFile status)
        {
#if NET6_0_OR_GREATER
            throw new NotImplementedException();
#else
            // signify the deployment is done by git push
            var kind = System.Environment.GetEnvironmentVariable(Constants.ScmDeploymentKind);
            if (string.IsNullOrEmpty(kind))
            {
                kind = status.Deployer;
            }

            var serializer = new JavaScriptSerializer();
            Persist(status.SiteName, kind, requestId, status.Status.ToString(), serializer.Serialize(status), status.ProjectType ?? string.Empty, status.VsProjectId ?? string.Empty);
#endif
        }

        public static void Persist(string siteName, string kind, string requestId, string status, string details, string projectType, string vsProjectId)
        {
#if NET6_0_OR_GREATER
            throw new NotImplementedException();
#else
            var info = new DeploymentCompletedInfo
            {
                TimeStamp = $"{DateTime.UtcNow:s}Z",
                SiteName = siteName,
                Kind = kind,
                RequestId = requestId,
                Status = status,
                Details = details ?? string.Empty
            };

            try
            {
                var path = Path.Combine(System.Environment.ExpandEnvironmentVariables(@"%HOME%"), "site", "deployments");
                var file = Path.Combine(path, $"{Constants.LatestDeployment}.json");
                var serializer = new JavaScriptSerializer();
                var content = serializer.Serialize(info);
                FileSystemHelpers.EnsureDirectory(path);

                // write deployment info to %home%\site\deployments\LatestDeployment.json
                OperationManager.Attempt(() => FileSystemHelpers.Instance.File.WriteAllText(file, content));

                // write to etw
                KuduEventSource.Log.DeploymentCompleted(
                    ServerConfiguration.GetRuntimeSiteName(),
                    info.Kind,
                    info.RequestId,
                    info.Status,
                    info.Details,
                    EnvironmentHelper.KuduVersion.Value,
                    EnvironmentHelper.AppServiceVersion.Value,
                    projectType,
                    vsProjectId);
            }
            catch (Exception ex)
            {
                KuduEventSource.Log.KuduException(
                    ServerConfiguration.GetRuntimeSiteName(),
                    string.Empty,
                    info.RequestId ?? string.Empty,
                    string.Empty,
                    string.Empty,
                    $"{ex}");
            }
#endif
        }
    }
}

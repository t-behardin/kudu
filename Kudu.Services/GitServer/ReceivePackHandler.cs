﻿#region License

// Copyright 2010 Jeremy Skinner (http://www.jeremyskinner.co.uk)
//  
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at http://github.com/JeremySkinner/git-dot-aspx

// This file was modified from the one found in git-dot-aspx

#endregion

using System;
using System.Net;
using System.Web;
using Kudu.Contracts.Infrastructure;
using Kudu.Contracts.SourceControl;
using Kudu.Contracts.Tracing;
using Kudu.Core;
using Kudu.Core.Deployment;
using Kudu.Core.Helpers;
using Kudu.Core.SourceControl;
using Kudu.Core.SourceControl.Git;
using Kudu.Core.Tracing;
using Kudu.Services.Infrastructure;

namespace Kudu.Services.GitServer
{
    public class ReceivePackHandler : GitServerHttpHandler
    {
        private readonly IRepositoryFactory _repositoryFactory;
        private readonly IEnvironment _environment;

        public ReceivePackHandler(ITracer tracer,
                                  IGitServer gitServer,
                                  IOperationLock deploymentLock,
                                  IDeploymentManager deploymentManager,
                                  IRepositoryFactory repositoryFactory,
                                  IEnvironment environment)
            : base(tracer, gitServer, deploymentLock, deploymentManager)
        {
            _repositoryFactory = repositoryFactory;
            _environment = environment;
        }

        public override void ProcessRequestBase(HttpContextBase context)
        {
            using (Tracer.Step("RpcService.ReceivePack"))
            {
                // Ensure that the target directory does not have a non-Git repository.
                IRepository repository = _repositoryFactory.GetRepository();
                if (repository != null && repository.RepositoryType != RepositoryType.Git)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    if (context.ApplicationInstance != null)
                    {
                        context.ApplicationInstance.CompleteRequest();
                    }
                    return;
                }

                try
                {
                    DeploymentLock.LockOperation(() =>
                    {
                        context.Response.ContentType = "application/x-git-receive-pack-result";

                        if (PostDeploymentHelper.IsAutoSwapOngoing())
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Conflict;
                            context.Response.Write(Resources.Error_AutoSwapDeploymentOngoing);
                            context.ApplicationInstance.CompleteRequest();
                            return;
                        }

                        string username = null;
                        if (AuthUtility.TryExtractBasicAuthUser(context.Request, out username))
                        {
                            GitServer.SetDeployer(username);
                        }

                        UpdateNoCacheForResponse(context.Response);

                        // This temporary deployment is for ui purposes only, it will always be deleted via finally.
                        ChangeSet tempChangeSet;
                        using (DeploymentManager.CreateTemporaryDeployment(Resources.ReceivingChanges, out tempChangeSet))
                        {
                            // to pass to kudu.exe post receive hook
                            System.Environment.SetEnvironmentVariable(Constants.RequestIdHeader, _environment.RequestId);
                            try
                            {
                                GitServer.Receive(context.Request.GetInputStream(), context.Response.OutputStream);
                            }
                            finally
                            {
                                System.Environment.SetEnvironmentVariable(Constants.RequestIdHeader, null);
                            }
                        }
                    }, "Handling git receive pack", TimeSpan.Zero);
                }
                catch (LockOperationException ex)
                {
                    context.Response.StatusCode = 409;
                    context.Response.Write(ex.Message);
                    context.ApplicationInstance.CompleteRequest();
                }
            }
        }
    }
}

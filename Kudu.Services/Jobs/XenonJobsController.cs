using Kudu.Contracts;
using Kudu.Contracts.Jobs;
using Kudu.Contracts.Tracing;
using Kudu.Core.Hooks;
using Kudu.Core.Jobs;
using Kudu.Core.Tracing;
using Kudu.Services.Arm;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;

namespace Kudu.Services.Jobs
{
    [ArmControllerConfiguration]
    public class XenonJobsController : ApiController
    {
        private readonly ITracer _tracer;
        private readonly ITriggeredJobsManager _triggeredJobsManager;
        private readonly IContinuousJobsManager _continuousJobsManager;

        public XenonJobsController(ITriggeredJobsManager triggeredJobsManager, IContinuousJobsManager continuousJobsManager, ITracer tracer)
        {
            _triggeredJobsManager = triggeredJobsManager;
            _continuousJobsManager = continuousJobsManager;
            _tracer = tracer;
        }


        [HttpGet]
        public HttpResponseMessage ListAllJobs()
        {
            return ForwardJobRequestToContainer("");
        }

        [HttpGet]
        public HttpResponseMessage ListContinuousJobs()
        {
            return ForwardJobRequestToContainer("continuouswebjobs/");
        }

        [HttpGet]
        public HttpResponseMessage GetContinuousJob(string jobName)
        {
            return ForwardJobRequestToContainer($"continuouswebjobs/{jobName}");
        }

        [HttpPost]
        public HttpResponseMessage EnableContinuousJob(string jobName)
        {
            return ForwardJobRequestToContainer($"continuouswebjobs/{jobName}/start");
        }

        [HttpPost]
        public HttpResponseMessage DisableContinuousJob(string jobName)
        {
            return ForwardJobRequestToContainer($"continuouswebjobs/{jobName}/stop");
        }

        [HttpGet]
        public HttpResponseMessage GetContinuousJobSettings(string jobName)
        {
            return ForwardJobRequestToContainer($"continuouswebjobs/{jobName}/settings");
        }

        [HttpPut]
        public HttpResponseMessage SetContinuousJobSettings(string jobName, JobSettings jobSettings)
        {
            return ForwardJobRequestToContainer($"continuouswebjobs/{jobName}/settings");
        }


        [HttpGet]
        public HttpResponseMessage ListTriggeredJobs()
        {
            return ForwardJobRequestToContainer("triggeredwebjobs/");
        }

        [HttpGet]
        public HttpResponseMessage ListTriggeredJobsInSwaggerFormat()
        {
            IEnumerable<TriggeredJob> triggeredJobs = _triggeredJobsManager.ListJobs(forceRefreshCache: false);

            SwaggerApiDef responseSwagger = new SwaggerApiDef(triggeredJobs);
            return Request.CreateResponse(responseSwagger);
        }

        [HttpGet]
        public HttpResponseMessage GetTriggeredJob(string jobName)
        {
            return ForwardJobRequestToContainer($"triggeredwebjobs/{jobName}");
        }

        [HttpGet]
        public HttpResponseMessage GetTriggeredJobHistory(string jobName)
        {
            return ForwardJobRequestToContainer($"triggeredwebjobs/{jobName}/history");
        }

        [HttpGet]
        public HttpResponseMessage GetTriggeredJobRun(string jobName, string runId)
        {
            return ForwardJobRequestToContainer($"triggeredwebjobs/{jobName}/history/{runId}");
        }

        [HttpPost]
        public HttpResponseMessage InvokeTriggeredJob(string jobName, string arguments = null)
        {
            return ForwardJobRequestToContainer($"triggeredwebjobs/{jobName}/run");
        }

        [HttpPut]
        public HttpResponseMessage CreateContinuousJob(string jobName)
        {
            return ForwardJobRequestToContainer($"continuouswebjobs/{jobName}");
        }

        [HttpPut]
        public HttpResponseMessage CreateContinuousJobArm(string jobName, ArmEntry<ContinuousJob> armContinuousJob)
        {
            // OLD: return SetJobSettings(jobName, armContinuousJob.Properties.Settings, _continuousJobsManager);
            // convert arm entry continuous job to settings for a job like the function normally would?
            // What does this to string actually do? Does it create a json string like I want, or do it just print the object type
            //Request.Headers.Add("settings", armContinuousJob.Properties.Settings.ToString());
            HttpResponseMessage msg = new HttpResponseMessage(HttpStatusCode.OK);
            msg.Content = new StringContent(armContinuousJob.Properties.ToString());
            return msg;
            //return ForwardJobRequestToContainer("continuouswebjobs/{jobName}/settings");
        }

        [HttpDelete]
        public HttpResponseMessage RemoveContinuousJob(string jobName)
        {
            return ForwardJobRequestToContainer($"continuouswebjobs/{jobName}");
        }

        [HttpPut]
        public HttpResponseMessage CreateTriggeredJob(string jobName)
        {
            return ForwardJobRequestToContainer($"triggeredwebjobs/{jobName}");
        }

        [HttpPut]
        public HttpResponseMessage CreateTriggeredJobArm(string jobName, ArmEntry<TriggeredJob> armTriggeredJob)
        {
            throw new NotImplementedException();
        }

        [HttpDelete]
        public HttpResponseMessage RemoveTriggeredJob(string jobName)
        {
            return ForwardJobRequestToContainer($"triggeredwebjobs/{jobName}");
        }

        [HttpGet]
        public HttpResponseMessage GetTriggeredJobSettings(string jobName)
        {
            return ForwardJobRequestToContainer($"triggeredwebjobs/{jobName}/settings");
        }

        [HttpPut]
        public HttpResponseMessage SetTriggeredJobSettings(string jobName, JobSettings jobSettings)
        {
            return ForwardJobRequestToContainer($"triggeredwebjobs/{jobName}/settings");
        }

        [AcceptVerbs("GET", "HEAD", "PUT", "POST", "DELETE", "PATCH")]
        public HttpResponseMessage RequestPassthrough(string jobName, string path)
        {
            return ForwardJobRequestToContainer($"/{jobName}/passthrough/{path}");
        }

        private HttpResponseMessage ListJobsResponseBasedOnETag(IEnumerable<JobBase> jobs)
        {
            string etag = GetRequestETag();

            string currentETag = "\"" + HashHelpers.CalculateCompositeHash(jobs.ToArray()).ToString("x") + "\"";

            HttpResponseMessage response;
            if (etag == currentETag)
            {
                response = Request.CreateResponse(HttpStatusCode.NotModified);
            }
            else
            {
                response = Request.CreateResponse(HttpStatusCode.OK, ArmUtils.AddEnvelopeOnArmRequest(jobs, Request));
            }

            response.Headers.ETag = new EntityTagHeaderValue(currentETag);

            return response;
        }

        private string GetRequestETag()
        {
            return Request.Headers.IfNoneMatch.Select(header => header.Tag).FirstOrDefault();
        }

        private HttpResponseMessage RemoveJob<TJob>(string jobName, IJobsManager<TJob> jobsManager) where TJob : JobBase, new()
        {
            jobsManager.DeleteJob(jobName);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        private async Task<HttpResponseMessage> CreateJob<TJob>(string jobName, IJobsManager<TJob> jobsManager) where TJob : JobBase, new()
        {
            TJob job = null;
            string errorMessage = null;
            HttpStatusCode errorStatusCode;

            // Get the script file name from the content disposition header
            string scriptFileName = null;
            HttpContent content = Request.Content;
            if (content.Headers != null && content.Headers.ContentDisposition != null)
            {
                scriptFileName = content.Headers.ContentDisposition.FileName;
            }

            if (String.IsNullOrEmpty(scriptFileName))
            {
                return CreateErrorResponse(HttpStatusCode.BadRequest, Resources.Error_MissingContentDispositionHeader);
            }

            // Clean the file name from quotes and directories
            scriptFileName = scriptFileName.Trim('"');
            scriptFileName = Path.GetFileName(scriptFileName);

            Stream fileStream = await content.ReadAsStreamAsync();

            try
            {
                // Upload as a zip if content type is of a zipped file
                if (content.Headers.ContentType != null &&
                    String.Equals(content.Headers.ContentType.MediaType, "application/zip", StringComparison.OrdinalIgnoreCase))
                {
                    job = jobsManager.CreateOrReplaceJobFromZipStream(fileStream, jobName);
                }
                else
                {
                    job = jobsManager.CreateOrReplaceJobFromFileStream(fileStream, jobName, scriptFileName);
                }

                errorMessage = job.Error;
                errorStatusCode = HttpStatusCode.BadRequest;
            }
            catch (ConflictException)
            {
                return CreateErrorResponse(HttpStatusCode.Conflict, Resources.Error_WebJobAlreadyExists);
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
                errorStatusCode = HttpStatusCode.InternalServerError;
                _tracer.TraceError(ex);
            }

            // On error, delete job (if exists)
            if (errorMessage != null)
            {
                jobsManager.DeleteJob(jobName);
                return CreateErrorResponse(errorStatusCode, errorMessage);
            }

            return Request.CreateResponse(job);
        }

        private HttpResponseMessage GetJobSettings<TJob>(string jobName, IJobsManager<TJob> jobsManager) where TJob : JobBase, new()
        {
            try
            {
                JobSettings jobSettings = jobsManager.GetJobSettings(jobName);
                return Request.CreateResponse(HttpStatusCode.OK, jobSettings);
            }
            catch (JobNotFoundException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        private HttpResponseMessage SetJobSettings<TJob>(string jobName, JobSettings jobSettings, IJobsManager<TJob> jobsManager) where TJob : JobBase, new()
        {
            try
            {

                jobsManager.SetJobSettings(jobName, jobSettings);
                return Request.CreateResponse(HttpStatusCode.OK);
            }
            catch (JobNotFoundException)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound);
            }
        }

        private HttpResponseMessage CreateErrorResponse(HttpStatusCode errorStatusCode, string errorMessage)
        {
            HttpResponseMessage response = Request.CreateResponse(errorStatusCode);
            response.Content = new StringContent(errorMessage);
            return response;
        }

        private HttpResponseMessage ForwardJobRequestToContainer(string route)
        {
            using (_tracer.Step("XenonJobsController.ForwardToContainer"))
            {
                return Diagnostics.HttpRequestExtensions.ForwardToContainer($"/webjobs/{route}", Request);
            }
        }
    }
}
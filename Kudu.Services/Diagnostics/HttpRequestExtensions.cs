using System;
using System.Collections;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web;

namespace Kudu.Services.Diagnostics
{
    public static class HttpRequestExtensions
    {
        public static bool IsFunctionsPortalRequest(this HttpRequest request)
        {
            return request.Headers[Constants.FunctionsPortal] != null;
        }

        public static HttpResponseMessage ForwardToContainer(string route, HttpRequestMessage request)
        {
            // Forward request to windows container
            // Get the container address and the port kudu agent port
            IDictionary environmentVariables = System.Environment.GetEnvironmentVariables();
            if (environmentVariables.Contains("KUDU_AGENT_HOST") && environmentVariables.Contains("KUDU_AGENT_PORT")
                && environmentVariables.Contains("KUDU_AGENT_USR") && environmentVariables.Contains("KUDU_AGENT_PWD"))
            {
                var containerAddress = environmentVariables["KUDU_AGENT_HOST"];
                var kuduContainerAgentPort = environmentVariables["KUDU_AGENT_PORT"];
                var kudu_agent_un = environmentVariables["KUDU_AGENT_USR"];
                var kudu_agent_pwd = environmentVariables["KUDU_AGENT_PWD"];

                string containerUrl = $"http://{containerAddress}:{kuduContainerAgentPort}" + route;

                // Request the kudu agent in the container to return list of processes
                HttpClient client = new HttpClient();
                string authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{kudu_agent_un}:{kudu_agent_pwd}"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("BASIC", authHeader);

                // Convert the request's address to be the container's address

                // TODO: TEST THAT THIS KEEPS CONTAINER AUTHORIZATION
                request.RequestUri = new Uri(containerUrl);
                //client.SendAsync(request);
                HttpResponseMessage response;
                
                // Determine the request method to use
                if (request.Method == HttpMethod.Get)
                {
                    response = client.GetAsync(containerUrl).Result;
                }
                else if (request.Method == HttpMethod.Post)
                {
                    response = client.PostAsync(containerUrl, request.Content).Result;
                }
                else if (request.Method == HttpMethod.Put)
                {
                    response = client.PutAsync(containerUrl, request.Content).Result;
                }
                else if (request.Method == HttpMethod.Delete)
                {
                    response = client.DeleteAsync(containerUrl).Result;
                }
                else
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
                
                if (response.IsSuccessStatusCode)
                {
                    String urlContents = response.Content.ReadAsStringAsync().Result;
                    return request.CreateResponse(HttpStatusCode.OK, urlContents);
                }
                else
                {
                    return request.CreateResponse(response.StatusCode, response.ReasonPhrase);
                }
            }
            else
            {
                // If these environment variables don't exist, then the container has not started yet
                return request.CreateResponse(HttpStatusCode.NotFound, "The container cannot be reached. Please ensure that is running.");
            }
        }
    }
}

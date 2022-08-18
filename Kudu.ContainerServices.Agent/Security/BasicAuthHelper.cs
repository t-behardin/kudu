﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Core;
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;
using Microsoft.AspNetCore.Authentication;
using System.Runtime.Versioning;
using Kudu.ContainerServices.Agent.Util;

namespace Kudu.ContainerServices.Agent.Security
{   
    public class BasicAuthHelper : ControllerBase
    {

        private readonly RequestDelegate _requestDelegate;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern bool LogonUser(String lpszUsername, String lpszDomain, String lpszPassword,
            int dwLogonType, int dwLogonProvider, out SafeAccessTokenHandle phToken);

        public BasicAuthHelper(RequestDelegate requestDelegate)
        {
            _requestDelegate = requestDelegate;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!OSDetector.IsOnWindows())
            {
                // PrincipalContext method for validating credentials only works on Windows
                throw new NotImplementedException();
            }

            var authHeader = AuthenticationHeaderValue.Parse(context.Request.Headers["Authorization"]);
            var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
            var credentials = Encoding.UTF8.GetString(credentialBytes).Split(':', 2);
            var username = credentials[0];
            var password = credentials[1];

            const int LOGON32_PROVIDER_DEFAULT = 0;
            //This parameter causes LogonUser to create a primary token.   
            const int LOGON32_LOGON_INTERACTIVE = 2;

            // Call LogonUser to obtain a handle to an access token.   
            SafeAccessTokenHandle safeAccessTokenHandle;
            bool success = LogonUser(username, "", password,
                LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT,
                out safeAccessTokenHandle);
            if (success)
            {
                await _requestDelegate(context);
            }
            else
            {
                context.Response.StatusCode = 401; //UnAuthorized
                await context.Response.WriteAsync("Username or password is incorrect.");
            }
        }
    }
}

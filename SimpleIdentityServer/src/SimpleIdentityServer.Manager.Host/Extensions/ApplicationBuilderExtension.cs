﻿#region copyright
// Copyright 2015 Habart Thierry
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
#endregion

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Serilog;
using SimpleIdentityServer.Logging;
using SimpleIdentityServer.Manager.Host.Middleware;
using SimpleIdentityServer.Oauth2Instrospection.Authentication;
using System;

namespace SimpleIdentityServer.Manager.Host.Extensions
{
    public static class ApplicationBuilderExtension
    {
        public static void UseSimpleIdentityServerManager(
            this IApplicationBuilder applicationBuilder,
            ILoggerFactory loggerFactory,
            ManagerOptions options)
        {
            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (options.Introspection == null)
            {
                throw new ArgumentNullException(nameof(options.Introspection));
            }
            
            // 1. Use log.
            loggerFactory.AddSerilog();
            // 2. Display status code page.
            applicationBuilder.UseStatusCodePages();
            // 3. Enable CORS
            applicationBuilder.UseCors("AllowAll");
            // 4. Enable custom exception handler
            applicationBuilder.UseSimpleIdentityServerManagerExceptionHandler(new ExceptionHandlerMiddlewareOptions
            {
                ManagerEventSource = (IManagerEventSource)applicationBuilder.ApplicationServices.GetService(typeof(IManagerEventSource))
            });
            // 5. Enable introspection.
            var introspectionOptions = new Oauth2IntrospectionOptions
            {
                InstrospectionEndPoint = options.Introspection.IntrospectionUrl,
                ClientId = options.Introspection.ClientId,
                ClientSecret = options.Introspection.ClientSecret
            };
            applicationBuilder.UseAuthenticationWithIntrospection(introspectionOptions);
            // 6. Launch ASP.NET API
            applicationBuilder.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller}/{action}/{id?}");
            });
        }
    }
}

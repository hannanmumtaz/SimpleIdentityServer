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

using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Http;
using Microsoft.Extensions.OptionsModel;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SimpleIdentityServer.Oauth2Instrospection.Authentication.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SimpleIdentityServer.Oauth2Instrospection.Authentication
{
    public class Oauth2IntrospectionMiddleware<TOptions> where TOptions : Oauth2IntrospectionOptions, new()
    {
        private const string AuthorizationName = "Authorization";

        private const string BearerName = "Bearer";

        private readonly RequestDelegate _next;

        private readonly Oauth2IntrospectionOptions _options;

        private readonly HttpClient _httpClient;

        #region Constructor

        public Oauth2IntrospectionMiddleware(
            RequestDelegate next,
            IOptions<TOptions> options)
        {
            if (next == null)
            {
                throw new ArgumentNullException(nameof(next));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            _next = next;
            _options = options.Value;
            _httpClient = new HttpClient(_options.BackChannelHttpHandler ?? new HttpClientHandler());
        }

        #endregion

        #region Public methods

        public async Task Invoke(HttpContext context)
        {
            var headers = context.Request.Headers;
            if (headers.ContainsKey(AuthorizationName))
            {
                var authorizationValues = headers[AuthorizationName];
                var authorizationValue = authorizationValues.FirstOrDefault();
                if (authorizationValue != null)
                {
                    var accessToken = GetAccessToken(authorizationValue);
                    if (!string.IsNullOrWhiteSpace(accessToken))
                    {
                        var introspectionResponse = await GetIntrospectionResponse(
                            _options,
                            accessToken);

                        if (introspectionResponse != null)
                        {
                            context.User = CreateClaimPrincipal(introspectionResponse);
                        }
                    }
                }
            }

            await _next.Invoke(context);
        }

        #endregion

        #region Private methods

        private string GetAccessToken(string authorizationValue)
        {
            var splittedAuthorizationValue = authorizationValue.Split(' ');
            if (splittedAuthorizationValue.Count() == 2 && 
                splittedAuthorizationValue[0].Equals(BearerName, StringComparison.InvariantCultureIgnoreCase))
            {
                return splittedAuthorizationValue[1];
            }

            return string.Empty;
        }

        private async Task<IntrospectionResponse> GetIntrospectionResponse(
            Oauth2IntrospectionOptions oauth2IntrospectionOptions,
            string token)
        {
            Uri uri = null;
            var url = oauth2IntrospectionOptions.InstrospectionEndPoint;
            if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                throw new ArgumentException(ErrorDescriptions.TheIntrospectionEndPointIsNotAWellFormedUrl);
            }

            if (string.IsNullOrWhiteSpace(oauth2IntrospectionOptions.ClientId))
            {
                throw new ArgumentException(string.Format(ErrorDescriptions.TheParameterCannotBeEmpty, nameof(oauth2IntrospectionOptions.ClientId)));
            }

            if (string.IsNullOrWhiteSpace(oauth2IntrospectionOptions.ClientSecret))
            {
                throw new ArgumentException(string.Format(ErrorDescriptions.TheParameterCannotBeEmpty, nameof(oauth2IntrospectionOptions.ClientSecret)));
            }

            var introspectionRequestParameters = new Dictionary<string, string>
            {
                { Constants.IntrospectionRequestNames.Token, token },
                { Constants.IntrospectionRequestNames.TokenTypeHint, "access_token" },
                { Constants.IntrospectionRequestNames.ClientId, oauth2IntrospectionOptions.ClientId },
                { Constants.IntrospectionRequestNames.ClientSecret, oauth2IntrospectionOptions.ClientSecret }
            };

            var requestContent = new FormUrlEncodedContent(introspectionRequestParameters);
            var requestMessage = new HttpRequestMessage(HttpMethod.Post, oauth2IntrospectionOptions.InstrospectionEndPoint);
            requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            requestMessage.Content = requestContent;
            var response = await _httpClient.SendAsync(requestMessage);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<IntrospectionResponse>(content);
        }

        #endregion

        #region Private static methods
        
        private static ClaimsPrincipal CreateClaimPrincipal(IntrospectionResponse introspectionResponse)
        {
            var claims = new List<Claim>
            {
                new Claim(Constants.ClaimNames.Subject, introspectionResponse.Subject)
            };
            if (!string.IsNullOrWhiteSpace(introspectionResponse.Scope))
            {
                var scopes = introspectionResponse.Scope.Split(' ');
                foreach(var scope in scopes)
                {
                    claims.Add(new Claim(Constants.ClaimNames.Scope, scope));
                }
            }

            var claimsIdentity = new ClaimsIdentity(claims, "Introspection");
            return new ClaimsPrincipal(claimsIdentity);
        }

        #endregion
    }
}
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

using SimpleIdentityServer.Proxy;
using System;

namespace SimpleIdentityServer.Uma.Core.IntegrationTests
{
    public class AuthProvider
    {
        private const string OpenIdConfigurationUrl = "https://localhost:5443/.well-known/openid-configuration";

        #region Public static methods

        public static string GetIdentityToken()
        {
            var authProvider = new AuthenticationProviderFactory();
            var options = new AuthOptions
            {
                OpenIdConfigurationUrl = OpenIdConfigurationUrl,
                ClientId = "Anonymous",
                ClientSecret = "Anonymous"
            };


            try
            {
                return authProvider.GetAuthProvider(options)
                    .GetIdentityToken("administrator", "password", "openid", "role", "profile")
                    .Result;
            }
            catch (AggregateException ex)
            {
                return null;
            }
        }

        #endregion
    }
}

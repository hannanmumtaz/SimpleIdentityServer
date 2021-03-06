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

using System.Collections.Generic;
using SimpleIdentityServer.Core.Results;

namespace SimpleIdentityServer.Host
{
    public static class Constants
    {
        public static Dictionary<IdentityServerEndPoints, string> MappingIdentityServerEndPointToPartialUrl = new Dictionary<IdentityServerEndPoints, string>
        {
            {
                IdentityServerEndPoints.AuthenticateIndex,
                "/Authenticate/OpenId"
            },
            {
                IdentityServerEndPoints.ConsentIndex,
                "/Consent"
            },
            {
                IdentityServerEndPoints.FormIndex,
                "/Form"
            }
        };

        public const string TwoFactorCookieName = "SimpleIdentityServer-TwoFactorAuth";

        public static class EndPoints
        {
            public const string DiscoveryAction = ".well-known/openid-configuration";
            public const string Authorization = "authorization";
            public const string Token = "token";
            public const string UserInfo = "userinfo";
            public const string Jwks = "jwks";
            public const string Registration = "registration";
            public const string EndSession = "end_session";
            public const string CheckSession = "check_session";
            public const string Revocation = "token/revoke";
            public const string Introspection = "introspect";
            public const string Get401 = "Error/401";            
            public const string Get404 = "Error/404";
            public const string Get500 = "Error/500";
        }
    }
}
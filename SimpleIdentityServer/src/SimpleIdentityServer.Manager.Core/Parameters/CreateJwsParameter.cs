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

using SimpleIdentityServer.Core.Jwt;

namespace SimpleIdentityServer.Manager.Core.Parameters
{
    public class CreateJwsParameter
    {
        /// <summary>
        /// Gets or sets the JSON WEB KEY KID
        /// </summary>
        public string Kid { get; set; }
        /// <summary>
        /// Gets or sets the sign alg
        /// </summary>
        public JwsAlg Alg { get; set; }
        /// <summary>
        /// Gets or sets the JWKS URL
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// Gets or sets the Payload
        /// </summary>
        public JwsPayload Payload { get; set; }
    }
}

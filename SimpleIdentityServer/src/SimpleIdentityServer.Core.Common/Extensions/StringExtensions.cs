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

using System;
using System.Text;

namespace SimpleIdentityServer.Core.Common.Extensions
{
    /// <summary>
    /// Implementation of base64 encoding & decoding according to the RFC
    /// https://tools.ietf.org/html/draft-ietf-jose-json-web-signature-41#appendix-C
    /// </summary>
    public static class StringExtensions
    {
        public static string Base64Encode(this string plainText)
        {
            var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
            return plainTextBytes.Base64EncodeBytes();
        }

        public static string Base64EncodeBytes(this byte[] bytes)
        {
            return Convert.ToBase64String(bytes)
                .Split('=')[0] // Remove any trailing '='s
                .Replace('+', '-') // 62nd char of encoding
                .Replace('/', '_'); // 63nd char of encoding
        }

        public static string Base64Decode(this string base64EncodedData)
        {
            return Encoding.UTF8.GetString(base64EncodedData.Base64DecodeBytes(),
                0,
                base64EncodedData.Base64DecodeBytes().Length);
        }

        public static byte[] Base64DecodeBytes(this string base64EncodedData)
        {
            var s = base64EncodedData
                .Trim()
                .Replace(" ", "+")
                .Replace('-', '+') // 62nd char of encoding
                .Replace('_', '/'); // 63nd char of encoding
            switch (s.Length%4)
            {
                case 0: // No pad chars in this case
                    return Convert.FromBase64String(s);
                case 2: // Two pad chars
                    s += "==";
                    goto case 0;
                case 3: // One pad char
                    s += "=";
                    goto case 0;
                default:
                    throw new Exception("Illegal base64url string!");
            }
        }
    }
}

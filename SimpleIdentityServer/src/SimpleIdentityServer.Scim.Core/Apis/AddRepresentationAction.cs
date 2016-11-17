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

using Newtonsoft.Json.Linq;
using SimpleIdentityServer.Scim.Core.Errors;
using SimpleIdentityServer.Scim.Core.Factories;
using SimpleIdentityServer.Scim.Core.Parsers;
using SimpleIdentityServer.Scim.Core.Results;
using SimpleIdentityServer.Scim.Core.Stores;
using SimpleIdentityServer.Scim.Core.Validators;
using System;
using System.Net;

namespace SimpleIdentityServer.Scim.Core.Apis
{
    public interface IAddRepresentationAction
    {
        ApiActionResult Execute(JObject jObj, string locationPattern, string schemaId, string resourceType, string id);
        ApiActionResult Execute(JObject jObj, string locationPattern, string schemaId, string resourceType);
    }

    internal class AddRepresentationAction : IAddRepresentationAction
    {
        private readonly IRepresentationRequestParser _requestParser;
        private readonly IRepresentationStore _representationStore;
        private readonly IRepresentationResponseParser _responseParser;
        private readonly IApiResponseFactory _apiResponseFactory;
        private readonly IParametersValidator _parametersValidator;

        public AddRepresentationAction(
            IRepresentationRequestParser requestParser, 
            IRepresentationStore representationStore, 
            IRepresentationResponseParser responseParser,
            IApiResponseFactory apiResponseFactory,
            IParametersValidator parametersValidator)
        {
            _requestParser = requestParser;
            _representationStore = representationStore;
            _responseParser = responseParser;
            _apiResponseFactory = apiResponseFactory;
            _parametersValidator = parametersValidator;
        }
        
        public ApiActionResult Execute(JObject jObj, string locationPattern, string schemaId, string resourceType, string id)
        {
            if (jObj == null)
            {
                throw new ArgumentNullException(nameof(jObj));
            }

            _parametersValidator.ValidateLocationPattern(locationPattern);
            if (string.IsNullOrWhiteSpace(schemaId))
            {
                throw new ArgumentNullException(nameof(schemaId));
            }

            if (string.IsNullOrWhiteSpace(resourceType))
            {
                throw new ArgumentNullException(nameof(resourceType));
            }

            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentNullException(nameof(id));
            }

            // 1. Check resource exists.
            if (_representationStore.GetRepresentation(id) != null)
            {
                return _apiResponseFactory.CreateError(
                    HttpStatusCode.InternalServerError,
                    string.Format(ErrorMessages.TheResourceAlreadyExist, id));
            }

            // 2. Parse the request
            string error;
            var result = _requestParser.Parse(jObj, schemaId, CheckStrategies.Strong, out error);
            if (result == null)
            {
                return _apiResponseFactory.CreateError(HttpStatusCode.InternalServerError,
                    error);
            }

            // 3. Set parameters
            result.Id = id;
            result.Created = DateTime.UtcNow;
            result.LastModified = DateTime.UtcNow;
            result.ResourceType = resourceType;
            result.Version = Guid.NewGuid().ToString();

            // 4. Save the request
            _representationStore.AddRepresentation(result);

            // 5. Transform and returns the representation.
            var response = _responseParser.Parse(result, locationPattern.Replace("{id}", result.Id), schemaId, OperationTypes.Modification);
            return _apiResponseFactory.CreateResultWithContent(HttpStatusCode.Created, response.Object, response.Location, result.Version, result.Id);
        }

        public ApiActionResult Execute(JObject jObj, string locationPattern, string schemaId, string resourceType)
        {
            return Execute(jObj, locationPattern, schemaId, resourceType, Guid.NewGuid().ToString());
        }
    }
}
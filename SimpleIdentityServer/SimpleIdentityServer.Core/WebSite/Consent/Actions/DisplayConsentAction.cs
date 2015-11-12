﻿using System.Collections.Generic;
using System.Linq;
using SimpleIdentityServer.Core.Factories;
using SimpleIdentityServer.Core.Helpers;
using SimpleIdentityServer.Core.Models;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Core.Repositories;
using SimpleIdentityServer.Core.Results;

namespace SimpleIdentityServer.Core.WebSite.Consent.Actions
{
    public interface IDisplayConsentAction
    {
        /// <summary>
        /// Fetch the scopes and client name from the ClientRepository and the parameter
        /// Those informations are used to create the consent screen.
        /// </summary>
        /// <param name="authorizationParameter">Authorization code grant type parameter.</param>
        /// <param name="client">Information about the client</param>
        /// <param name="allowedScopes">Allowed scopes</param>
        /// <returns>Action result.</returns>
        ActionResult Execute(
            AuthorizationParameter authorizationParameter,
            out Client client,
            out List<Scope> allowedScopes);
    }

    public class DisplayConsentAction : IDisplayConsentAction
    {
        private readonly IScopeRepository _scopeRepository;

        private readonly IClientRepository _clientRepository;

        private readonly IActionResultFactory _actionResultFactory;

        public DisplayConsentAction(
            IScopeRepository scopeRepository,
            IClientRepository clientRepository,
            IActionResultFactory actionResultFactory)
        {
            _scopeRepository = scopeRepository;
            _clientRepository = clientRepository;
            _actionResultFactory = actionResultFactory;
        }

        /// <summary>
        /// Fetch the scopes and client name from the ClientRepository and the parameter
        /// Those informations are used to create the consent screen.
        /// </summary>
        /// <param name="authorizationParameter">Authorization code grant type parameter.</param>
        /// <param name="client">Information about the client</param>
        /// <param name="allowedScopes">Allowed scopes</param>
        /// <returns>Action result.</returns>
        public ActionResult Execute(
            AuthorizationParameter authorizationParameter,
            out Client client,
            out List<Scope> allowedScopes)
        {
            allowedScopes = GetScopes(authorizationParameter.Scope)
                .Where(s => s.IsDisplayedInConsent)
                .ToList();
            client = _clientRepository.GetClientById(authorizationParameter.ClientId);
            return _actionResultFactory.CreateAnEmptyActionResultWithOutput();
        }

        /// <summary>
        /// Returns a list of scopes from a concatenate list of scopes separated by whitespaces.
        /// </summary>
        /// <param name="concatenateListOfScopes"></param>
        /// <returns>List of scopes</returns>
        private IEnumerable<Scope> GetScopes(string concatenateListOfScopes)
        {
            var result = new List<Scope>();
            var scopeNames = concatenateListOfScopes.Split(' ');
            foreach (var scopeName in scopeNames)
            {
                var scope = _scopeRepository.GetScopeByName(scopeName);
                result.Add(scope);
            }

            return result;
        } 
    }
}

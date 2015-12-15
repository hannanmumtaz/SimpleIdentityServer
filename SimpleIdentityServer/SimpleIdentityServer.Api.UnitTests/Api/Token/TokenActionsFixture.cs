﻿using System;
using System.Net.Http.Headers;
using Moq;

using NUnit.Framework;

using SimpleIdentityServer.Core.Api.Token;
using SimpleIdentityServer.Core.Api.Token.Actions;
using SimpleIdentityServer.Core.Models;
using SimpleIdentityServer.Core.Parameters;
using SimpleIdentityServer.Core.Validators;
using SimpleIdentityServer.Logging;

namespace SimpleIdentityServer.Api.UnitTests.Api.Token
{
    [TestFixture]
    public sealed class TokenActionsFixture
    {
        private Mock<IGetTokenByResourceOwnerCredentialsGrantTypeAction>
            _getTokenByResourceOwnerCredentialsGrantTypeActionFake;

        private Mock<IGetTokenByAuthorizationCodeGrantTypeAction> _getTokenByAuthorizationCodeGrantTypeActionFake;

        private Mock<IResourceOwnerGrantTypeParameterValidator> _resourceOwnerGrantTypeParameterValidatorFake;

        private Mock<IAuthorizationCodeGrantTypeParameterTokenEdpValidator>
            _authorizationCodeGrantTypeParameterTokenEdptValidatorFake;

        private Mock<ISimpleIdentityServerEventSource> _simpleIdentityServerEventSourceFake;

        private ITokenActions _tokenActions;

        [Test]
        public void When_Passing_No_Request_To_ResourceOwner_Grant_Type_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT & ASSERT
            Assert.Throws<ArgumentNullException>(() => _tokenActions.GetTokenByResourceOwnerCredentialsGrantType(null, null));
        }

        [Test]
        public void When_Requesting_Token_Via_Resource_Owner_Credentials_Grant_Type_Then_Events_Are_Logged()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string clientId = "clientId";
            const string userName = "userName";
            const string password = "password";
            const string accessToken = "accessToken";
            const string identityToken = "identityToken";
            var parameter = new ResourceOwnerGrantTypeParameter
            {
                ClientId = clientId,
                UserName = userName,
                Password = password
            };
            var grantedToken = new GrantedToken
            {
                AccessToken = accessToken,
                IdToken = identityToken
            };
            _getTokenByResourceOwnerCredentialsGrantTypeActionFake.Setup(
                g => g.Execute(It.IsAny<ResourceOwnerGrantTypeParameter>(), It.IsAny<AuthenticationHeaderValue>()))
                .Returns(grantedToken);

            // ACT
            _tokenActions.GetTokenByResourceOwnerCredentialsGrantType(parameter, null);

            // ASSERTS
            _simpleIdentityServerEventSourceFake.Verify(s => s.StartGetTokenByResourceOwnerCredentials(clientId, userName, password));
            _simpleIdentityServerEventSourceFake.Verify(s => s.EndGetTokenByResourceOwnerCredentials(accessToken, identityToken));
        }

        [Test]
        public void When_Passing_No_Request_To_AuthorizationCode_Grant_Type_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT & ASSERT
            Assert.Throws<ArgumentNullException>(() => _tokenActions.GetTokenByAuthorizationCodeGrantType(null, null));
        }

        [Test]
        public void When_Requesting_Token_Via_AuthorizationCode_Grant_Type_Then_Events_Are_Logged()
        {
            // ARRANGE
            InitializeFakeObjects();
            const string clientId = "clientId";
            const string code = "code";
            const string accessToken = "accessToken";
            const string identityToken = "identityToken";
            var parameter = new AuthorizationCodeGrantTypeParameter
            {
                ClientId = clientId,
                Code = code
            };
            var grantedToken = new GrantedToken
            {
                AccessToken = accessToken,
                IdToken = identityToken
            };
            _getTokenByAuthorizationCodeGrantTypeActionFake.Setup(
                g => g.Execute(It.IsAny<AuthorizationCodeGrantTypeParameter>(), It.IsAny<AuthenticationHeaderValue>()))
                .Returns(grantedToken);

            // ACT
            _tokenActions.GetTokenByAuthorizationCodeGrantType(parameter, null);

            // ASSERTS
            _simpleIdentityServerEventSourceFake.Verify(s => s.StartGetTokenByAuthorizationCode(clientId, code));
            _simpleIdentityServerEventSourceFake.Verify(s => s.EndGetTokenByAuthorizationCode(accessToken, identityToken));
        }

        private void InitializeFakeObjects()
        {
            _getTokenByResourceOwnerCredentialsGrantTypeActionFake = new Mock<IGetTokenByResourceOwnerCredentialsGrantTypeAction>();
            _getTokenByAuthorizationCodeGrantTypeActionFake = new Mock<IGetTokenByAuthorizationCodeGrantTypeAction>();
            _resourceOwnerGrantTypeParameterValidatorFake = new Mock<IResourceOwnerGrantTypeParameterValidator>();
            _authorizationCodeGrantTypeParameterTokenEdptValidatorFake = new Mock<IAuthorizationCodeGrantTypeParameterTokenEdpValidator>();
            _simpleIdentityServerEventSourceFake = new Mock<ISimpleIdentityServerEventSource>();
            _tokenActions = new TokenActions(_getTokenByResourceOwnerCredentialsGrantTypeActionFake.Object,
                _getTokenByAuthorizationCodeGrantTypeActionFake.Object,
                _resourceOwnerGrantTypeParameterValidatorFake.Object,
                _authorizationCodeGrantTypeParameterTokenEdptValidatorFake.Object,
                _simpleIdentityServerEventSourceFake.Object);
        }
    }
}
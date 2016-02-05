﻿using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using SimpleIdentityServer.Api.DTOs.Request;
using SimpleIdentityServer.Api.Extensions;
using SimpleIdentityServer.Api.ViewModels;
using SimpleIdentityServer.Core.Exceptions;
using SimpleIdentityServer.Core.Protector;
using SimpleIdentityServer.Core.Translation;
using SimpleIdentityServer.Core.WebSite.Authenticate;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web.Mvc;

using ActionResult = System.Web.Mvc.ActionResult;

namespace SimpleIdentityServer.Api.Controllers
{
    public class AuthenticateController : Controller
    {
        private readonly IAuthenticateActions _authenticateActions;

        private readonly IProtector _protector;

        private readonly IEncoder _encoder;

        private readonly ITranslationManager _translationManager;

        // Used for XSRF protection when adding external logins
        private const string XsrfKey = "XsrfId";

        public AuthenticateController(
            IAuthenticateActions authenticateActions,
            IProtector protector,
            IEncoder encoder,
            ITranslationManager translationManager)
        {
            _authenticateActions = authenticateActions;
            _protector = protector;
            _encoder = encoder;
            _translationManager = translationManager;
        }

        #region Public methods

        [HttpGet]
        public ActionResult Index(string code)
        {
            var authenticatedUser = this.GetAuthenticatedUser();
            var decodedCode = _encoder.Decode(code);
            var request = _protector.Decrypt<AuthorizationRequest>(decodedCode);

            var actionResult = _authenticateActions.AuthenticateResourceOwner(
                request.ToParameter(),
                authenticatedUser,
                code);
            var result = this.CreateRedirectionFromActionResult(actionResult,
                request);
            if (result != null)
            {
                return result;
            }

            TranslateView(request.ui_locales);
            var viewModel = new AuthorizeViewModel
            {
                Code = code
            };

            return View(viewModel);
        }

        [HttpPost]
        public ActionResult Index(AuthorizeViewModel authorize)
        {
            var code = _encoder.Decode(authorize.Code);
            var request = _protector.Decrypt<AuthorizationRequest>(code);
            try
            {
                var authenticationManager = this.GetAuthenticationManager();
                var claims = new List<Claim>();
                var actionResult = _authenticateActions.LocalUserAuthentication(authorize.ToParameter(),
                    request.ToParameter(),
                    authorize.Code,
                    out claims);

                var claimIdentity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ApplicationCookie);
                authenticationManager.SignIn(
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = DateTime.UtcNow.AddDays(7)
                    },
                    claimIdentity
                );

                var result = this.CreateRedirectionFromActionResult(actionResult,
                    request);
                if (result != null)
                {
                    return result;
                }

            }
            catch (IdentityServerAuthenticationException)
            {
                // TODO : log the exception
            }

            TranslateView(request.ui_locales);
            return View(authorize);
        }

        [HttpPost]
        public ActionResult ExternalLogin(string provider, string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                throw new ArgumentNullException("code");
            }

            return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Authenticate", new {code = code}));
        }

        public async Task<ActionResult> ExternalLoginCallback(string code)
        {
            var authenticationManager = HttpContext.GetOwinContext().Authentication;
            var loginInformation = await authenticationManager.GetExternalLoginInfoAsync();
            if (loginInformation == null)
            {
                return RedirectToAction("Index", new {code = code});
            }

            string s = "";
            return null;
        }

        #endregion

        #region Private methods

        private void TranslateView(string uiLocales)
        {
            var translations = _translationManager.GetTranslations(uiLocales, new List<string>
            {
                Core.Constants.StandardTranslationCodes.LoginCode,
                Core.Constants.StandardTranslationCodes.UserNameCode,
                Core.Constants.StandardTranslationCodes.PasswordCode,
                Core.Constants.StandardTranslationCodes.RememberMyLoginCode,
                Core.Constants.StandardTranslationCodes.LoginLocalAccount,
                Core.Constants.StandardTranslationCodes.LoginExternalAccount
            });

            ViewBag.Translations = translations;
        }

        #endregion

        #region Private classes

        private class ChallengeResult : HttpUnauthorizedResult
        {
            public ChallengeResult(string provider, string redirectUri)
                : this(provider, redirectUri, null)
            {
            }

            public ChallengeResult(string provider, string redirectUri, string userId)
            {
                LoginProvider = provider;
                RedirectUri = redirectUri;
                UserId = userId;
            }

            public string LoginProvider { get; set; }

            public string RedirectUri { get; set; }

            public string UserId { get; set; }

            public override void ExecuteResult(ControllerContext context)
            {
                var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
                if (UserId != null)
                {
                    properties.Dictionary[XsrfKey] = UserId;
                }
                context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
            }
        }

        #endregion
    }
}
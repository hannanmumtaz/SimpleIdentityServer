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

using Moq;
using SimpleIdentityServer.Uma.Core.Api.CodeSampleController.Actions;
using SimpleIdentityServer.Uma.Core.Code;
using System;
using Xunit;

namespace SimpleIdentityServer.Uma.Core.UnitTests.Api.CodeSampleController
{
    public class GetFrontendCodeActionFixture
    {
        private Mock<ICodeProvider> _codeProviderStub;
        private IGetFrontendCodeAction _getFrontendCodeAction;

        [Fact]
        public void When_Passing_Null_Parameter_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT & ASSERT
            Assert.Throws<ArgumentNullException>(() => _getFrontendCodeAction.Execute(null));
            Assert.Throws<ArgumentNullException>(() => _getFrontendCodeAction.Execute(string.Empty));
        }

        [Fact]
        public void When_Passing_Not_Supported_Language_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT & ASSERT
            var exception = Assert.Throws<ArgumentException>(() => _getFrontendCodeAction.Execute("not_supported"));
            Assert.True(exception.Message == "the language is not supported");
        }
                
        [Fact]
        public void When_Getting_Csharp_Frontend_Code_Then_MemoryStream_Is_Returned()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT
            var result = _getFrontendCodeAction.Execute("csharp");

            // ASSERT
            _codeProviderStub.Verify(c => c.GetFiles(Languages.Csharp, Core.Code.TypeCode.Frontend));
        }

        private void InitializeFakeObjects()
        {
            _codeProviderStub = new Mock<ICodeProvider>();
            _getFrontendCodeAction = new GetFrontendCodeAction(_codeProviderStub.Object);
        }
    }
}

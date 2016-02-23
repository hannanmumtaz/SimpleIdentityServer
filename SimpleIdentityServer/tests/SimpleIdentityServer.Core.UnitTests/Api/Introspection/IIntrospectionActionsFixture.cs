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
using SimpleIdentityServer.Core.Api.Introspection;
using SimpleIdentityServer.Core.Api.Introspection.Actions;
using SimpleIdentityServer.Core.Parameters;
using System;
using System.Net.Http.Headers;
using Xunit;

namespace SimpleIdentityServer.Core.UnitTests.Api.Introspection
{
    public class IIntrospectionActionsFixture
    {
        private Mock<IPostIntrospectionAction> _postIntrospectionActionStub;

        private IIntrospectionActions _introspectionActions;

        #region Exceptions

        [Fact]
        public void When_Passing_Null_Parameter_To_PostIntrospection_Then_Exception_Is_Thrown()
        {
            // ARRANGE
            InitializeFakeObjects();

            // ACT & ASSERT
            Assert.Throws<ArgumentNullException>(() => _introspectionActions.PostIntrospection(null, null));
        }

        #endregion

        #region Happy Path

        [Fact]
        public void When_Passing_Valid_Parameter_To_PostIntrospection_Then_Operation_Is_Called()
        {
            // ARRANGE
            InitializeFakeObjects();
            var parameter = new IntrospectionParameter();

            // ACT
            _introspectionActions.PostIntrospection(parameter, null);

            // ASSERT
            _postIntrospectionActionStub.Verify(p => p.Execute(It.IsAny<IntrospectionParameter>(),
                It.IsAny<AuthenticationHeaderValue>()));
        }

        #endregion

        private void InitializeFakeObjects()
        {
            _postIntrospectionActionStub = new Mock<IPostIntrospectionAction>();
            _introspectionActions = new IntrospectionActions(_postIntrospectionActionStub.Object);
        }
    }
}
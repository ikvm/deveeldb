﻿// 
//  Copyright 2010-2018 Deveel
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
// 
//        http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Deveel.Data.Security;
using Deveel.Data.Services;

using Moq;

using Xunit;

namespace Deveel.Data.Sql.Statements {
	public class DropUserStatementTests : IDisposable {
		private string droppedUser;

		private IContext adminContext;
		private IContext userContext;
		private IContext userInAdminRoleContext;

		public DropUserStatementTests() {
			var container = new ServiceContainer();

			var userManager = new Mock<IUserManager>();
			userManager.Setup(x =>
					x.DropUserAsync(It.IsNotNull<string>()))
				.Callback<string>(x => droppedUser = x)
				.Returns<string>(x => Task.FromResult(true));
			userManager.Setup(x => x.UserExistsAsync(It.IsAny<string>()))
				.Returns<string>(x => Task.FromResult(true));


			var securityManager = new Mock<IRoleManager>();
			securityManager.Setup(x => x.GetUserRolesAsync(It.Is<string>(u => u == "user2")))
				.Returns<string>(x => Task.FromResult<IEnumerable<Role>>(new[] {new Role("admin_group")}));

			var grantManager = new Mock<IGrantManager>();

			container.RegisterInstance<IRoleManager>(securityManager.Object);
			container.RegisterInstance<IUserManager>(userManager.Object);
			container.RegisterInstance<IGrantManager>(grantManager.Object);

			var cache = new PrivilegesCache(null);
			cache.SetSystemPrivileges("admin_group", SqlPrivileges.Admin);

			container.RegisterInstance<IAccessController>(cache);

			var systemContext = new Mock<IContext>();
			systemContext.SetupGet(x => x.Scope)
				.Returns(container);

			adminContext = CreateUserSession(systemContext.Object, User.System);
			userContext = CreateUserSession(systemContext.Object, new User("user1"));
			userInAdminRoleContext = CreateUserSession(systemContext.Object, new User("user2"));
		}

		private static IContext CreateUserSession(IContext parent, User user) {
			var userSession = new Mock<ISession>();
			userSession.SetupGet(x => x.User)
				.Returns(user);
			userSession.SetupGet(x => x.Scope)
				.Returns(parent.Scope.OpenScope(KnownScopes.Session));
			userSession.SetupGet(x => x.ParentContext)
				.Returns(parent);

			return userSession.Object;
		}

		[Theory]
		[InlineData("anto")]
		public async void AdminCreatesUserWithPassword(string user) {
			var statement = new DropUserStatement(user);
			var result = await statement.ExecuteAsync(adminContext);

			Assert.NotNull(result);
			Assert.True(result.IsEmpty());
			Assert.Equal(user, droppedUser);
		}

		[Theory]
		[InlineData("user2")]
		public async void UserDropsOtherUser(string user) {
			var statement = new DropUserStatement(user);

			await Assert.ThrowsAsync<UnauthorizedAccessException>(() => statement.ExecuteAsync(userContext));
		}

		[Theory]
		[InlineData("user2")]
		public async void UserInAdminRoleDropsOtherUser(string user) {
			var statement = new DropUserStatement(user);
			var result = await statement.ExecuteAsync(userInAdminRoleContext);

			Assert.NotNull(result);
			Assert.True(result.IsEmpty());
			Assert.Equal(user, droppedUser);
		}

		[Theory]
		[InlineData("anto", "DROP USER anto")]
		public void DropUser_AsString(string user, string expected) {
			var statement = new DropUserStatement(user);

			Assert.Equal(expected, statement.ToSqlString());
		}


		public void Dispose() {
			adminContext?.Dispose();
			userContext?.Dispose();
			userInAdminRoleContext?.Dispose();
		}
	}
}
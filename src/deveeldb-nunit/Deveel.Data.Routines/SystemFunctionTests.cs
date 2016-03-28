﻿// 
//  Copyright 2010-2014 Deveel
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
using System;

using Deveel.Data;
using Deveel.Data.Sql;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Objects;
using Deveel.Data.Sql.Types;

using NUnit.Framework;

namespace Deveel.Data.Routines {
	[TestFixture]
	public class SystemFunctionTests : ContextBasedTest {
		private Field InvokeFunction(string name) {
			return Query.Access.InvokeSystemFunction(name);
		}

		private Field InvokeFunction(string name, Field arg) {
			return Query.Access.InvokeSystemFunction(name, SqlExpression.Constant(arg));
		}

		[Test]
		public void ResolveSystemFunctionWithNoSchema() {
			IFunction function = null;
			Assert.DoesNotThrow(() => function = Query.Session.Access.ResolveFunction(Query, new ObjectName("user")));
			Assert.IsNotNull(function);
			Assert.AreEqual(SystemSchema.Name, function.FullName.ParentName);
			Assert.AreEqual("user", function.FullName.Name);
		}

		[Test]
		public void ResolveSystemFunctionFullyQualified() {
			var function = Query.Session.Access.ResolveFunction(Query, ObjectName.Parse("SYSTEM.user"));

			Assert.IsNotNull(function);
			Assert.AreEqual(SystemSchema.Name, function.FullName.ParentName);
			Assert.AreEqual("user", function.FullName.Name);
		}

		[Test]
		public void InvokeUserFunction() {
			var result = InvokeFunction("user");
			Assert.IsNotNull(result);
			Assert.AreEqual(AdminUserName, result.Value.ToString());
		}
	}
}

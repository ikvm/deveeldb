﻿using System;

using Deveel.Data.Sql.Statements;

using NUnit.Framework;

namespace Deveel.Data {
	[TestFixture]
	public sealed class RaiseTests : ContextBasedTest {
		protected override void AssertNoErrors(string testName) {
		}

		[Test]
		public void ExceptionInitAndRaise() {
			var block = new PlSqlBlockStatement();
			block.Declarations.Add(new DeclareExceptionInitStatement("MY_ERROR", 340059));
			block.Statements.Add(new RaiseStatement("MY_ERROR"));

			var expected = Is.InstanceOf<StatementException>()
				.And.Property("ErrorCode").EqualTo(340059);

			Assert.Throws(expected, () => AdminQuery.ExecuteStatement(block));
		}

		[Test]
		public void RaiseSystemError() {
			var block = new PlSqlBlockStatement();
			block.Statements.Add(new RaiseStatement("ZERO_DIVIDE"));

			var expected = Is.InstanceOf<StatementException>()
				.And.Property("ErrorCode").EqualTo(100067);

			Assert.Throws(expected, () => AdminQuery.ExecuteStatement(block));
		}
	}
}

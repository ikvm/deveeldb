﻿// 
//  Copyright 2010-2015 Deveel
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

using Deveel.Data.Sql.Cursors;
using Deveel.Data.Sql.Expressions;

namespace Deveel.Data.Sql.Statements {
	public sealed class FetchStatement : SqlStatement, IPreparableStatement, IPreparable {
		public FetchStatement(string cursorName, FetchDirection direction) {
			CursorName = cursorName;
			Direction = direction;
		}

		public string CursorName { get; private set; }

		public FetchDirection Direction { get; private set; }

		public SqlExpression PositionExpression { get; set; }

		public SqlExpression IntoReference { get; set; }

		object IPreparable.Prepare(IExpressionPreparer preparer) {
			throw new NotImplementedException();
		}

		IStatement IPreparableStatement.Prepare(IRequest request) {
			throw new NotImplementedException();
		}
	}
}

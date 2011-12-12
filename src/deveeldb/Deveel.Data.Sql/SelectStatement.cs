// 
//  Copyright 2010-2011 Deveel
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
using System.Collections.Generic;
using System.Text;

using Deveel.Data.QueryPlanning;
using Deveel.Diagnostics;

namespace Deveel.Data.Sql {
	/// <summary>
	/// Provides logics for interpreting an SQL <c>SELECT</c> statement.
	/// </summary>
	public class SelectStatement : Statement {
		/// <summary>
		/// The TableSelectExpression representing the select command itself.
		/// </summary>
		private TableSelectExpression selectExpression;

		/// <summary>
		/// The list of all columns to order by. (ByColumn)
		/// </summary>
		private IList<ByColumn> orderBy;

		/// <summary>
		/// The plan for evaluating this select expression.
		/// </summary>
		private IQueryPlanNode plan;

		private static bool IsIdentitySelect(TableSelectExpression expression) {
			if (expression.Columns.Count != 1)
				return false;
			if (expression.From == null)
				return false;
			if (expression.From.AllTables.Count != 1)
				return false;

			SelectColumn column =  expression.Columns[0];
			if (column.ResolvedName == null)
				return false;
			if (column.ResolvedName.Name != "IDENTITY")
				return false;

			return true;
		}

		protected override void Prepare() {
			// Prepare this object from the StatementTree,
			// The select expression itself
			selectExpression = (TableSelectExpression)GetValue("table_expression");
			// The order by information
			orderBy = (IList<ByColumn>) GetList("order_by");

			// check to see if the construct is the special one for
			// selecting the latest IDENTITY value from a table
			if (IsIdentitySelect(selectExpression)) {
				selectExpression.Columns.RemoveAt(0);
				FromTable fromTable = ((IList<FromTable>) selectExpression.From.AllTables)[0];
				selectExpression.Columns.Add(new SelectColumn(Expression.Parse("IDENTITY('" + fromTable.Name + "')"), "IDENTITY"));
			}

			// Generate the TableExpressionFromSet hierarchy for the expression,
			TableExpressionFromSet fromSet = Planner.GenerateFromSet(selectExpression, Connection);

			// Form the plan
			plan = Planner.FormQueryPlan(Connection, selectExpression, fromSet, orderBy);
		}


		protected override Table Evaluate() {
			// Check the permissions for this user to select from the tables in the
			// given plan.
			CheckUserSelectPermissions(plan);

			bool error = true;
			try {
				Table t = plan.Evaluate(QueryContext);

				if (selectExpression.Into.HasElements)
					t = selectExpression.Into.SelectInto(QueryContext, t);

				error = false;
				return t;
			} finally {
				// If an error occured, dump the command plan to the debug log.
				// Or just dump the command plan if debug level = Information
				if (Debug.IsInterestedIn(DebugLevel.Information) ||
					(error && Debug.IsInterestedIn(DebugLevel.Warning))) {
					StringBuilder buf = new StringBuilder();
					plan.DebugString(0, buf);

					Debug.Write(DebugLevel.Warning, this, "Query Plan debug:\n" + buf);
				}
			}
		}
	}
}
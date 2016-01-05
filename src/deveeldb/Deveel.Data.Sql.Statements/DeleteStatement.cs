﻿using System;

using Deveel.Data.Security;
using Deveel.Data.Serialization;
using Deveel.Data.Sql.Expressions;
using Deveel.Data.Sql.Query;
using Deveel.Data.Sql.Tables;

namespace Deveel.Data.Sql.Statements {
	public sealed class DeleteStatement : SqlStatement, IPreparableStatement {
		public DeleteStatement(ObjectName tableName, SqlExpression whereExpression) 
			: this(tableName, whereExpression, -1) {
		}

		public DeleteStatement(ObjectName tableName, SqlExpression whereExpression, long limit) {
			if (tableName == null)
				throw new ArgumentNullException("tableName");
			if (whereExpression == null)
				throw new ArgumentNullException("whereExpression");

			if (limit <= 0)
				limit = -1;

			TableName = tableName;
			WhereExpression = whereExpression;
			Limit = limit;
		}

		public ObjectName TableName { get; private set; }

		public SqlExpression WhereExpression { get; private set; }

		public long Limit { get; set; }

		IStatement IPreparableStatement.Prepare(IRequest request) {
			var tableName = request.Query.ResolveTableName(TableName);

			if (!request.Query.TableExists(tableName))
				throw new ObjectNotFoundException(tableName);

			var queryExp = new SqlQueryExpression(new SelectColumn[] {SelectColumn.Glob("*") });
			queryExp.FromClause.AddTable(tableName.FullName);
			queryExp.WhereExpression = WhereExpression;

			var queryInfo = new QueryInfo(request, queryExp);
			if (Limit > 0)
				queryInfo.Limit = new QueryLimit(Limit);

			var queryPlan = request.Query.Context.QueryPlanner().PlanQuery(queryInfo);

			return new Prepared(tableName, queryPlan);
		}

		#region Prepared

		[Serializable]
		class Prepared : SqlStatement {
			public Prepared(ObjectName tableName, IQueryPlanNode queryPlan) {
				TableName = tableName;
				QueryPlan = queryPlan;
			}

			private Prepared(ObjectData data)
				: base(data) {
				TableName = data.GetValue<ObjectName>("TableName");
				QueryPlan = data.GetValue<IQueryPlanNode>("QueryPlan");
			}

			public ObjectName TableName { get; private set; }

			public IQueryPlanNode QueryPlan { get; private set; }

			protected override void GetData(SerializeData data) {
				data.SetValue("TableName", TableName);
				data.SetValue("QueryPlan", QueryPlan);
			}

			protected override void ExecuteStatement(ExecutionContext context) {
				var deleteTable = context.Request.Query.GetMutableTable(TableName);

				if (deleteTable == null)
					throw new ObjectNotFoundException(TableName);

				if (!context.Request.Query.UserCanSelectFromPlan(QueryPlan))
					throw new MissingPrivilegesException(context.Request.User().Name, TableName, Privileges.Select);
				if (!context.Request.Query.UserCanDeleteFromTable(TableName))
					throw new MissingPrivilegesException(context.Request.User().Name, TableName, Privileges.Delete);
				
				var result = QueryPlan.Evaluate(context.Request);
				var count = deleteTable.Delete(result);

				context.SetResult(count);
			}
		}

		#endregion
	}
}

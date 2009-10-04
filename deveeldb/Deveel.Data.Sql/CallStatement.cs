//  
//  CallStatement.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
//       Tobias Downer <toby@mckoi.com>
// 
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;

namespace Deveel.Data.Sql {
	/// <summary>
	/// A statement that calls a procedure, and returns a resultant table.
	/// </summary>
	/// <remarks>
	/// This is used to perform some sort of function over the database.  
	/// For example, "CALL SYSTEM_BACKUP('/my_backups/1')" makes a copy 
	/// of the database in the given directory on the disk.
	/// </remarks>
	public class CallStatement : Statement {

		// ---------- Implemented from Statement ----------

		/// <inheritdoc/>
		public override void Prepare() {
		}

		/// <inheritdoc/>
		public override Table Evaluate() {
			DatabaseQueryContext context = new DatabaseQueryContext(database);

			String proc_name = (String)cmd.GetObject("proc_name");
			Expression[] args = (Expression[])cmd.GetObject("args");

			// Get the procedure manager
			ProcedureManager manager = database.ProcedureManager;
			ProcedureName name;

			TableName p_name = null;

			// If no schema def given in the procedure name, first check for the
			// function in the SYS_INFO schema.
			if (proc_name.IndexOf(".") == -1) {
				// Resolve the procedure name into a TableName object.    
				String schema_name = database.CurrentSchema;
				TableName tp_name = TableName.Resolve(Database.SystemSchema, proc_name);
				tp_name = database.TryResolveCase(tp_name);

				// If exists then use this
				if (manager.ProcedureExists(tp_name)) {
					p_name = tp_name;
				}
			}

			if (p_name == null) {
				// Resolve the procedure name into a TableName object.    
				String schema_name = database.CurrentSchema;
				TableName tp_name = TableName.Resolve(schema_name, proc_name);
				tp_name = database.TryResolveCase(tp_name);

				// Does the schema exist?
				bool ignore_case = database.IsInCaseInsensitiveMode;
				SchemaDef schema =
							database.ResolveSchemaCase(tp_name.Schema, ignore_case);
				if (schema == null) {
					throw new DatabaseException("Schema '" + tp_name.Schema +
												"' doesn't exist.");
				} else {
					tp_name = new TableName(schema.Name, tp_name.Name);
				}

				// If this doesn't exist then generate the error
				if (!manager.ProcedureExists(tp_name)) {
					throw new DatabaseException("Stored procedure '" + proc_name +
												"' was not found.");
				}

				p_name = tp_name;
			}

			// Does the procedure exist in the system schema?
			name = new ProcedureName(p_name);

			// Check the user has privs to use this stored procedure
			if (!database.Database.CanUserExecuteStoredProcedure(context,
															 user, name.ToString())) {
				throw new UserAccessException("User not permitted to call: " + proc_name);
			}

			// Evaluate the arguments
			TObject[] vals = new TObject[args.Length];
			for (int i = 0; i < args.Length; ++i) {
				if (args[i].IsConstant) {
					vals[i] = args[i].Evaluate(null, null, context);
				} else {
					throw new StatementException(
									 "CALL argument is not a constant: " + args[i].Text);
				}
			}

			// Invoke the procedure
			TObject result = manager.InvokeProcedure(name, vals);

			// Return the result of the procedure,
			return FunctionTable.ResultTable(context, result);
		}
	}
}
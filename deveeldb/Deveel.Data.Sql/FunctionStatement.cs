// 
//  FunctionStatement.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
//       Tobias Downer <toby@mckoi.com>
//  
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;
using System.Reflection;

namespace Deveel.Data.Sql {
	///<summary>
	/// A handler for defining and dropping functions.
	///</summary>
	public class FunctionStatement : Statement {
		/// <summary>
		/// The type of command we are running through this IFunction object.
		/// </summary>
		private String type;

		/// <summary>
		/// The name of the function.
		/// </summary>
		private TableName fun_name;

		// ----------- Implemented from Statement ----------

		public override void Prepare() {
			type = (String)cmd.GetObject("type");
			String function_name = (String)cmd.GetObject("function_name");

			// Resolve the function name into a TableName object.    
			String schema_name = database.CurrentSchema;
			fun_name = TableName.Resolve(schema_name, function_name);
			fun_name = database.TryResolveCase(fun_name);

		}

		public override Table Evaluate() {
			DatabaseQueryContext context = new DatabaseQueryContext(database);

			// Does the schema exist?
			bool ignore_case = database.IsInCaseInsensitiveMode;
			SchemaDef schema =
					database.ResolveSchemaCase(fun_name.Schema, ignore_case);
			if (schema == null) {
				throw new DatabaseException("Schema '" + fun_name.Schema +
											"' doesn't exist.");
			} else {
				fun_name = new TableName(schema.Name, fun_name.Name);
			}

			if (type.Equals("create")) {

				// Does the user have privs to create this function?
				if (!database.Database.CanUserCreateProcedureObject(context,
																	   user, fun_name)) {
					throw new UserAccessException(
									"User not permitted to create function: " + fun_name);
				}

				// Does a table already exist with this name?
				if (database.TableExists(fun_name)) {
					throw new DatabaseException("Database object with name '" + fun_name +
												"' already exists.");
				}

				// Get the information about the function we are creating
				IList arg_names = (IList)cmd.GetObject("arg_names");
				IList arg_types = (IList)cmd.GetObject("arg_types");
				TObject loc_name = (TObject)cmd.GetObject("location_name");
				TType return_type = (TType)cmd.GetObject("return_type");

				// Note that we currently ignore the arg_names list.


				// Convert arg types to an array
				TType[] arg_type_array = new TType[arg_types.Count];
				arg_types.CopyTo(arg_type_array, 0);

				// We must parse the location name into a class name, and method name
				String specification = loc_name.Object.ToString();
				// Resolve the java_specification to an invokation method.
				MethodInfo proc_method = ProcedureManager.GetProcedureMethod(specification, arg_type_array);
				if (proc_method == null) {
					throw new DatabaseException("Unable to find invokation method for " +
									  ".NET stored procedure name: " + specification);
				}

				// Convert the information into an easily digestible form.
				ProcedureName proc_name = new ProcedureName(fun_name);
				int sz = arg_types.Count;
				TType[] arg_list = new TType[sz];
				for (int i = 0; i < sz; ++i) {
					arg_list[i] = (TType)arg_types[i];
				}

				// Create the .NET function,
				ProcedureManager manager = database.ProcedureManager;
				manager.DefineProcedure(proc_name, specification, return_type, arg_list, user.UserName);

				// The initial grants for a procedure is to give the user who created it
				// full access.
				database.GrantManager.Grant(
					 Privileges.ProcedureAll, GrantObject.Table,
					 proc_name.ToString(), user.UserName, true,
					 Database.InternalSecureUsername);

			} else if (type.Equals("drop")) {
				// Does the user have privs to create this function?
				if (!database.Database.CanUserDropProcedureObject(context,
																	   user, fun_name)) {
					throw new UserAccessException(
									"User not permitted to drop function: " + fun_name);
				}

				// Drop the function
				ProcedureName proc_name = new ProcedureName(fun_name);
				ProcedureManager manager = database.ProcedureManager;
				manager.DeleteProcedure(proc_name);

				// Drop the grants for this object
				database.GrantManager.RevokeAllGrantsOnObject(
											  GrantObject.Table, proc_name.ToString());

			} else {
				throw new Exception("Unknown type: " + type);
			}

			// Return an update result table.
			return FunctionTable.ResultTable(context, 0);

		}

	}
}
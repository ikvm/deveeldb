// 
//  Copyright 2010  Deveel
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

namespace Deveel.Data {
	/// <summary>
	/// Provides very limited access to a <see cref="Table"/> object.
	/// </summary>
	/// <remarks>
	/// The purpose of this object is to define the functionality of a table 
	/// when the root table(s) are locked via the <see cref="Table.LockRoot"/>
	/// method, and when the table is no longer <i>read</i> or <i>write</i> locked via 
	/// the <see cref="LockingMechanism"/> system. During these conditions, 
	/// the table is in a semi-volatile state, so this class provides a safe 
	/// way to access the table without having to worry about using any 
	/// functionality of <see cref="Table"/> which isn't supported at this 
	/// time.
	/// </remarks>
	public sealed class TableAccessState {
		/// <summary>
		/// The underlying Table object.
		/// </summary>
		private readonly Table table;

		/// <summary>
		/// Set to true when the table is first locked.
		/// </summary>
		private bool been_locked;

		internal TableAccessState(Table table) {
			this.table = table;
			been_locked = false;
		}

		/// <summary>
		/// Returns the cell value at the given row/column coordinates 
		/// in the table.
		/// </summary>
		/// <param name="column"></param>
		/// <param name="row"></param>
		/// <remarks>
		/// This method is valid because it doesn't use any of the SelectableScheme
		/// information in any of its parent tables which could change at any time
		/// when there is no <i>read</i> or <i>write</i> lock on the table.
		/// </remarks>
		/// <returns></returns>
		public TObject GetCellContents(int column, int row) {
			return table.GetCell(column, row);
		}

		/// <summary>
		/// Returns the DataTableInfo object that contains information on the 
		/// columns of the table.
		/// </summary>
		public DataTableInfo TableInfo {
			get { return table.TableInfo; }
		}

		/// <summary>
		/// Gets the name of the given column of this table.
		/// </summary>
		/// <param name="column">Index of the column to resolve.</param>
		/// <remarks>
		/// This, together with <see cref="TableInfo"/> is used to find the 
		/// fully qualified name of a column of the table.
		/// </remarks>
		/// <returns>
		/// Returns a fully qualified name for the column at the given index
		/// within the underlying table.
		/// </returns>
		public VariableName GetResolvedVariable(int column) {
			return table.GetResolvedVariable(column);
		}

		/// <summary>
		/// Locks the root rows of the table.
		/// </summary>
		/// <param name="key">Key used to lock.</param>
		/// <remarks>
		/// This method only permits the roots to be locked once.
		/// </remarks>
		public void LockRoot(int key) {
			if (!been_locked) {
				table.LockRoot(key);
				been_locked = true;
			}
		}

		/// <summary>
		/// Unlocks the root rows of the table.
		/// </summary>
		/// <param name="key"></param>
		/// <exception cref="DatabaseException">
		/// If the root rows of the underlying table weren't locked.
		/// </exception>
		public void UnlockRoot(int key) {
			if (been_locked) { // && table.hasRootsLocked()) {
				table.UnlockRoot(key);
				been_locked = false;
			} else {
				throw new Exception("The root rows aren't locked.");
			}
		}

	}
}
﻿// 
//  Copyright 2010-2011  Deveel
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

using Deveel.Data.Deveel.Data;

namespace Deveel.Data {
	public sealed partial class DatabaseConnection {
		/// <summary>
		/// A buffer of triggers.  This contains triggers that can't fire until
		/// the current transaction has closed.  These triggers were generated by
		/// external actions outside of the context of this transaction.
		/// </summary>
		private readonly List<TriggerEventArgs> triggerEventBuffer;

		/// <summary>
		/// A list of triggers that are fired by actions taken on tables in this
		/// transaction.  When the transaction is successfully committed, these
		/// trigger events need to be propogated to other connections in the database
		/// listening for trigger events on the triggered objects.
		/// </summary>
		private readonly List<TriggerEventArgs> triggerEventList;

		/// <summary>
		/// The connection trigger manager that handles actions that cause triggers
		/// to fire on this connection.
		/// </summary>
		private ConnectionTriggerManager triggerManager;

		/// <summary>
		/// A local member that represents the OLD and NEW system tables that
		/// represent the OLD and NEW data in a triggered action.
		/// </summary>
		private readonly OldAndNewInternalTableInfo oldNewTableInfo;

		/// <summary>
		/// The current state of the OLD and NEW system tables including any cached
		/// information about the tables.
		/// </summary>
		private OldNewTableState currentOldNewState = new OldNewTableState();

		/// <summary>
		/// Returns the connection trigger manager for this connection.
		/// </summary>
		public ConnectionTriggerManager TriggerManager {
			get { return triggerManager; }
		}

		///<summary>
		/// Adds a type of trigger for the given trigger source (usually the
		/// name of the table).
		///</summary>
		///<param name="triggerName"></param>
		///<param name="triggerSource"></param>
		///<param name="type"></param>
		/// <remarks>
		/// Adds a type of trigger to the given Table.  When the event is fired, the
		/// <see cref="TriggerCallback"/> delegate is notified of the event.
		/// </remarks>
		public void CreateCallbackTrigger(string triggerName, TableName triggerSource, TriggerEventType type) {
			TriggerManager.CreateCallbackTrigger(triggerName, type, triggerSource, FireCallbackTrigger);
		}

		/// <summary>
		/// Removes a type of trigger for the given trigger source (usually the
		/// name of the table).
		/// </summary>
		/// <param name="triggerName"></param>
		public void DeleteCallbackTrigger(string triggerName) {
			TriggerManager.DropCallbackTrigger(triggerName);
		}

		/// <summary>
		/// Informs the underlying transaction that a high level transaction event
		/// has occurred and should be dispatched to any listeners occordingly.
		/// </summary>
		/// <param name="args"></param>
		public void OnTriggerEvent(TriggerEventArgs args) {
			triggerEventList.Add(args);
		}

		/// <summary>
		/// Notifies the session that an insert/delete or update operation has occurred 
		/// on some table of this <see cref="DatabaseConnection"/>.
		/// </summary>
		/// <param name="args"></param>
		/// <remarks>
		/// This should notify the trigger connection manager of this event so that it 
		/// may perform any action that may have been set up to occur on this event.
		/// </remarks>
		internal void FireTableEvent(TriggerEventArgs args) {
			triggerManager.PerformTriggerAction(args);
		}

		private void FireCallbackTrigger(object sender, TriggerEventArgs args) {
			try {
				// Did we pass in a call back interface?
				if (triggerCallback != null) {
					lock (triggerEventBuffer) {
						// If there is no active transaction then fire trigger immediately.
						if (transaction == null) {
							triggerCallback(args.TriggerName, args.Source.ToString(), args.EventType, args.FireCount);
						}
							// Otherwise add to buffer
						else {
							triggerEventBuffer.Add(args);
						}
					}
				}
			} catch (Exception e) {
				Logger.Error(this, "TRIGGER Exception: " + e.Message);
			}

		}


		/// <summary>
		/// Fires any triggers that are pending in the trigger buffer.
		/// </summary>
		private void FirePendingTriggerEvents() {
			int sz;
			lock (triggerEventBuffer) {
				sz = triggerEventBuffer.Count;
			}
			if (sz > 0) {
				// Post an event that fires the triggers for each listener.
				// Post the event to go off approx 3ms from now.
				database.PostEvent(3, database.CreateEvent(delegate {
					lock (triggerEventBuffer) {
						// Fire all pending trigger events in buffer
						foreach (TriggerEventArgs args in triggerEventBuffer) {
							triggerCallback(args.TriggerName, args.Source.ToString(), args.EventType, args.FireCount);
						}
						// Clear the buffer
						triggerEventBuffer.Clear();
					}

				}));
			}

		}

		// ---------- Triggered OLD/NEW table handling ----------
		// These methods are used by the triggerManager object to
		// temporarily create OLD and NEW tables in this connection from inside a
		// triggered action.  In some cases (before the operation) the OLD table
		// is mutable.

		/// <summary>
		/// Returns the current state of the old/new tables.
		/// </summary>
		/// <returns></returns>
		internal OldNewTableState GetOldNewTableState() {
			return currentOldNewState;
		}

		/**
		 * Sets the current state of the old/new tables.  When nesting OLD/NEW
		 * tables for nested stored procedures, the current state should be first
		 * recorded and reverted back when the nested procedure finishes.
		 */
		internal void SetOldNewTableState(OldNewTableState state) {
			currentOldNewState = state;
		}

		/// <summary>
		/// An internal table info object that handles OLD and NEW tables for
		/// triggered actions.
		/// </summary>
		private class OldAndNewInternalTableInfo : IInternalTableInfo {
			private readonly DatabaseConnection conn;

			public OldAndNewInternalTableInfo(DatabaseConnection conn) {
				this.conn = conn;
			}

			private bool HasOLDTable {
				get { return conn.currentOldNewState.OldRowIndex != -1; }
			}

			private bool HasNEWTable {
				get { return conn.currentOldNewState.NewDataRow != null; }
			}

			public int TableCount {
				get {
					int count = 0;
					if (HasOLDTable) {
						++count;
					}
					if (HasNEWTable) {
						++count;
					}
					return count;
				}
			}

			public int FindTableName(TableName name) {
				if (HasOLDTable && name.Equals(SystemSchema.OldTriggerTable)) {
					return 0;
				}
				if (HasNEWTable && name.Equals(SystemSchema.NewTriggerTable)) {
					return HasOLDTable ? 1 : 0;
				}
				return -1;
			}

			public TableName GetTableName(int i) {
				if (HasOLDTable) {
					if (i == 0) {
						return SystemSchema.OldTriggerTable;
					}
				}
				return SystemSchema.NewTriggerTable;
			}

			public bool ContainsTableName(TableName name) {
				return FindTableName(name) != -1;
			}

			public String GetTableType(int i) {
				return "SYSTEM TABLE";
			}

			public DataTableInfo GetTableInfo(int i) {
				DataTableInfo tableInfo = conn.GetTableInfo(conn.currentOldNewState.TableSource);
				DataTableInfo newTableInfo = tableInfo.Clone(GetTableName(i));
				return newTableInfo;
			}

			public ITableDataSource CreateInternalTable(int index) {
				DataTableInfo tableInfo = GetTableInfo(index);

				TriggeredOldNewDataSource table = new TriggeredOldNewDataSource(conn.System, tableInfo);

				if (HasOLDTable) {
					if (index == 0) {

						// Copy data from the table to the new table
						DataTable dtable = conn.GetTable(conn.currentOldNewState.TableSource);
						DataRow oldRow = new DataRow(table);
						int rowIndex = conn.currentOldNewState.OldRowIndex;
						for (int i = 0; i < tableInfo.ColumnCount; ++i) {
							oldRow.SetValue(i, dtable.GetCell(i, rowIndex));
						}
						// All OLD tables are immutable
						table.SetImmutable(true);
						table.SetRowData(oldRow);

						return table;
					}
				}

				table.SetImmutable(!conn.currentOldNewState.IsNewMutable);
				table.SetRowData(conn.currentOldNewState.NewDataRow);

				return table;
			}

		}

		/// <summary>
		/// A IMutableTableDataSource implementation that is used for trigger actions
		/// to represent the data in the OLD and NEW tables.
		/// </summary>
		private sealed class TriggeredOldNewDataSource : GTDataSource, IMutableTableDataSource {
			private readonly DataTableInfo tableInfo;
			private DataRow content;
			private bool immutable;

			public TriggeredOldNewDataSource(TransactionSystem system, DataTableInfo tableInfo)
				: base(system) {
				this.tableInfo = tableInfo;
			}

			internal void SetImmutable(bool im) {
				immutable = im;
			}

			internal void SetRowData(DataRow dataRow) {
				content = dataRow;
			}

			public override DataTableInfo TableInfo {
				get { return tableInfo; }
			}

			public override int RowCount {
				get { return 1; }
			}

			public override TObject GetCell(int column, int row) {
				if (row < 0 || row > 0) {
					throw new Exception("Row index out of bounds.");
				}
				return content.GetValue(column);
			}

			public int AddRow(DataRow dataRow) {
				throw new Exception("Inserting into table '" + TableInfo.TableName + "' is not permitted.");
			}

			public void RemoveRow(int rowIndex) {
				throw new Exception("Deleting from table '" + TableInfo.TableName + "' is not permitted.");
			}

			public int UpdateRow(int rowIndex, DataRow dataRow) {
				if (immutable)
					throw new Exception("Updating table '" + TableInfo.TableName + "' is not permitted.");
				if (rowIndex < 0 || rowIndex > 0)
					throw new Exception("Row index out of bounds.");

				int sz = TableInfo.ColumnCount;
				for (int i = 0; i < sz; ++i) {
					content.SetValue(i, dataRow.GetValue(i));
				}

				return 0;
			}

			public MasterTableJournal Journal {
				get {
					// Shouldn't be used...
					throw new Exception("Invalid method used.");
				}
			}

			public void FlushIndexChanges() {
				// Shouldn't be used...
				throw new Exception("Invalid method used.");
			}

			public void ConstraintIntegrityCheck() {
				// Should always pass (not integrity check needed for OLD/NEW table.
			}

			public void AddRootLock() {
			}

			public void RemoveRootLock() {
			}
		}

		/// <summary>
		/// An internal table info object that handles OLD and NEW tables for
		/// triggered actions.
		/// </summary>
		internal sealed class OldNewTableState {

			/// <summary>
			///  The name of the table that is the trigger source.
			/// </summary>
			private readonly TableName tableSource;

			/// <summary>
			/// The row index of the OLD data that is being updated or deleted in the
			/// trigger source table.
			/// </summary>
			private readonly int oldRowIndex = -1;

			/// <summary>
			/// The DataRow of the new data that is being inserted/updated in the trigger
			/// source table.
			/// </summary>
			private readonly DataRow newDataRow;

			/// <summary>
			/// If true then the 'new_data' information is mutable which would be true for
			/// a BEFORE trigger.
			/// </summary>
			/// <remarks>
			/// For example, we would want to change the data in the row that caused the 
			/// trigger to fire.
			/// </remarks>
			private readonly bool newMutable;

			/// <summary>
			/// The DataTable object that represents the OLD table, if set.
			/// </summary>
			private DataTable oldDataTable;

			/// <summary>
			/// The DataTable object that represents the NEW table, if set.
			/// </summary>
			private DataTable newDataTable;

			public OldNewTableState(TableName tableSource, int oldRowIndex, DataRow newDataRow, bool newMutable) {
				this.tableSource = tableSource;
				this.oldRowIndex = oldRowIndex;
				this.newDataRow = newDataRow;
				this.newMutable = newMutable;
			}

			internal OldNewTableState() {
			}

			/// <summary>
			///  The name of the table that is the trigger source.
			/// </summary>
			public TableName TableSource {
				get { return tableSource; }
			}

			/// <summary>
			/// The row index of the OLD data that is being updated or deleted in the
			/// trigger source table.
			/// </summary>
			public int OldRowIndex {
				get { return oldRowIndex; }
			}

			/// <summary>
			/// The DataRow of the new data that is being inserted/updated in the trigger
			/// source table.
			/// </summary>
			public DataRow NewDataRow {
				get { return newDataRow; }
			}

			/// <summary>
			/// If true then the 'new_data' information is mutable which would be true for
			/// a BEFORE trigger.
			/// </summary>
			/// <remarks>
			/// For example, we would want to change the data in the row that caused the 
			/// trigger to fire.
			/// </remarks>
			public bool IsNewMutable {
				get { return newMutable; }
			}

			/// <summary>
			/// The DataTable object that represents the OLD table, if set.
			/// </summary>
			public DataTable OldDataTable {
				get { return oldDataTable; }
				set { oldDataTable = value; }
			}

			/// <summary>
			/// The DataTable object that represents the NEW table, if set.
			/// </summary>
			public DataTable NewDataTable {
				get { return newDataTable; }
				set { newDataTable = value; }
			}
		}
	}
}
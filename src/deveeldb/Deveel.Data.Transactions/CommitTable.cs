﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Deveel.Data.DbSystem;
using Deveel.Data.Deveel.Data.Sql;
using Deveel.Data.Deveel.Data.Transactions;
using Deveel.Data.Index;
using Deveel.Data.Sql;

namespace Deveel.Data.Transactions {
	internal class CommitTable : IMutableTable {
		private int rowListRebuild;
		private IIndex rowIndex;

		private int[] indexRebuilds;
		private IIndexSet indexSet;
		private ColumnIndex[] columnIndexes;

		private int lastEntryRICheck;

		public CommitTable(ITransaction transaction, TableSource tableSource, TableEventRegistry eventRegistry) {
			Transaction = transaction;
			TableSource = tableSource;
			EventRegistry = eventRegistry;

			indexSet = transaction.GetTableManager().GetIndexSetForTable(tableSource);
			rowListRebuild = 0;

			var colCount = ColumnCount;
			indexRebuilds = new int[colCount];
			columnIndexes = new ColumnIndex[colCount];
			lastEntryRICheck = eventRegistry.EventCount;
		}

		~CommitTable() {
			Dispose(false);
		}

		public int ColumnCount {
			get { return TableInfo.ColumnCount; }
		}

		public TableInfo TableInfo {
			get { return TableSource.TableInfo; }
		}

		public int RowCount {
			get {
				// Ensure the row list is up to date.
				EnsureRowIndexListCurrent();
				return RowIndex.Count;
			}
		}

		private void EnsureRowIndexListCurrent() {
			throw new NotImplementedException();
		}

		public DataObject GetValue(long rowNumber, int columnOffset) {
			throw new NotImplementedException();
		}

		public ColumnIndex GetIndex(int columnOffset) {
			throw new NotImplementedException();
		}

		private IIndex RowIndex {
			get {
				if (rowIndex == null)
					rowIndex = indexSet.GetIndex(0);

				return rowIndex;
			}
		}

		public ITransaction Transaction { get; private set; }

		public TableSource TableSource { get; private set; }

		public TableEventRegistry EventRegistry { get; private set; }

		private TableSourceComposite TableComposite {
			get { return Transaction.Context.Database.TableComposite; }
		}

		private int TableId {
			get { return TableSource.TableId; }
		}

		public ObjectName FullName {
			get { return TableInfo.TableName; }
		}

		public DbObjectType ObjectType {
			get { return DbObjectType.Table; }
		}

		public IEnumerator<Row> GetEnumerator() {
			// Ensure the row list is up to date.
			EnsureRowIndexListCurrent();
			var enumerator = RowIndex.GetEnumerator();
			return new RowEnumerator(this, enumerator);
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}

		private void Dispose(bool disposing) {
			if (disposing) {
				// Dispose and invalidate the schemes
				// This is really a safety measure to ensure the schemes can't be
				// used outside the scope of the lifetime of this object.
				foreach (var columnIndex in columnIndexes) {
					if (columnIndex != null)
						columnIndex.Dispose();
				}

				columnIndexes = null;
				rowIndex = null;
				EventRegistry = null;
				indexRebuilds = null;
				indexSet = null;
				Transaction = null;
			}
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public Row NewRow() {
			return new Row(this, new RowId(TableId, -1));
		}

		public void AddRow(Row row) {
			if (Transaction.IsReadOnly)
				throw new Exception("Transaction is Read only.");

			if (TableSource.IsReadOnly)
				throw new ApplicationException("Can not add row - table is read-only.");

			int rowNum;
			try {
				rowNum = TableSource.AddRow(row);
			} catch (IOException e) {
				throw new ApplicationException("IO Error: " + e.Message, e);
			}

			row.SetRowNumber(rowNum);

			// Note this doesn't need to be synchronized because we are exclusive on
			// this table.
			
			EventRegistry.Register(new TableRowEvent(TableId, rowNum, TableRowEventType.Add));
		}

		public void UpdateRow(Row row) {
			if (Transaction.IsReadOnly)
				throw new Exception("Transaction is Read only.");

			// Check this isn't a Read only source
			if (TableSource.IsReadOnly)
				throw new ApplicationException("Can not update row - table is read-only.");

			if (row.RowId.IsNull)
				throw new ArgumentException("The ROWID cannot be null in an update.");

			if (row.RowId.TableId != TableId)
				throw new ArgumentException("The row was not created from this table.");

			var rowNum = row.RowId.RowNumber;

			if (rowNum < 0)
				throw new ArgumentException("The number part of the ROWID is invalid.");

			// Note this doesn't need to be synchronized because we are exclusive on
			// this table.
			EventRegistry.Register(new TableRowEvent(TableId, rowNum, TableRowEventType.UpdateRemove));

			int newRowIndex;

			try {
				newRowIndex = TableSource.AddRow(row);
			} catch (IOException e) {
				throw new ApplicationException("IO Error: " + e.Message, e);
			}

			row.SetRowNumber(newRowIndex);

			// Note this doesn't need to be synchronized because we are exclusive on
			// this table.
			EventRegistry.Register(new TableRowEvent(TableId, newRowIndex, TableRowEventType.UpdateAdd));
		}

		public bool RemoveRow(RowId rowId) {
			if (rowId.IsNull)
				throw new ArgumentNullException("rowId");

			if (rowId.TableId != TableId)
				throw new ArgumentException("The table part of the ROWID is not this table.");
			if (rowId.RowNumber < 0)
				throw new ArgumentException("The number part of the ROWID is not valid for remove.");

			if (Transaction.IsReadOnly)
				throw new Exception("Transaction is Read only.");

			// Check this isn't a Read only source
			if (TableSource.IsReadOnly)
				throw new ApplicationException("Can not remove row - table is Read only.");

			// NOTE: This must <b>NOT</b> call 'RemoveRow' in TableSource.
			//   We do not want to delete a row permanently from the underlying
			//   file because the transaction using this data source may yet decide
			//   to roll back the change and not delete the row.

			// Note this doesn't need to be synchronized because we are exclusive on
			// this table.
			EventRegistry.Register(new TableRowEvent(rowId.TableId, rowId.RowNumber, TableRowEventType.Remove));

			return true;
		}

		public void FlushIndexes() {
			EnsureRowIndexListCurrent();

			// This will flush all of the column indexes
			for (int i = 0; i < columnIndexes.Length; ++i) {
				GetIndex(i);
			}
		}

		private void ExecuteUpdateReferentialAction(ConstraintInfo constraint, DataObject[] originalKey, DataObject[] newKey, IQueryContext context) {
			var update_rule = constraint.OnUpdate;
			if (update_rule == ForeignKeyAction.NoAction &&
				constraint.Deferred != ConstraintDeferrability.InitiallyImmediate) {
				// Constraint check is deferred
				return;
			}

			// So either update rule is not NO ACTION, or if it is we are initially
			// immediate.
			var keyTable = Transaction.GetMutableTable(constraint.TableName);
			var tableInfo = keyTable.TableInfo;
			var keyCols = tableInfo.IndexOfColumns(constraint.ColumnNames).ToArray();
			var keyEntries = keyTable.FindKeys(keyCols, originalKey);

			// Are there keys effected?
			if (keyEntries.Any()) {
				if (update_rule == ForeignKeyAction.NoAction)
					// Throw an exception;
					throw new ConstraintViolationException(
						SqlModelErrorCodes.ForeignKeyViolation,
						constraint.Deferred.AsDebugString() +
						" foreign key constraint violation on update (" +
						constraint.ConstraintName + ") Columns = " +
						constraint.TableName + "( " +
						String.Join(", ", constraint.ColumnNames) +
						" ) -> " + constraint.ForeignTable.FullName + "( " +
						String.Join(", ", constraint.ForeignColumnNames) +
						" )");

				// Perform a referential action on each updated key
				foreach (int rowNum in keyEntries) {
					var dataRow = new Row(keyTable, new RowId(keyTable.TableInfo.Id, rowNum));
					dataRow.SetFromTable();

					if (update_rule == ForeignKeyAction.Cascade) {
						// Update the keys
						for (int n = 0; n < keyCols.Length; ++n) {
							dataRow.SetValue(keyCols[n], newKey[n]);
						}
						keyTable.UpdateRow(dataRow);
					} else if (update_rule == ForeignKeyAction.SetNull) {
						for (int n = 0; n < keyCols.Length; ++n) {
							dataRow.SetNull(keyCols[n]);
						}
						keyTable.UpdateRow(dataRow);
					} else if (update_rule == ForeignKeyAction.SetDefault) {
						for (int n = 0; n < keyCols.Length; ++n) {
							dataRow.SetDefault(keyCols[n], context);
						}
						keyTable.UpdateRow(dataRow);
					} else {
						throw new Exception("Do not understand referential action: " + update_rule);
					}
				}

				// Check referential integrity of modified table,
				keyTable.AssertConstraints();
			}
		}

		public void AssertConstraints() {
			try {
				// Early exit condition
				if (lastEntryRICheck == EventRegistry.EventCount)
					return;

				// This table name
				var tableInfo = TableInfo;
				var tName = tableInfo.TableName;
				IQueryContext context = new SystemQueryContext(Transaction, tName.Parent.Name);

				// Are there any added, deleted or updated entries in the journal since
				// we last checked?
				List<int> rowsUpdated = new List<int>();
				List<int> rowsDeleted = new List<int>();
				List<int> rowsAdded = new List<int>();

				var events = EventRegistry.Skip(lastEntryRICheck);
				foreach (var tableEvent in events.OfType<TableRowEvent>()) {
					var rowNum = tableEvent.RowNumber;
					if (tableEvent.EventType == TableRowEventType.Remove ||
					    tableEvent.EventType == TableRowEventType.UpdateRemove) {
						rowsDeleted.Add(rowNum);

						var index = rowsAdded.IndexOf(rowNum);
						if (index != -1)
							rowsAdded.RemoveAt(index);
					} else if (tableEvent.EventType == TableRowEventType.Add ||
					           tableEvent.EventType == TableRowEventType.UpdateAdd) {
						rowsAdded.Add(rowNum);
					}

					if (tableEvent.EventType == TableRowEventType.UpdateAdd ||
						tableEvent.EventType == TableRowEventType.UpdateRemove)
						rowsUpdated.Add(rowNum);
				}

				// Were there any updates or deletes?
				if (rowsDeleted.Count > 0) {
					// Get all references on this table
					var foreignConstraints = Transaction.QueryTableImportedForeignKeys(tName);

					// For each foreign constraint
					foreach (var constraint in foreignConstraints) {
						// For each deleted/updated record in the table,
						foreach (var rowNum in rowsDeleted) {
							// What was the key before it was updated/deleted
							var cols = tableInfo.IndexOfColumns(constraint.ForeignColumnNames).ToArray();

							var originalKey = new DataObject[cols.Length];
							int nullCount = 0;
							for (int p = 0; p < cols.Length; ++p) {
								originalKey[p] = GetValue(rowNum, cols[p]);
								if (originalKey[p].IsNull) {
									++nullCount;
								}
							}

							// Check the original key isn't null
							if (nullCount != cols.Length) {
								// Is is an update?
								int updateIndex = rowsUpdated.IndexOf(rowNum);
								if (updateIndex != -1) {
									// Yes, this is an update
									int rowIndexAdd = rowsUpdated[updateIndex + 1];
									// It must be an update, so first see if the change caused any
									// of the keys to change.
									bool keyChanged = false;
									var keyUpdatedTo = new DataObject[cols.Length];
									for (int p = 0; p < cols.Length; ++p) {
										keyUpdatedTo[p] = GetValue(rowIndexAdd, cols[p]);
										if (originalKey[p].CompareTo(keyUpdatedTo[p]) != 0) {
											keyChanged = true;
										}
									}
									if (keyChanged) {
										// Allow the delete, and execute the action,
										// What did the key update to?
										ExecuteUpdateReferentialAction(constraint, originalKey, keyUpdatedTo, context);
									}
									// If the key didn't change, we don't need to do anything.
								} else {
									// No, so it must be a delete,
									// This will look at the referencee table and if it contains
									// the key, work out what to do with it.
									ExecuteDeleteReferentialAction(constraint, originalKey, context);
								}

							}  // If the key isn't null

						}  // for each deleted rows

					}  // for each foreign key reference to this table

				}

				// Were there any rows added (that weren't deleted)?
				if (rowsAdded.Count > 0) {
					int[] rowIndices = rowsAdded.ToArray();

					// Check for any field constraint violations in the added rows
					Transaction.CheckFieldConstraintViolations(this, rowIndices);

					// Check this table, adding the given row_index, immediate
					Transaction.CheckAddConstraintViolations(this, rowIndices, ConstraintDeferrability.InitiallyImmediate);
				}
			} catch (ConstraintViolationException) {
				// If a constraint violation, roll back the changes since the last
				// check.
				int rollbackPoint = EventRegistry.EventCount - lastEntryRICheck;
				if (rowListRebuild <= rollbackPoint) {
					EventRegistry.Rollback(rollbackPoint);
				} else {
					// TODO: emit a warning
				}

				throw;
			} finally {
				// Make sure we update the 'last_entry_ri_check' variable
				lastEntryRICheck = EventRegistry.EventCount;
			}
		}

		private void ExecuteDeleteReferentialAction(ConstraintInfo constraint, DataObject[] originalKey, IQueryContext context) {
			var deleteRule = constraint.OnDelete;
			if (deleteRule == ForeignKeyAction.NoAction &&
				constraint.Deferred != ConstraintDeferrability.InitiallyImmediate) {
				// Constraint check is deferred
				return;
			}

			// So either delete rule is not NO ACTION, or if it is we are initially
			// immediate.
			var keyTable = Transaction.GetMutableTable(constraint.TableName);
			var tableInfo = keyTable.TableInfo;
			var keyCols = tableInfo.IndexOfColumns(constraint.ColumnNames).ToArray();
			var keyEntries = keyTable.FindKeys(keyCols, originalKey).ToList();

			// Are there keys effected?
			if (keyEntries.Count > 0) {
				if (deleteRule == ForeignKeyAction.NoAction) {
					// Throw an exception;
					throw new ConstraintViolationException(
						SqlModelErrorCodes.ForeignKeyViolation,
						constraint.Deferred.AsDebugString() +
						" foreign key constraint violation on delete (" +
						constraint.ConstraintName + ") Columns = " +
						constraint.TableName + "( " +
						String.Join(", ", constraint.ColumnNames) +
						" ) -> " + constraint.ForeignTable.FullName + "( " +
						String.Join(", ", constraint.ForeignColumnNames) +
						" )");
				}

				// Perform a referential action on each updated key
				foreach (int rowNum in keyEntries) {
					var dataRow = new Row(keyTable, new RowId(tableInfo.Id, rowNum));
					dataRow.SetFromTable();

					if (deleteRule == ForeignKeyAction.Cascade) {
						// Cascade the removal of the referenced rows
						keyTable.RemoveRow(dataRow.RowId);
					} else if (deleteRule == ForeignKeyAction.SetNull) {
						for (int n = 0; n < keyCols.Length; ++n) {
							dataRow.SetNull(keyCols[n]);
						}
						keyTable.UpdateRow(dataRow);
					} else if (deleteRule == ForeignKeyAction.SetDefault) {
						for (int n = 0; n < keyCols.Length; ++n) {
							dataRow.SetDefault(keyCols[n], context);
						}
						keyTable.UpdateRow(dataRow);
					} else {
						throw new Exception("Do not understand referential action: " + deleteRule);
					}
				}

				// Check referential integrity of modified table,
				keyTable.AssertConstraints();
			}

		}

		public void AddLock() {
			TableSource.AddLock();
		}

		public void RemoveLock() {
			TableSource.RemoveLock();
		}

		#region RowEnumerator

		class RowEnumerator : IEnumerator<Row> {
			private readonly CommitTable table;
			private readonly IIndexEnumerator<int> enumerator;

			public RowEnumerator(CommitTable table, IIndexEnumerator<int> enumerator) {
				this.table = table;
				this.enumerator = enumerator;
			}

			public void Dispose() {
				enumerator.Dispose();
			}

			public bool MoveNext() {
				return enumerator.MoveNext();
			}

			public void Reset() {
				enumerator.Reset();
			}

			public Row Current {
				get { return new Row(table, enumerator.Current); }
			}

			object IEnumerator.Current {
				get { return Current; }
			}
		}

		#endregion
	}
}
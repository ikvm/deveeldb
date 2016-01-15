﻿using System;
using System.Collections.Generic;
using System.Linq;

using Deveel.Data.Diagnostics;
using Deveel.Data.Sql;

namespace Deveel.Data.Transactions {
	public sealed class TableCommitEvent : Event {
		public TableCommitEvent(ObjectName tableName, int tableId, IEnumerable<int> addedRows, IEnumerable<int> removedRows) {
			if (tableName == null)
				throw new ArgumentNullException("tableName");

			TableName = tableName;
			TableId = tableId;
			AddedRows = addedRows;
			RemovedRows = removedRows;
		}

		public ObjectName TableName { get; private set; }

		public int TableId { get; private set; }

		public IEnumerable<int> AddedRows { get; private set; }
		
		public IEnumerable<int> RemovedRows { get; private set; }

		protected override void GetEventData(Dictionary<string, object> data) {
			data[KnownEventMetadata.TableId] = TableId;
			data[KnownEventMetadata.TableName] = TableName.FullName;
			data["table.addedRows"] = AddedRows.ToArray();
			data["table.removedRows"] = RemovedRows.ToArray();
		}
	}
}
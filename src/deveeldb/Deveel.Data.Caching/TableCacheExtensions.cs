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

using Deveel.Data.Sql;

namespace Deveel.Data.Caching {
	public static class TableCacheExtensions {
		public static bool TryGetValue(this ITableCellCache cache, int tableId, int rowNumber, int columnOffset,
			out DataObject value) {
			var rowId = new RowId(tableId, rowNumber);
			return cache.TryGetValue(rowId, columnOffset, out value);
		}

		public static void Set(this ITableCellCache cache, int tableId, int rowNumber, int columnOffset, DataObject value) {
			var rowId = new RowId(tableId, rowNumber);
			cache.Set(new CachedCell(rowId, columnOffset, value));
		}
	}
}
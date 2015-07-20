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
using System.Collections.Generic;

using Deveel.Data.Caching;
using Deveel.Data.Configuration;
using Deveel.Data.Routines;
using Deveel.Data.Sql.Query;
using Deveel.Data.Store;

namespace Deveel.Data.DbSystem {
	public static class DatabaseContextExtensions {
		public static Type StorageSystemType(this IDatabaseContext context) {
			var value = context.Configuration.GetString(DatabaseConfigKeys.StorageSystem);
			if (String.IsNullOrEmpty(value))
				return null;

			if (String.Equals(value, DefaultStorageSystemNames.File))
				throw new NotImplementedException();

			if (String.Equals(value, DefaultStorageSystemNames.SingleFile))
				throw new NotSupportedException();

			if (string.Equals(value, DefaultStorageSystemNames.Heap))
				return typeof (InMemoryStorageSystem);

			return Type.GetType(value, false, true);
		}

		public static bool ReadOnly(this IDatabaseContext context) {
			return context.Configuration.GetBoolean(SystemConfigKeys.ReadOnly);
		}

		public static bool DeleteOnClose(this IDatabaseContext context) {
			return context.Configuration.GetBoolean(DatabaseConfigKeys.DeleteOnClose);
		}

		public static string DatabaseName(this IDatabaseContext context) {
			return context.Configuration.GetString(DatabaseConfigKeys.DatabaseName);
		}

		public static string DefaultSchema(this IDatabaseContext context) {
			return context.Configuration.GetString(DatabaseConfigKeys.DefaultSchema);
		}

		public static bool AutoCommit(this IDatabaseContext context) {
			return context.SystemContext.AutoCommit();
		}

		public static bool IgnoreIdentifiersCase(this IDatabaseContext context) {
			return context.SystemContext.IgnoreIdentifiersCase();
		}

		public static Type QueryPlannerType(this IDatabaseContext context) {
			var value = context.Configuration.GetString(DatabaseConfigKeys.QueryPlanner);
			if (String.IsNullOrEmpty(value))
				return typeof (QueryPlanner);


			return Type.GetType(value, false, true);
		}

		public static IQueryPlanner QueryPlanner(this IDatabaseContext context) {
			var type = context.QueryPlannerType();
			if (type == typeof (QueryPlanner))
				return new QueryPlanner();

			if (context.SystemContext.ServiceProvider == null ||
				!typeof(IQueryPlanner).IsAssignableFrom(type))
				return null;

			return context.SystemContext.ServiceProvider.Resolve(type)  as IQueryPlanner;
		}

		public static int CellCacheMaxSize(this IDatabaseContext context) {
			return context.Configuration.GetInt32(DatabaseConfigKeys.CellCacheMaxSize);
		}

		public static int CellCacheMaxCellSize(this IDatabaseContext context) {
			return context.Configuration.GetInt32(DatabaseConfigKeys.CellCacheMaxCellSize);
		}

		public static Type CellCacheType(this IDatabaseContext context) {
			var typeString = context.Configuration.GetString(DatabaseConfigKeys.CellCacheType);
			if (String.IsNullOrEmpty(typeString))
				return typeof (MemoryCache);

			return Type.GetType(typeString, false, true);
		}
	}
}

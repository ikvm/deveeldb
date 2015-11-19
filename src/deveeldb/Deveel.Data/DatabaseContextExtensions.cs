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
using Deveel.Data.Services;
using Deveel.Data.Sql.Query;
using Deveel.Data.Store;
#if !PCL
using Deveel.Data.Store.Journaled;
#endif

namespace Deveel.Data {
	public static class DatabaseContextExtensions {
		public static bool ReadOnly(this IDatabaseContext context) {
			return context.Configuration.GetBoolean("system.readOnly", false);
		}

		public static bool DeleteOnClose(this IDatabaseContext context) {
			return context.Configuration.GetBoolean("system.deleteOnClose", false);
		}

		public static string DatabaseName(this IDatabaseContext context) {
			return context.Configuration.GetString("database.name");
		}

		public static string DefaultSchema(this IDatabaseContext context) {
			return context.Configuration.GetString("database.defaultSchema", "APP");
		}

		public static bool AutoCommit(this IDatabaseContext context) {
			return context.SystemContext.AutoCommit();
		}

		public static bool IgnoreIdentifiersCase(this IDatabaseContext context) {
			return context.SystemContext.IgnoreIdentifiersCase();
		}

		public static IQueryPlanner QueryPlanner(this IDatabaseContext context) {
			return context.SystemContext.QueryPlanner();
		}

		public static ITableCellCache TableCellCache(this IDatabaseContext context) {
			return context.SystemContext.TableCellCache();
		}

#region Services

		//public static object ResolveService(this IDatabaseContext context, Type serviceType) {
		//	return ResolveService(context, serviceType, null);
		//}

		//public static object ResolveService(this IDatabaseContext context, Type serviceType, string name) {
		//	var scope = context as IResolveScope;
		//	return context.SystemContext.ResolveService(serviceType, name, scope);
		//}

		//public static TService ResolveService<TService>(this IDatabaseContext context) {
		//	return ResolveService<TService>(context, null);
		//}

		//public static TService ResolveService<TService>(this IDatabaseContext context, string name) {
		//	var scope = context as IResolveScope;
		//	return context.SystemContext.ResolveService<TService>(name, scope);
		//}

#endregion
	}
}
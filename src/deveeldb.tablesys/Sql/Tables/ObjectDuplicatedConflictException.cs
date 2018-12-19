﻿// 
//  Copyright 2010-2018 Deveel
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

using Deveel.Data.Transactions;

namespace Deveel.Data.Sql.Tables
{
	public sealed class ObjectDuplicatedConflictException : TransactionException
	{
		public ObjectDuplicatedConflictException(ObjectName objectName, string conflictType)
			: this(objectName, conflictType, $"Duplication conflict of type '{conflictType}' on object '{objectName}'") {
		}

		public ObjectDuplicatedConflictException(ObjectName objectName, string conflictType, string message)
			: base(message) {
			ObjectName = objectName;
			ConflictType = conflictType;
		}

		public ObjectName ObjectName { get; }

		public string ConflictType { get; }
	}
}
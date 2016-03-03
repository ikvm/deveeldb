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

using Deveel.Data.Serialization;
using Deveel.Data.Transactions;

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public sealed class SetIsolationLevelStatement : SqlStatement {
		public SetIsolationLevelStatement(IsolationLevel isolationLevel) {
			IsolationLevel = isolationLevel;
		}

		private SetIsolationLevelStatement(ObjectData data) {
			IsolationLevel = (IsolationLevel) data.GetValue<int>("IsolationLevel");
		}

		public IsolationLevel IsolationLevel { get; private set; }

		protected override void GetData(SerializeData data) {
			data.SetValue("IsolationLevel", (int)IsolationLevel);
		}

		protected override void ExecuteStatement(ExecutionContext context) {
			// TODO: chage the isolation level into the parent transaction
			base.ExecuteStatement(context);
		}
	}
}

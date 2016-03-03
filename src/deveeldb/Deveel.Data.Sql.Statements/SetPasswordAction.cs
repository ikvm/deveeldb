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
using System.IO;

using Deveel.Data.Serialization;
using Deveel.Data.Sql.Expressions;

namespace Deveel.Data.Sql.Statements {
	[Serializable]
	public sealed class SetPasswordAction : IAlterUserAction, IPreparable {
		public SetPasswordAction(SqlExpression passwordExpression) {
			if (passwordExpression == null)
				throw new ArgumentNullException("passwordExpression");

			PasswordExpression = passwordExpression;
		}

		private SetPasswordAction(ObjectData data) {
			PasswordExpression = data.GetValue<SqlExpression>("Password");
		}

		public AlterUserActionType ActionType {
			get { return AlterUserActionType.SetPassword; }
		}

		public SqlExpression PasswordExpression { get; private set; }

		object IPreparable.Prepare(IExpressionPreparer preparer) {
			var preparedExp = PasswordExpression.Prepare(preparer);
			return new SetPasswordAction(preparedExp);
		}

		void ISerializable.GetData(SerializeData data) {
			data.SetValue("Password", PasswordExpression);
		}

		//public static void Serialize(SetPasswordAction action, BinaryWriter writer) {
		//	SqlExpression.Serialize(action.PasswordExpression, writer);
		//}

		//public static SetPasswordAction Deserialize(BinaryReader reader) {
		//	var exp = SqlExpression.Deserialize(reader);
		//	return new SetPasswordAction(exp);
		//}
	}
}

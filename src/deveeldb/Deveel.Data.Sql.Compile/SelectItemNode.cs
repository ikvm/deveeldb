﻿// 
//  Copyright 2010-2014 Deveel
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
using System.Linq;

namespace Deveel.Data.Sql.Compile {
	[Serializable]
	public sealed class SelectItemNode : SqlNode {
		public ObjectNameNode Name { get; private set; }

		public IExpressionNode Expression { get; private set; }

		public string Alias { get; private set; }

		protected override ISqlNode OnChildNode(ISqlNode node) {
			if (node.NodeName == "select_source") {
				var source = node.ChildNodes.First();
				if (source is IExpressionNode) {
					Expression = (IExpressionNode) source;
				} else if (source is ObjectNameNode) {
					Name = (ObjectNameNode) source;
				}
			} else if (node is IdentifierNode) {
				Alias = ((IdentifierNode) node).Text;
			}

			return base.OnChildNode(node);
		}
	}
}
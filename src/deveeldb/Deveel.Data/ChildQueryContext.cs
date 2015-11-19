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

namespace Deveel.Data {
	class ChildQueryContext : QueryContextBase {
		private IQueryContext parentContext;

		public ChildQueryContext(IQueryContext parentContext)
			: base(parentContext.Session()) {
			if (parentContext == null)
				throw new ArgumentNullException("parentContext");

			this.parentContext = parentContext;
		}

		public override IQueryContext ParentQueryContext {
			get { return parentContext; }
		}

		protected override void Dispose(bool disposing) {
			parentContext = null;
			base.Dispose(disposing);
		}
	}
}
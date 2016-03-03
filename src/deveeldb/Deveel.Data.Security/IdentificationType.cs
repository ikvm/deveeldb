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

namespace Deveel.Data.Security {
	/// <summary>
	/// The mechanism used for identifying a user in a database.
	/// </summary>
	public enum IdentificationType {
		/// <summary>
		/// This is a plain-text password defined by the user.
		/// </summary>
		Password = 1,

		/// <summary>
		/// The hash of a password obtained from the configured
		/// hash mechanism, given an input password from the user.
		/// </summary>
		Hash = 2,

		/// <summary>
		/// A configured external mechanism that takes an input
		/// string and authenticates a user.
		/// </summary>
		External = 3
	}
}

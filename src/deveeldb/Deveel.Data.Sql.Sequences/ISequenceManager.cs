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

using Deveel.Data;
using Deveel.Data.Sql.Sequences;
using Deveel.Data.Sql.Tables;
using Deveel.Data.Transactions;

namespace Deveel.Data.Sql {
	/// <summary>
	/// Manages the sequences within an isolated context.
	/// </summary>
	/// <seealso cref="SequenceManager"/>
	public interface ISequenceManager : IObjectManager {
		/// <summary>
		/// Provides a table container that exposes the sequences managed as tables.
		/// </summary>
		ITableContainer TableContainer { get; }

		///// <summary>
		///// Creates a new sequence withing the underlying context.
		///// </summary>
		///// <param name="sequenceInfo">The configuration information of the sequence to create</param>
		///// <returns>
		///// Returns a new instance of <see cref="ISequence"/> generated by the given configuration
		///// and compatible with this manager.
		///// </returns>
		//ISequence CreateSequence(SequenceInfo sequenceInfo);

		///// <summary>
		///// Drops a sequence identified by the given name from the underlying context
		///// </summary>
		///// <param name="sequenceName">The unique name of the sequnce to drop.</param>
		///// <returns>
		///// Returns <c>true</c> if the sequence was found and dropped, otherwise <c>false</c>.
		///// </returns>
		//bool DropSequence(ObjectName sequenceName);

		///// <summary>
		///// Checks if any sequence with the given name exists.
		///// </summary>
		///// <param name="sequenceName"></param>
		///// <returns></returns>
		//bool SequenceExists(ObjectName sequenceName);

		///// <summary>
		///// Gets a sequence identified by the given name.
		///// </summary>
		///// <param name="sequenceName">The name of the sequence to return.</param>
		///// <returns>
		///// Returns an instance of <see cref="ISequence"/> for the given name.
		///// </returns>
		//ISequence GetSequence(ObjectName sequenceName);
	}
}

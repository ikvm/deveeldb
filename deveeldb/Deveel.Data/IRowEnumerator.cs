//  
//  IRowEnumerator.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
//       Tobias Downer <toby@mckoi.com>
// 
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections;

namespace Deveel.Data {
	/// <summary>
	/// Allows for access to a tables rows.
	/// </summary>
	/// <remarks>
	/// Each call to <see cref="RowIndex"/> returns an <see cref="Int32"/> that 
	/// can be used in the <see cref="Table.GetCellContents(Int32, Int32)"/>.
	/// </remarks>
	public interface IRowEnumerator : IEnumerator {
		/// <summary>
		/// Gets the current row index of the enumeration.
		/// </summary>
		int RowIndex { get; }
	}
}
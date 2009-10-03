// 
//  StringObject.cs
//  
//  Author:
//       Antonello Provenzano <antonello@deveel.com>
//       Tobias Downer <toby@mckoi.com>
//  
//  Copyright (c) 2009 Deveel
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.IO;

namespace Deveel.Data {
	/// <summary>
	/// A concrete implementation of <see cref="IStringAccessor"/> 
	/// that uses a <see cref="string"/> object.
	/// </summary>
	[Serializable]
	public class StringObject : IStringAccessor {

		/// <summary>
		/// The <see cref="string"/> object.
		/// </summary>
		private readonly String str;

		private StringObject(String str) {
			this.str = str;
		}

		/// <summary>
		/// Returns the length of the string.
		/// </summary>
		public int Length {
			get { return str.Length; }
		}

		/// <summary>
		/// Returns a <see cref="TextReader"/> that can read 
		/// from the string.
		/// </summary>
		/// <returns></returns>
		public TextReader GetTextReader() {
			return new StringReader(str);
		}

		public override String ToString() {
			return str;
		}

		/// <summary>
		/// Static method that returns a <see cref="StringObject"/> from the 
		/// given <see cref="string"/>.
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
		public static StringObject FromString(String str) {
			return str != null ? new StringObject(str) : null;
		}
	}
}
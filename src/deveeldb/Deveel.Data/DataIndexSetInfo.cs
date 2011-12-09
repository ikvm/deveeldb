// 
//  Copyright 2010  Deveel
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
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace Deveel.Data {
	/// <summary>
	/// Represents the meta-data for a set of indexes of a table.
	/// </summary>
	public sealed class DataIndexSetInfo : ICloneable, IEnumerable<DataIndexInfo> {
		/// <summary>
		/// The TableName this index set meta data is for.
		/// </summary>
		private readonly TableName tableName;
		/// <summary>
		/// The list of indexes in the table.
		/// </summary>
		private List<DataIndexInfo> indexList;

		/// <summary>
		/// True if this object is immutable.
		/// </summary>
		private bool readOnly;

		///<summary>
		///</summary>
		///<param name="tableName"></param>
		public DataIndexSetInfo(TableName tableName) {
			this.tableName = tableName;
			indexList = new List<DataIndexInfo>();
		}

		///<summary>
		/// Adds a DataIndexInfo to this table.
		///</summary>
		///<param name="info"></param>
		///<exception cref="Exception"></exception>
		public void AddIndex(DataIndexInfo info) {
			if (IsReadOnly)
				throw new Exception("Tried to add index to immutable info.");

			indexList.Add(info);
		}

		/// <summary>
		/// Removes an index from the set at the given position.
		/// </summary>
		/// <param name="i">Pointer to the index to remove.</param>
		/// <exception cref="DatabaseException">
		/// If the set is in read-only mode.
		/// </exception>
		public void RemoveIndex(int i) {
			if (IsReadOnly)
				throw new Exception("Tried to add index to immutable info.");

			indexList.RemoveAt(i);
		}

		/// <summary>
		/// Gets the number of <see cref="DataIndexInfo"/> object in the set.
		/// </summary>
		public int IndexCount {
			get { return indexList.Count; }
		}

		/// <summary>
		/// True if this object is immutable.
		/// </summary>
		public bool IsReadOnly {
			get { return readOnly; }
			set { readOnly = value; }
		}

		/// <summary>
		/// Gets the index at the given position within the set.
		/// </summary>
		/// <param name="i">Position of the index to get.</param>
		/// <returns>
		/// Returns a <see cref="DataIndexInfo"/> located at the position 
		/// indicated by <paramref name="i"/>.
		/// </returns>
		/// <exception cref="System.IndexOutOfRangeException">If the given <paramref name="i"/>
		/// is out of range.</exception>
		public DataIndexInfo this[int i] {
			get { return indexList[i]; }
		}

		/// <summary>
		/// Gets the position of a named index within the set.
		/// </summary>
		/// <param name="indexName"></param>
		/// <returns>
		/// Returns an integer pointer to the index with the given <paramref name="indexName"/>
		/// if found, otherwise -1.
		/// </returns>
		/// <exception cref="System.ArgumentNullException">If the given <paramref name="indexName"/>
		/// is <b>null</b>.</exception>
		public int FindIndexWithName(string indexName) {
			int i = 0;
			foreach (DataIndexInfo indexInfo in indexList) {
				if (indexInfo.Name.Equals(indexName))
					return i;
				i++;
			}
			return -1;
		}

		/// <summary>
		/// Finds the index for the given column names.
		/// </summary>
		/// <param name="columnNames">The column name list to search the index.</param>
		/// <remarks>
		/// This method fails if <paramref name="columnNames"/> order differs
		/// from the defined order <see cref="DataIndexInfo.ColumnNames"/>.
		/// </remarks>
		/// <returns>
		/// Returns an integer pointer to the index for the given <paramref name="columnNames"/>
		/// if found, otherwise -1.
		/// </returns>
		public int FindIndexForColumns(string[] columnNames) {
			int sz = IndexCount;
			for (int i = 0; i < sz; ++i) {
				string[] t_cols = this[i].ColumnNames;
				if (t_cols.Length == columnNames.Length) {
					bool passed = true;
					for (int n = 0; n < t_cols.Length && passed; ++n) {
						if (!t_cols[n].Equals(columnNames[n])) {
							passed = false;
						}
					}
					if (passed) {
						return i;
					}
				}
			}
			return -1;
		}

		///<summary>
		/// Returns the <see cref="DataIndexInfo"/> with the given name or 
		/// null if it couldn't be found.
		///</summary>
		///<param name="indexName"></param>
		///<returns></returns>
		public DataIndexInfo IndexWithName(string indexName) {
			int i = FindIndexWithName(indexName);
			if (i != -1)
				return this[i];
			return null;
		}

		/// <summary>
		/// Attempts to resolve the given <paramref name="indexName"/> from the 
		/// index in the set.
		/// </summary>
		/// <param name="indexName">Index name to resolve.</param>
		/// <param name="ignoreCase">Indicates if the resolving should be
		/// in case-sensitive mode.</param>
		/// <returns>
		/// Returns a <see cref="System.String"/> for the resolved index name.
		/// </returns>
		/// <exception cref="DatabaseException">
		/// If none index or if multiple references found for the given 
		/// <paramref name="indexName"/>.
		/// </exception>
		public string ResolveIndexName(string indexName, bool ignoreCase) {
			string found = null;
			foreach (DataIndexInfo indexInfo in indexList) {
				string curIndexName = indexInfo.Name;
				if (String.Compare(curIndexName, indexName, ignoreCase) == 0) {
					if (found != null)
						throw new DatabaseException("Ambigious index name '" + indexName + "'");

					found = curIndexName;
				}
			}
			if (found == null)
				throw new DatabaseException("Index '" + indexName + "' not found.");

			return found;
		}

		///<summary>
		/// Writes this <see cref="DataIndexSetInfo"/> object to the given 
		/// <see cref="BinaryWriter"/>.
		///</summary>
		///<param name="dout"></param>
		public void Write(BinaryWriter dout) {
			dout.Write(1);
			dout.Write(tableName.Schema);
			dout.Write(tableName.Name);
			dout.Write(indexList.Count);
			foreach (DataIndexInfo indexInfo in indexList) {
				indexInfo.Write(dout);
			}
		}

		///<summary>
		/// Reads the <see cref="DataIndexSetInfo"/> object from the given 
		/// <see cref="BinaryReader"/>.
		///</summary>
		///<param name="din"></param>
		///<returns></returns>
		///<exception cref="IOException"></exception>
		public static DataIndexSetInfo Read(BinaryReader din) {
			int version = din.ReadInt32();
			if (version != 1)
				throw new IOException("Don't understand version.");

			string schema = din.ReadString();
			string name = din.ReadString();
			int sz = din.ReadInt32();
			DataIndexSetInfo indexSet = new DataIndexSetInfo(new TableName(schema, name));
			for (int i = 0; i < sz; ++i) {
				indexSet.AddIndex(DataIndexInfo.Read(din));
			}

			return indexSet;
		}

		object ICloneable.Clone() {
			return Clone();
		}

		public DataIndexSetInfo Clone() {
			DataIndexSetInfo clone = new DataIndexSetInfo(tableName);
			clone.indexList = new List<DataIndexInfo>();
			foreach (DataIndexInfo indexInfo in indexList) {
				clone.indexList.Add(indexInfo.Clone());
			}

			return clone;
		}

		public IEnumerator<DataIndexInfo> GetEnumerator() {
			return indexList.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
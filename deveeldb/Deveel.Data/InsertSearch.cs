// 
//  InsertSearch.cs
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

using Deveel.Data.Collections;
using Deveel.Data.Util;

namespace Deveel.Data {
	/// <summary>
	/// This is a <see cref="SelectableScheme"/> similar in some ways 
	/// to the binary tree.
	/// </summary>
	/// <remarks>
	/// When a new row is added, it is inserted into a sorted list of rows.  
	/// We can then use this list to select out the sorted list of elements.
	/// <para>
	/// This requires less memory than the BinaryTree, however it is not as fast.
	/// Even though, it should still perform fairly well on medium size data sets.
	/// On large size data sets, insert and remove performance may suffer.
	/// </para>
	/// <para>
	/// This object retains knowledge of all set elements unlike BlindSearch which 
	/// has no memory overhead.
	/// </para>
	/// <para>
	/// Performance should be very comparable to BinaryTree for sets that aren't 
	/// altered much.
	/// </para>
	/// </remarks>
	public sealed class InsertSearch : CollatedBaseSearch {
		/// <summary>
		/// The sorted list of rows in this set.
		/// </summary>
		/// <remarks>
		/// This is sorted from min to max (not sorted by row number - sorted 
		/// by entity row value).
		/// </remarks>
		private IIntegerList set_list;

		/// <summary>
		/// If this is true, then this <see cref="SelectableScheme"/> records additional 
		/// rid information that can be used to very quickly identify whether a value is 
		/// greater, equal or less.
		/// </summary>
		internal bool RECORD_UID;

		/// <summary>
		/// The <see cref="IIndexComparer"/> that we use to refer elements in the set to 
		/// actual data objects.
		/// </summary>
		private IIndexComparer set_comparator;


		// ----- DEBUGGING -----

		/// <summary>
		/// If this is immutable, this stores the number of entries in <see cref="set_list"/> 
		/// when this object was made.
		/// </summary>
		private readonly int DEBUG_immutable_set_size;




		public InsertSearch(ITableDataSource table, int column)
			: base(table, column) {
			set_list = new BlockIntegerList();

			// The internal comparator that enables us to sort and lookup on the data
			// in this column.
			SetupComparer();
		}


		/// <summary>
		/// Constructor sets the scheme with a pre-sorted list.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="column"></param>
		/// <param name="vec">A sorted list, with a low to high direction order, that is used to
		/// set the scheme. This should not be used again after it is passed to this constructor.</param>
		public InsertSearch(ITableDataSource table, int column, IntegerVector vec)
			: this(table, column) {
			for (int i = 0; i < vec.Count; ++i) {
				set_list.Add(vec[i]);
			}

			// NOTE: This must be removed in final, this is a post condition check to
			//   make sure 'vec' is infact sorted
			//checkSchemeSorted();

		}

		/// <summary>
		/// Constructor sets the scheme with a pre-sorted list.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="column"></param>
		/// <param name="list">A sorted list, with a low to high direction order, that is used to
		/// set the scheme. This should not be used again after it is passed to this constructor.</param>
		internal InsertSearch(ITableDataSource table, int column, IIntegerList list)
			: this(table, column) {
			this.set_list = list;

			// NOTE: This must be removed in final, this is a post condition check to
			//   make sure 'vec' is infact sorted
			//checkSchemeSorted();

		}

		/// <summary>
		/// Constructs this as a copy of the given, either mutable or immutable copy.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="from"></param>
		/// <param name="immutable"></param>
		private InsertSearch(ITableDataSource table, InsertSearch from, bool immutable)
			: base(table, from.Column) {

			if (immutable) {
				SetImmutable();
			}

			if (immutable) {
				// Immutable is a shallow copy
				set_list = from.set_list;
				DEBUG_immutable_set_size = set_list.Count;
			} else {
				set_list = new BlockIntegerList(from.set_list);
			}

			// Do we generate lookup caches?
			RECORD_UID = from.RECORD_UID;

			// The internal comparator that enables us to sort and lookup on the data
			// in this column.
			SetupComparer();

		}

		/// <summary>
		/// Sets the internal comparator that enables us to sort and lookup on the
		/// data in this column.
		/// </summary>
		private void SetupComparer() {
			set_comparator = new IndexComparatorImpl(this);
		}


		private class IndexComparatorImpl : IIndexComparer {
			private readonly InsertSearch scheme;

			public IndexComparatorImpl(InsertSearch scheme) {
				this.scheme = scheme;
			}

			private int InternalCompare(int index, TObject cell2) {
				TObject cell1 = scheme.GetCellContents(index);
				return cell1.CompareTo(cell2);
			}

			public int Compare(int index, Object val) {
				return InternalCompare(index, (TObject)val);
			}
			public int Compare(int index1, int index2) {
				TObject cell = scheme.GetCellContents(index2);
				return InternalCompare(index1, cell);
			}

			#region Implementation of IComparer

			public int Compare(object x, object y) {
				return y is int ? Compare((int) x, (int) y) : Compare((int) x, y);
			}

			#endregion
		}


		/// <summary>
		/// Inserts a row into the list.
		/// </summary>
		/// <param name="row"></param>
		/// <remarks>
		/// This will always be thread safe, table changes cause a Write Lock which 
		/// prevents reads while we are writing to the table.
		/// </remarks>
		internal override void Insert(int row) {
			if (IsImmutable) {
				throw new ApplicationException("Tried to change an immutable scheme.");
			}

			TObject cell = GetCellContents(row);
			set_list.InsertSort(cell, row, set_comparator);

		}

		/// <summary>
		/// Removes a row from the list.
		/// </summary>
		/// <param name="row"></param>
		/// <remarks>
		/// This will always be thread safe, table changes cause a Write Lock which 
		/// prevents reads while we are writing to the table.
		/// </remarks>
		internal override void Remove(int row) {
			if (IsImmutable) {
				throw new ApplicationException("Tried to change an immutable scheme.");
			}

			TObject cell = GetCellContents(row);
			int removed = set_list.RemoveSort(cell, row, set_comparator);

			if (removed != row) {
				throw new ApplicationException("Removed value different than row asked to remove.  " +
								"To remove: " + row + "  Removed: " + removed);
			}
		}

		/// <summary>
		/// Reads the entire state of the scheme from the input stream.
		/// </summary>
		/// <param name="input"></param>
		/// <exception cref="Exception">
		/// Thrown if the scheme is not empty.
		/// </exception>
		public override void ReadFrom(InputStream input) {
			if (set_list.Count != 0) {
				throw new Exception("Error reading scheme, already a set in the Scheme");
			}
			BinaryReader din = new BinaryReader(input);
			int vec_size = din.ReadInt32();

			int row_count = Table.RowCount;
			// Check we Read in as many indices as there are rows in the table
			if (row_count != vec_size) {
				throw new IOException(
				   "Different table row count to indices in scheme. " +
				   "table=" + row_count +
				   ", vec_size=" + vec_size);
			}

			for (int i = 0; i < vec_size; ++i) {
				int row = din.ReadInt32();
				if (row < 0) { // || row >= row_count) {
					set_list = new BlockIntegerList();
					throw new IOException("Scheme contains out of table bounds index.");
				}
				set_list.Add(row);
			}

			System.Stats.Add(vec_size, "{session} InsertSearch.read_indices");

			// NOTE: This must be removed in final, this is a post condition check to
			//   make sure 'vec' is infact sorted
			//checkSchemeSorted();
		}

		/// <summary>
		/// Writes the entire state of the scheme to the output stream.
		/// </summary>
		/// <param name="output"></param>
		public override void WriteTo(Stream output) {
			BinaryWriter dout = new BinaryWriter(output);
			int list_size = set_list.Count;
			dout.Write(list_size);

			IIntegerIterator i = set_list.GetIterator(0, list_size - 1);
			while (i.MoveNext()) {
				dout.Write(i.next());
			}
		}

		/// <summary>
		/// Returns an exact copy of this scheme including any optimization
		/// information.
		/// </summary>
		/// <param name="table"></param>
		/// <param name="immutable"></param>
		/// <remarks>
		/// The copied scheme is identical to the original but does not share any 
		/// parts. Modifying any part of the copied scheme will have no effect on 
		/// the original and vice versa.
		/// </remarks>
		/// <returns></returns>
		public override SelectableScheme Copy(ITableDataSource table, bool immutable) {
			// ASSERTION: If immutable, check the size of the current set is equal to
			//   when the scheme was created.
			if (IsImmutable) {
				if (DEBUG_immutable_set_size != set_list.Count) {
					throw new ApplicationException("Assert failed: " +
									"Immutable set size is different from when created.");
				}
			}

			// We must create a new InsertSearch object and copy all the state
			// information from this object to the new one.
			return new InsertSearch(table, this, immutable);
		}

		/// <summary>
		/// Disposes this scheme.
		/// </summary>
		public override void Dispose() {
			// Close and invalidate.
			set_list = null;
			set_comparator = null;
		}

		// ---------- Implemented/Overwritten from CollatedBaseSearch ----------

		protected override int SearchFirst(TObject val) {
			return set_list.SearchFirst(val, set_comparator);
		}

		protected override int SearchLast(TObject val) {
			return set_list.SearchLast(val, set_comparator);
		}

		protected override int SetSize {
			get { return set_list.Count; }
		}

		protected override TObject FirstInCollationOrder {
			get { return GetCellContents(set_list[0]); }
		}

		protected override TObject LastInCollationOrder {
			get { return GetCellContents(set_list[SetSize - 1]); }
		}

		protected override IntegerVector AddRangeToSet(int start, int end,
											  IntegerVector ivec) {
			if (ivec == null) {
				ivec = new IntegerVector((end - start) + 2);
			}
			IIntegerIterator i = set_list.GetIterator(start, end);
			while (i.MoveNext()) {
				ivec.AddInt(i.next());
			}
			return ivec;
		}

		public override IntegerVector SelectAll() {
			IntegerVector ivec = new IntegerVector(set_list);
			return ivec;
		}

	}
}
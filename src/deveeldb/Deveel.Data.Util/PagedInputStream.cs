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
using System.IO;

namespace Deveel.Data.Util {
	/// <summary>
	/// A <see cref="Stream"/> that reads data from an underlying
	/// representation in fixed sized pages.
	/// </summary>
	/// <remarks>
	/// This object maintains a single buffer that is the size of a page.
	/// <para>
	/// This implementation supports <see cref="Mark"/> and buffered access 
	/// to the data.
	/// </para>
	/// <para>
	/// The only method that needs to be implemented is the <see cref="ReadPageContent"/>
	/// method.
	/// </para>
	/// </remarks>
	public abstract class PagedInputStream : Stream {
		/// <summary>
		/// The size of the buffer page.
		/// </summary>
		private readonly int BUFFER_SIZE;

		/// <summary>
		/// The current position in the stream.
		/// </summary>
		private long position;

		/// <summary>
		/// The total size of the underlying dataset.
		/// </summary>
		private readonly long size;

		/// <summary>
		/// The start buffer position.
		/// </summary>
		private long buffer_pos;

		/// <summary>
		/// The buffer.
		/// </summary>
		private readonly byte[] buf;

		/// <summary>
		/// Constructs the input stream.
		/// </summary>
		/// <param name="page_size">The size of the pages when accessing 
		/// the underlying stream.</param>
		/// <param name="total_size">The total size of the underlying 
		/// data set.</param>
		protected PagedInputStream(int page_size, long total_size) {
			BUFFER_SIZE = page_size;
			position = 0;
			size = total_size;
			buf = new byte[BUFFER_SIZE];
			buffer_pos = -1;
		}

		/// <summary>
		/// Reads the page at the given offset in the underlying data into the 
		/// given byte[] array.
		/// </summary>
		/// <param name="buf"></param>
		/// <param name="pos">The starting position within the stream where to 
		/// start to read. is guarenteed to be a multiple of 
		/// <see cref="BUFFER_SIZE">buffer size</see></param>
		/// <param name="length">The number of bytes to read from the page. This
		/// must be either equal or smaller than <see cref="BUFFER_SIZE">buffer size</see>,
		/// if the page to read contains the end of the stream.</param>
		/// <remarks>
		/// </remarks>
		protected abstract void ReadPageContent(byte[] buf, long pos, int length);

		/// <summary>
		/// Fills the buffer with data from the blob at the given position.
		/// </summary>
		/// <param name="pos"></param>
		/// <remarks>
		/// A buffer may be partially filled if the end is reached.
		/// </remarks>
		private void FillBuffer(long pos) {
			long read_pos = (pos / BUFFER_SIZE) * BUFFER_SIZE;
			int to_read = (int)System.Math.Min((long)BUFFER_SIZE, (size - read_pos));
			if (to_read > 0) {
				ReadPageContent(buf, read_pos, to_read);
				buffer_pos = read_pos;
			}
		}

		// ---------- Implemented from InputStream ----------

		/// <inheritdoc/>
		public override bool CanSeek {
			get { return true; }
		}

		public override bool CanRead {
			get { return true; }
		}

		public override bool CanWrite {
			get { return false; }
		}

		/// <inheritdoc/>
		public override long Position {
			get { return position; }
			set { throw new NotSupportedException(); }
		}

		/// <inheritdoc/>
		public override long Length {
			get { return size; }
		}

		/// <inheritdoc/>
		public override void SetLength(long value) {
			throw new NotSupportedException();
		}

		public override void Write(byte[] buffer, int offset, int count) {
			throw new  NotSupportedException();
		}

		public override void Flush() {
		}

		/// <inheritdoc/>
		public override int ReadByte() {
			if (position >= size) {
				return 0;
			}

			if (buffer_pos == -1) {
				FillBuffer(position);
			}

			int p = (int)(position - buffer_pos);
			int v = ((int)buf[p]) & 0x0FF;

			++position;
			// Fill the next part of the buffer?
			if (p + 1 >= BUFFER_SIZE) {
				FillBuffer(buffer_pos + BUFFER_SIZE);
			}

			return v;
		}

		/// <inheritdoc/>
		public override int Read(byte[] read_buf, int off, int len) {
			if (len <= 0)
				throw new ArgumentException();

			if (buffer_pos == -1) {
				FillBuffer(position);
			}

			int p = (int)(position - buffer_pos);
			long buffer_end = System.Math.Min(buffer_pos + BUFFER_SIZE, size);
			int to_read = (int)System.Math.Min((long)len, buffer_end - position);
			if (to_read <= 0) {
				return 0;
			}
			int has_read = 0;
			while (to_read > 0) {
				Array.Copy(buf, p, read_buf, off, to_read);
				has_read += to_read;
				p += to_read;
				off += to_read;
				len -= to_read;
				position += to_read;
				if (p >= BUFFER_SIZE) {
					FillBuffer(buffer_pos + BUFFER_SIZE);
					p -= BUFFER_SIZE;
				}
				buffer_end = System.Math.Min(buffer_pos + BUFFER_SIZE, size);
				to_read = (int)System.Math.Min((long)len, buffer_end - position);
			}
			return has_read;
		}

		/// <inheritdoc/>
		public override long Seek(long offset, SeekOrigin origin) {
			if (offset < 0)
				throw new NotSupportedException("Backward seeking not supported.");

			if (origin == SeekOrigin.End)
				throw new NotSupportedException("Seeking from end of the stream is not yet supported.");

			if (origin == SeekOrigin.Begin && offset <= position)
				position = offset;
			if (origin == SeekOrigin.Current && offset + position <= size)
				position += offset;

			if (buffer_pos == -1 || (position - buffer_pos) > BUFFER_SIZE) {
				FillBuffer((position / BUFFER_SIZE) * BUFFER_SIZE);
			}			

			return position;
		}

		/// <inheritdoc/>
		public override void Close() {
		}
	}
}
//  
//  IDatabaseInterface.cs
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

namespace Deveel.Data.Client {
	///<summary>
	/// The interface with the <see cref="Database"/> whether it be remotely via 
	/// TCP/IP or locally within the current runtime.
	///</summary>
	public interface IDatabaseInterface : IDisposable {
		///<summary>
		/// Attempts to log in to the database as the given username with the 
		/// given password.
		///</summary>
		///<param name="default_schema"></param>
		///<param name="username"></param>
		///<param name="password"></param>
        ///<param name="call_back">A <see cref="IDatabaseCallBack"/> implementationthat 
        /// is notified of all events from the database. Events are only received if the 
        /// login was successful.</param>
		/// <remarks>
		/// Only one user may be authenticated per connection.
		/// <para>
        /// This must be called before the other methods are used.
		/// </para>
		/// </remarks>
		///<returns></returns>
		bool Login(String default_schema, String username, String password, IDatabaseCallBack call_back);

	    ///<summary>
	    /// Pushes a part of a streamable object from the client onto the server.
	    ///</summary>
	    ///<param name="type">The <see cref="StreamableObject"/> type (1 = byte array, 2 = char array)</param>
	    ///<param name="object_id">The identifier of the <see cref="StreamableObject"/> 
	    /// for future queries.</param>
	    ///<param name="object_length">The total length of the <see cref="StreamableObject"/>.</param>
	    ///<param name="buf">The byte array representing the block of information being sent.</param>
	    ///<param name="offset">The offset into of the object of this block.</param>
	    ///<param name="length">The length of the block being pushed.</param>
	    /// <remarks>
	    /// The server stores the large object for use with a future query. 
	    /// For example,a sequence of with a query with large objects may operate as follows:
	    /// <list type="number">
	    /// <item>Push 100 MB object (id = 104)</item>
	    /// <item><see cref="ExecuteQuery"/> with query that contains a streamable object 
	    /// with id 104</item>
	    /// </list>
	    /// <para>
	    /// The client may push any part of a streamable object onto the server, 
	    /// however the streamable object must have been completely pushed for the 
	    /// query to execute correctly.  For example, an 100 MB byte array may be 
	    /// pushed onto the server in blocks of 64K (in 1,600 separate blocks).
	    /// </para>
	    /// </remarks>
	    void PushStreamableObjectPart(byte type, long object_id, long object_length,
	                                  byte[] buf, long offset, int length);

		///<summary>
		/// Executes the query and returns a <see cref="IQueryResponse"/> object that 
		/// describes the result of the query.
		///</summary>
		///<param name="sql"></param>
		/// <remarks>
		/// This method will block until the query has completed. The <see cref="IQueryResponse"/> 
		/// can be used to obtain the 'result id' variable that is used in subsequent 
		/// queries to the engine to retrieve the actual result of the query.
		/// </remarks>
		///<returns></returns>
		IQueryResponse ExecuteQuery(SQLQuery sql);

		///<summary>
        /// Returns a part of a result set.
		///</summary>
		///<param name="result_id"></param>
		///<param name="row_number"></param>
		///<param name="row_count"></param>
		/// <remarks>
		/// The result set part is referenced via the <see cref="IQueryResponse.ResultId">result id</see> 
		/// found in the <see cref="IQueryResponse"/>. This is used to Read parts of the query 
		/// once it has been found via <see cref="ExecuteQuery"/>.
		/// <para>
        /// If the result contains any <see cref="StreamableObject"/> objects, then the 
        /// server allocates a channel to the object via the <see cref="GetStreamableObjectPart"/> 
        /// and the identifier of the <see cref="StreamableObject"/>.  The channel may 
        /// only be disposed if the <see cref="DisposeStreamableObject"/> method is called.
		/// </para>
		/// </remarks>
		///<returns></returns>
		ResultPart GetResultPart(int result_id, int row_number, int row_count);

		///<summary>
        /// Disposes of a result of a query on the server.
		///</summary>
		///<param name="result_id"></param>
		/// <remarks>
		/// This frees up server side resources allocated to a query. This should be 
		/// called when the <see cref="ResultSet"/> of a query closes. We should try 
		/// and use this method as soon as possible because it frees locks on tables 
		/// and allows deleted rows to be reclaimed.
		/// </remarks>
		void DisposeResult(int result_id);

	    ///<summary>
	    /// Returns a section of a large binary or character stream in a result set.
	    ///</summary>
	    ///<param name="result_id"></param>
	    ///<param name="streamable_object_id"></param>
	    ///<param name="offset"></param>
	    ///<param name="len"></param>
	    /// <remarks>
	    /// This is used to stream large values over the connection.  For example, if a 
	    /// row contained a multi megabyte object and the client is only interested in 
	    /// the first few characters and the last few characters of the stream.
	    /// This would require only a few queries to the database and the multi-megabyte 
	    /// object would not need to be downloaded to the client in its entirety.
	    /// </remarks>
	    ///<returns></returns>
	    StreamableObjectPart GetStreamableObjectPart(int result_id, long streamable_object_id, long offset, int len);

		///<summary>
        /// Disposes a streamable object channel with the given identifier.
		///</summary>
		///<param name="result_id"></param>
		///<param name="streamable_object_id"></param>
		/// <remarks>
		/// This should be called to free any resources on the server associated with the
		/// object.  It should be called as soon as possible because it frees locks on the
		/// tables and allows deleted rows to be reclaimed.
		/// </remarks>
		void DisposeStreamableObject(int result_id, long streamable_object_id);
	}
}
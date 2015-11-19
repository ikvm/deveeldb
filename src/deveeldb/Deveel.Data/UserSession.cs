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
using System.Collections.Generic;
using System.Linq;

using Deveel.Data.Diagnostics;
using Deveel.Data.Sql;
using Deveel.Data.Transactions;

namespace Deveel.Data {
	/// <summary>
	/// This is a session that is constructed around a given user and a transaction,
	/// to the given database.
	/// </summary>
	public sealed class UserSession : IUserSession {
		private List<LockHandle> lockHandles;
		private bool disposed;

		/// <summary>
		/// Constructs the session for the given user and transaction to the
		/// given database.
		/// </summary>
		/// <param name="database">The database to which the user is connected.</param>
		/// <param name="transaction">A transaction that handles the commands issued by
		/// the user during the session.</param>
		/// <param name="sessionInfo">The information about the session to be created (user, 
		/// connection endpoint, statistics, etc.)</param>
		/// <seealso cref="ITransaction"/>
		/// <seealso cref="SessionInfo"/>
		public UserSession(IDatabase database, ITransaction transaction, SessionInfo sessionInfo) {
			if (database == null)
				throw new ArgumentNullException("database");
			if (transaction == null)
				throw new ArgumentNullException("transaction");
			if (sessionInfo == null)
				throw new ArgumentNullException("sessionInfo");

			if (sessionInfo.User.IsSystem ||
				sessionInfo.User.IsPublic)
				throw new ArgumentException(String.Format("Cannot open a session for user '{0}'.", sessionInfo.User.Name));

			Database = database;
			Transaction = transaction;

			Database.DatabaseContext.Sessions.Add(this);

			SessionInfo = sessionInfo;
		}

		~UserSession() {
			Dispose(false);
		}

		public void Dispose() {
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		public string CurrentSchema {
			get { return Transaction.CurrentSchema(); }
		}

		public SessionInfo SessionInfo { get; private set; }

		IEventSource IEventSource.ParentSource {
			get { return null; }
		}

		IEnumerable<KeyValuePair<string, object>> IEventSource.Metadata {
			get { return GetMetadata(); }
		}

		private IEnumerable<KeyValuePair<string, object>> GetMetadata() {
			return new Dictionary<string, object> {
				{ EventMetadataKeys.UserName, SessionInfo.User.Name },
				{ EventMetadataKeys.Protocol, SessionInfo.EndPoint.Protocol }
			};
		}

		public ITransaction Transaction { get; private set; }

		private void AssertNotDisposed() {
			if (disposed)
				throw new ObjectDisposedException("UserSession");
		}

		public void Access(IEnumerable<IDbObject> objects, AccessType accessType) {
			if (Database == null)
				return;

			lock (Database) {
				var lockables = objects.OfType<ILockable>().ToArray();
				if (lockables.Length == 0)
					return;

				CheckAccess(lockables, accessType);

				var isolation = Transaction.Isolation;

				LockHandle handle;

				if (isolation == IsolationLevel.Serializable) {
					handle = Database.Locker().Lock(lockables, AccessType.ReadWrite, LockingMode.Exclusive);
				} else {
					throw new NotImplementedException(string.Format("The locking for isolation '{0}' is not implemented yet.", isolation));
				}

				if (handle != null) {
					if (lockHandles == null)
						lockHandles = new List<LockHandle>();

					lockHandles.Add(handle);
				}
			}
		}

		public void Exit(IEnumerable<IDbObject> objects, AccessType accessType) {
			// Only SERIALIZABLE isolation is supported, that means locks for read and write
			//    are acquired on access and released only at the end of the session/transaction
			throw new NotImplementedException("The Exit mechanism is not implemented");
		}

		public void Lock(IEnumerable<IDbObject> objects, AccessType accessType, LockingMode mode) {
			lock (Database) {
				var lockables = objects.OfType<ILockable>().ToArray();
				if (lockables.Length == 0)
					return;

				// Before we can lock the objects, we must wait for them
				//  to be available...
				CheckAccess(lockables, accessType);

				var handle = Database.Locker().Lock(lockables, accessType, mode);

				if (lockHandles == null)
					lockHandles = new List<LockHandle>();

				lockHandles.Add(handle);
			}
		}

		private void CheckAccess(ILockable[] lockables, AccessType accessType) {
			if (lockHandles == null || lockables == null)
				return;

			foreach (var handle in lockHandles) {
				foreach (var lockable in lockables) {
					if (handle.IsHandled(lockable))
						handle.CheckAccess(lockable, accessType);
				}
			}
		}

		private void ReleaseLocks() {
			if (Database == null)
				return;

			lock (Database) {
				if (lockHandles != null) {
					foreach (var handle in lockHandles) {
						if (handle != null)
							handle.Release();
					}
				}
			}
		}

		public IDatabase Database { get; private set; }

		public void Commit() {
			AssertNotDisposed();

			if (Transaction != null) {
				try {
					Transaction.Commit();
				} finally {
					SessionInfo.OnCommand();
					DisposeTransaction();
				}
			}
		}

		public void Rollback() {
			AssertNotDisposed();

			if (Transaction != null) {
				try {
					Transaction.Rollback();
				} finally {
					SessionInfo.OnCommand();
					DisposeTransaction();
				}
			}
		}

		private void DisposeTransaction() {
			ReleaseLocks();

			if (Database != null)
				Database.DatabaseContext.Sessions.Remove(this);

			Transaction = null;
			Database = null;
		}

		private void Dispose(bool disposing) {
			if (!disposed) {
				if (disposing) {
					try {
						Rollback();
					} catch (Exception) {
						// TODO: Notify the underlying system
					}
				}

				lockHandles = null;
				disposed = true;
			}
		}
	}
}
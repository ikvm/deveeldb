﻿using System;
using System.IO;

using NUnit.Framework;

namespace Deveel.Data.Client {
	[TestFixture]
	public sealed class LobTest : TestBase {
		protected override void OnSetUp() {
			DeveelDbConnection connection = CreateConnection();
			DeveelDbCommand command = connection.CreateCommand("CREATE TABLE IF NOT EXISTS LOB_TEST (Id INTEGER NOT NULL, Data BLOB)");
			command.ExecuteNonQuery();
			connection.Close();
		}

		protected override void OnTearDown() {
			DeveelDbConnection connection = CreateConnection();
			DeveelDbCommand command = connection.CreateCommand("DROP TABLE IF EXISTS LOB_TEST");
			command.ExecuteNonQuery();
			connection.Close();
		}

		[Test]
		public void SmallBlobWrite() {
			DeveelDbConnection connection = CreateConnection();
			DeveelDbCommand command = connection.CreateCommand("INSERT INTO LOB_TEST (Id, Data) VALUES (1, ?)");
			DeveelDbLob lob = new DeveelDbLob(command, ReferenceType.Binary, 1024 * 4);

			BinaryWriter writer = new BinaryWriter(lob);
			Random rnd = new Random();
			for (int i = 1; i <= 1024; i++) {
				int value = rnd.Next();
				writer.Write(value);
			}

			writer.Flush();

			// the parameter can be added in every moment: there is not a required
			// order to do it ...
			command.Parameters.Add(lob);
			command.Prepare();

			int count = command.ExecuteNonQuery();

			Assert.AreEqual(1, count);

			connection.Close();
		}

		[Test]
		public void SmallBlobRead() {
			SmallBlobWrite();

			DeveelDbConnection connection = CreateConnection();

			DeveelDbCommand command = connection.CreateCommand("SELECT Data FROM LOB_TEST WHERE Id = 1");
			DeveelDbDataReader reader = command.ExecuteReader();
			if (reader.Read()) {
				DeveelDbLob lob = reader.GetLob(0);
				byte[] buffer = new byte[1024];
				int offset = 0;
				long length = lob.Length;
				while (offset < length) {
					int readCount = lob.Read(buffer, 0, buffer.Length);

					// handle the buffer in some way...

					offset += readCount;
				}
			}

			connection.Close();
		}
	}
}
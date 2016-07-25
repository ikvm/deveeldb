﻿using System;

namespace Deveel.Data.Client.Commands {
	public sealed class CommandInfo {
		public CommandInfo(string name) {
			if (String.IsNullOrEmpty(name))
				throw new ArgumentNullException("name");

			Name = name;
		}

		public string Name { get; private set; }

		public string DisplayName { get; set; }

		public string Description { get; set; }
	}
}

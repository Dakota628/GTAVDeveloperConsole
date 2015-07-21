using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeveloperConsole.UnitTests.Fakes {
	public class FakeDeveloperConsole : IDeveloperConsole {
		public void PrintError(string s) {
			throw new NotImplementedException();
		}
	}
}

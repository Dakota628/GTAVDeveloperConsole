using System;

namespace DeveloperConsole.UnitTests.Fakes {
	public class FakeDeveloperConsole : IDeveloperConsole {
		public void PrintError(string s) {
			throw new NotImplementedException();
		}
        public void PrintDebug(string s) {
            throw new NotImplementedException();
        }
	}
}

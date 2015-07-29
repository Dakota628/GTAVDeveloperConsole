using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeveloperConsole {
	public interface IDeveloperConsole {
		void PrintError(string s);
        void PrintDebug(string s);
	}
}

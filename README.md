# GTAV Developer Console

### Screenshots:
Console:
![console](https://cloud.githubusercontent.com/assets/1666312/8538530/431cab4a-243b-11e5-8546-2ddfada3ef6d.png)
Object Selector:
![obj-selector](https://cloud.githubusercontent.com/assets/1666312/8538531/43286c0a-243b-11e5-82c1-f937f533e085.png)


### Downloads:
Prebuilt binaries can be found on the [releases](../../releases) page


### Features:

 * Run C# code from console or as arguments
 * GTA native type based commands
 * Copy/Paste
 * API to integrate with other scripts
 * Integrated object selector to select onscreen objects


### Controls:
* **Ctrl+V** - Paste into input line
* **Ctrl+C** - Copy input line
* **Ctrl+Tab** - Toggle object selector (when object selected press again to manipulate)
* **Tab** - Scroll through onscreen objects when object selector open
* **Page Up** - Scroll up through the console output
* **Page Down** - Scroll down through the console output
* **Up** - Scroll backward through input history
* **Down** - Scroll forward through input history (if no forward history it will clear the current line)
* **Left** / **Right** - Move cursor through text
* **Backspace** / **Delete** - Does what you would expect..

### Command Syntax Examples:
* **Number** - 10.000000, 10, -10, -10.000
* **CodeBlock (Can represent any object)** - {return new Player(0)}, {return new Player(0).Character}
* **String** - "hello", "im a string", "YAY!", quotes are not required if there are no spaces in the string
* **Boolean** - true, false


### Dependencies:
* [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/)
* [ScriptHookVDotNet](https://github.com/crosire/scripthookvdotnet)


### Script Integration

Include DeveloperConsole.dll in your solution references. [See Example.cs](gtav_console/Example.cs) for API examples.

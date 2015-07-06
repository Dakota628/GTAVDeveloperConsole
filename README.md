# GTAV Developer Console

### Downloads:
Prebuild binaries can be found on the [releases](releases) page


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


### Dependencies:
* [ScriptHookV](http://www.dev-c.com/gtav/scripthookv/)
* [ScriptHookVDotNet](https://github.com/crosire/scripthookvdotnet)


### Script Integration

Include DeveloperConsole.dll in your solution references. [See Example.cs](gtav_console/Example.cs) for API examples.
using System;
using DeveloperConsole;
using GTA;

public class Example : Script {
    private DeveloperConsole.DeveloperConsole _developerConsole;

    public Example() {
        //Register our script to use the console
        this.RegisterConsoleScript(OnConsoleAttached);
    }

    private void OnConsoleAttached(DeveloperConsole.DeveloperConsole dc) {
        //Initialize console stuff here
        _developerConsole = dc;

        //Register commands
        dc.CommandDispatcher.RegisterCommand(
            new CommandDispatcher.Command("ping", "Prints 'pong!' in the console", ExampleCommandEventHandler), true);

        //Register Tick/KeyUp/KeyDown events after we have access to the console
        Tick += OnTick;
    }

    private void ExampleCommandEventHandler(CommandDispatcher.CommandEventArgs e) {
        //Handle commands
        if (e.CommandName == "ping" && e.ArgIndex == 0) {
            _developerConsole.PrintLine("pong!");
        }
    }

    private void OnTick(object sender, EventArgs e) {
    }
}
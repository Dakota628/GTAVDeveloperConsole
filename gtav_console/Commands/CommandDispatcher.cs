﻿using System;
using System.Collections.Generic;
using System.Security.AccessControl;

namespace DeveloperConsole {
    /// <summary>
    /// Model for dispatching commands to their specified callbacks
    /// </summary>
    public class CommandDispatcher {
        /// <summary>
        /// Delegate that handles command events
        /// </summary>
        /// <param name="e">The CommandEventArgs of the run command</param>
        public delegate void CommandEventHandler(CommandEventArgs e);

        /// <summary>
        /// Creates a new CommandDispatcher
        /// </summary>
        public CommandDispatcher() {
            Commands = new Dictionary<string, Command>();
        }

        /// <summary>
        /// A dictionary of all commands where the key is the command name and the value is the Command object
        /// </summary>
        public Dictionary<string, Command> Commands { get; private set; }

        /// <summary>
        /// Registers a command to be handled by the dispatcher
        /// </summary>
        /// <param name="cmd">The Command object</param>
        /// <param name="overwrite">Whether or not we should overwrite an existing command</param>
        /// <returns>Whether or not the command was registered succesfully</returns>
        public bool RegisterCommand(Command cmd, bool overwrite = false) {
            if (!Commands.ContainsKey(cmd.Name) || overwrite) {
                if (!Commands.ContainsKey(cmd.Name)) Commands.Add(cmd.Name, cmd);
                else Commands[cmd.Name] = cmd;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Model for a command arugments
        /// </summary>
        public class CommandArgument {
            /// <summary>
            /// Creates a new CommandArgument from name, description and type
            /// </summary>
            /// <param name="name">The name of the argument</param>
            /// <param name="desc">The description of the argument</param>
            /// <param name="type">The type the argument should return</param>
            public CommandArgument(string name, string desc, Type type) {
                Name = name;
                Type = type;
                Description = desc;
            }

            /// <summary>
            /// The type the argument should return
            /// </summary>
            public Type Type { get; private set; }
            /// <summary>
            /// The name of the argument
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// The description of the argument
            /// </summary>
            public string Description { get; private set; }
        }

        /// <summary>
        /// Model for commands
        /// </summary>
        public class Command {
            /// <summary>
            /// Creates a new Command
            /// </summary>
            /// <param name="cmd">The name of the command</param>
            /// <param name="desc">The description of the command</param>
            /// <param name="expectedArgs">A list of a list of expected command arguments where each outer list is a new argument set</param>
            /// <param name="callback">The CommandEventHandler delegate to callback to</param>
            public Command(string cmd, string desc, List<List<CommandArgument>> expectedArgs,
                CommandEventHandler callback) {
                Name = cmd;
                ExpectedArgs = expectedArgs;
                Callback = callback;
                Description = desc;
            }

            /// <summary>
            /// Creates a new Command
            /// </summary>
            /// <param name="cmd">The name of the command</param>
            /// <param name="desc">The description of the command</param>
            /// <param name="callback">The CommandEventHandler delegate to callback to</param>
            public Command(string cmd, string desc, CommandEventHandler callback) {
                Name = cmd;
                ExpectedArgs = new List<List<CommandArgument>>();
                Callback = callback;
                Description = desc;
            }

            /// <summary>
            /// The CommandEventHandler delegate to callback to
            /// </summary>
            public CommandEventHandler Callback { get; private set; }
            /// <summary>
            /// A list of a list of expected command arguments where each outer list is a new argument set
            /// </summary>
            public List<List<CommandArgument>> ExpectedArgs { get; private set; }
            /// <summary>
            /// The name of the command
            /// </summary>
            public string Name { get; private set; }
            /// <summary>
            /// The description of the command
            /// </summary>
            public string Description { get; private set; }

            /// <summary>
            /// Adds an argument set to the command
            /// </summary>
            /// <param name="args">A list of CommandArguments for this set</param>
            /// <returns>The argument set ID</returns>
            public int AddArgumentSet(params CommandArgument[] args) {
                var i = ExpectedArgs.Count;
                ExpectedArgs.Add(new List<CommandArgument>(args));
                return i;
            }
        }

        /// <summary>
        /// Model for command event arguments
        /// </summary>
        public class CommandEventArgs {
            /// <summary>
            /// Creates a CommandEventArgs
            /// </summary>
            /// <param name="cmd">The name of the command</param>
            /// <param name="tokens">The command tokens in the order in which they were passed</param>
            /// <param name="idx">The argument set ID</param>
            public CommandEventArgs(string cmd, List<CommandToken> tokens, int idx) {
                CommandName = cmd;
                Tokens = tokens;
                ArgIndex = idx;
            }

            /// <summary>
            /// The argument set index
            /// </summary>
            public int ArgIndex { get; set; }
            /// <summary>
            /// The command tokens in the order in which they were passed
            /// </summary>
            public List<CommandToken> Tokens { get; private set; }
            /// <summary>
            /// The name of the command
            /// </summary>
            public string CommandName { get; private set; }
        }
    }
}
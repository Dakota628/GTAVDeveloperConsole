using System;
using System.CodeDom.Compiler;
using GTA;
using Microsoft.CSharp;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DeveloperConsole {
    internal class CommandParser {
        private const string CODEBLOCK_OPEN = "{";
        private const string CODEBLOCK_CLOSE = "}";
        private const string STRING_OPEN = "\"";
        private const string STRING_CLOSE = STRING_OPEN;

        private String _data;
        private readonly IDeveloperConsole _console;
        private List<CommandToken> _tokens = new List<CommandToken>();
        public List<CommandToken> Tokens {
            get {
                return _tokens;
            }
        }

        private String _segments = "";
        private bool _isString = false;
        private bool _isCodeBlock = false;

        public CommandParser(String data, IDeveloperConsole console) {
            _data = data;
            _console = console;
            Parse();
        }

        private void Parse()
        {
            _segments = "";
            _isString = false;
            _isCodeBlock = false;

            foreach (String __s in Regex.Split(_data, @"[\s\r\n]+").Where(s => s != string.Empty)) {
                String s = __s;

                if (_isString) { //Begining of string was found, end not yet found
                    if (IsEndOfString(s)) { //Found end of string
                        TrimStringEnd(ref s);
                        AddSegment(s);
                        AddToken(CommandTokenKind.String);
                    } else { // Found string contents
                        ReplaceStringLiterals(ref s);
                        AddSegment(s);
                    }
                } else if (_isCodeBlock) { //Begining of codeblock was found, end not yet found
                    if (IsEndOfCodeBlock(s)) { //Found end of code block
                        TrimCodeBlockEnd(ref s);
                        AddSegment(s);
                        AddToken(CommandTokenKind.CodeBlock);
                    } else { // Found codeblock contents
                        ReplaceCodeBlockLiterals(ref s);
                        AddSegment(s);
                    }
                } else {

                    if (IsStartOfCodeBlock(s)) { //Found begining of code block
                        TrimCodeBlockStart(ref s);

                        if (IsEndOfCodeBlock(s)) { //This is also the end of the code block
                            TrimCodeBlockEnd(ref s);
                            AddToken(CommandTokenKind.CodeBlock, s);
                        } else {
                            _isCodeBlock = true;
                            AddSegment(s);
                        }
                    } else if (IsStartOfString(s)) { //Found begining of string
                        TrimStringStart(ref s);

                        if (IsEndOfString(s)) { //This is also the end of the string
                            TrimStringEnd(ref s);
                            AddToken(CommandTokenKind.String, s);
                        } else {
                            _isString = true;
                            AddSegment(s);
                        }
                    } else if (IsNumeric(s)) { //Found number
                        AddToken(CommandTokenKind.Number, s);
                    } else { //Found word
                        AddToken(CommandTokenKind.Word, s);
                    }
                }
            }

            if (_segments != "") {
                if (_isString) AddToken(CommandTokenKind.String);
                else if (_isCodeBlock) AddToken(CommandTokenKind.CodeBlock);
            }
        }


        private void AddSegment(String s) {
            if (_segments != "") _segments += " ";
            _segments += s;
        }

        private void AddToken(CommandTokenKind k, String data) {
            var tok = new CommandToken(k, data, _console);
            _console.PrintDebug("Found token -> " + k + " : " + data + " : " + tok.Eval.GetType().FullName);
            _tokens.Add(tok);
            _segments = "";
            _isString = false;
            _isCodeBlock = false;
        }

        private void AddToken(CommandTokenKind k) {
            AddToken(k, _segments);
        }

        private void ReplaceStringLiterals(ref String s) {
            s = s.Replace("\\" + STRING_CLOSE, "");
        }

        private void ReplaceCodeBlockLiterals(ref String s) {
            s = s.Replace("\\" + CODEBLOCK_CLOSE, "");
        }

        private void TrimStringEnd(ref String s) {
            if(IsEndOfString(s)) s = s.Remove(s.Length - 1, STRING_CLOSE.Length);
        }

        private void TrimCodeBlockEnd(ref String s) {
            if (IsEndOfCodeBlock(s)) s = s.Remove(s.Length - 1, CODEBLOCK_CLOSE.Length);
        }

        private void TrimStringStart(ref String s) {
            s = s.Substring(STRING_OPEN.Length);
        }

        private void TrimCodeBlockStart(ref String s) {
            s = s.Substring(CODEBLOCK_OPEN.Length);
        }

        private bool IsNumeric(String s) {
            int i;
            double d;
            return int.TryParse(s, out i) || double.TryParse(s, out d);
        }

        private bool IsEndOfCodeBlock(String s) {
            return s.EndsWith(CODEBLOCK_CLOSE);
        }

        private bool IsStartOfCodeBlock(String s) {
            return s.StartsWith(CODEBLOCK_OPEN);
        }

        private bool IsStartOfString(String s) {
            return s.StartsWith(STRING_OPEN);
        }

        private bool IsEndOfString(String s) {
            return s.EndsWith(STRING_CLOSE) && !s.EndsWith("\\" + STRING_CLOSE);
        }
    }

    public enum CommandTokenKind {
        Word,
        Number,
        String,
        CodeBlock
    }

    public class CommandToken {
        private readonly IDeveloperConsole _console;

        public CommandToken(CommandTokenKind kind, string data, IDeveloperConsole console) {
            _console = console;
            Kind = kind;
            String = data;
        }

        public CommandTokenKind Kind { get; private set; }
        public string String { get; private set; }

        public object Eval {
            get {
                switch (Kind) {
                    case CommandTokenKind.Word:
                        switch (String.ToLower()) {
                            case "true":
                            case "on":
                            case "enable":
                                return true;
                            case "false":
                            case "off":
                            case "disable":
                                return false;
                            case "self":
                                return Game.Player;
                            case "self.character":
                            case "self.ped":
                                return Game.Player.Character;
                            case "self.vehicle":
                                return Game.Player.Character.IsInVehicle() ? Game.Player.Character.CurrentVehicle : null;
                            case "self.pos":
                            case "self.position":
                                return Game.Player.Character.Position;
                            case "self.name":
                                return Game.Player.Name;
                            case "self.id":
                            case "self.handle":
                                return Game.Player.Handle;
                            default:
                                return String;
                        }
                    case CommandTokenKind.CodeBlock:
                        try {
                            return CSharpEval(String);
                        }
                        catch (Exception e) {
                            return null;
                        }
                    case CommandTokenKind.Number:
                        if (String.Contains('.')) return Convert.ToDouble(String);
                        return Convert.ToInt32(String);
                    default:
                        return String;
                }
            }
        }

        public Type Type {
            get { return Eval.GetType(); }
        }

        private object CSharpEval(string cs) {
            var c = new CSharpCodeProvider();
            var cp = new CompilerParameters();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                try {
                    var location = assembly.Location;
                    if (!string.IsNullOrEmpty(location)) cp.ReferencedAssemblies.Add(location);
                }
                catch (NotSupportedException) {
                }
            }

            cp.ReferencedAssemblies.Add(typeof (DeveloperConsole).Assembly.Location);

            foreach (var s in DeveloperConsole.RegisteredScripts)
                cp.ReferencedAssemblies.Add(s.Key.GetType().Assembly.Location);

            cp.CompilerOptions = "/t:library";
            cp.GenerateInMemory = true;

            var code = "using System;\n";
            code += "using System.Drawing;\n";
            code += "using System.Windows.Forms;\n";
            code += "using GTA;\n";
            code += "using GTA.Math;\n";
            code += "using GTA.Native;\n";
            code += "using DeveloperConsole;\n";

            code += "namespace DeveloperConsoleCodeEval{ \n";
            code += "public class DeveloperConsoleCodeEval{ \n";
            code += "public object EvalCode(){\n";
            code += "Player PLAYER = Game.Player; \n";
            code += "Ped PED = PLAYER.Character;\n";
            code += "int MP_ID = Function.Call<int>(Hash.NETWORK_GET_PLAYER_INDEX, PED.Handle);\n";

            code += cs + ";\n";
            code += "return null; \n";
            code += "} \n";
            code += "} \n";
            code += "}\n";

            var cr = c.CompileAssemblyFromSource(cp, code);
            if (cr.Errors.Count > 0) {
                _console.PrintError("C# ERROR: " + cr.Errors[0].ErrorText + " at " + cr.Errors[0].Line + ":" +
                                    cr.Errors[0].Column);
                return null;
            }

            var a = cr.CompiledAssembly;
            var o = a.CreateInstance("DeveloperConsoleCodeEval.DeveloperConsoleCodeEval");

            if (o != null) {
                var t = o.GetType();
                var mi = t.GetMethod("EvalCode");
                var s = mi.Invoke(o, null);
                return s;
            }
            return null;
        }
    }
}
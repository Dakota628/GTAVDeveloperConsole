using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DeveloperConsole.UnitTests.Fakes;

namespace DeveloperConsole.UnitTests {
	[TestClass]
	public class CommandParserTests {
		
		[TestMethod]
		public void cmdParser_HandlesPositiveNumbers() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("12", console);

			CommandToken token = parser.Tokens[0];
			Assert.AreEqual(CommandTokenKind.Number, token.Kind);
			Assert.AreEqual("12", token.String);
		}

		[TestMethod]
		public void cmdParser_HandlesDecimalNumbers() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("12.5", console);

            CommandToken token = parser.Tokens[0];
			Assert.AreEqual(CommandTokenKind.Number, token.Kind);
			Assert.AreEqual("12.5", token.String);
		}

		[TestMethod]
		public void cmdParser_HandlesNegativeNumbers() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("-12", console);

            CommandToken token = parser.Tokens[0];
			Assert.AreEqual(CommandTokenKind.Number, token.Kind);
		}

		[TestMethod]
		public void cmdParser_HandlesNegativeDecimalNumbers() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("-12.5", console);

            CommandToken token = parser.Tokens[0];
			Assert.AreEqual(CommandTokenKind.Number, token.Kind);
			Assert.AreEqual("-12.5", token.String);
        }

		[TestMethod]
		public void cmdParser_HandlesSoloNegativeAsWord() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("-", console);

            CommandToken token = parser.Tokens[0];
			Assert.AreEqual(CommandTokenKind.Word, token.Kind);
			Assert.AreEqual("-", token.String);
		}

		[TestMethod]
		public void cmdParser_HandlesNegativePrefixedWordAsWord() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("-hello", console);

            CommandToken token = parser.Tokens[0];
			Assert.AreEqual(CommandTokenKind.Word, token.Kind);
			Assert.AreEqual("-hello", token.String);
		}

        [TestMethod]
        public void cmdParser_HandlesString() {
            FakeDeveloperConsole console = new FakeDeveloperConsole();
            CommandParser parser = new CommandParser("\"single_string_test\"", console);

            CommandToken token = parser.Tokens[0];
            Assert.AreEqual(CommandTokenKind.String, token.Kind);
            Assert.AreEqual("single_string_test", token.String);
        }

        [TestMethod]
        public void cmdParser_HandlesCodeBlock() {
            FakeDeveloperConsole console = new FakeDeveloperConsole();
            CommandParser parser = new CommandParser("{Test()}", console);

            CommandToken token = parser.Tokens[0];
            Assert.AreEqual(CommandTokenKind.CodeBlock, token.Kind);
        }

        [TestMethod]
        public void cmdParser_HandlesSpacedString() {
            FakeDeveloperConsole console = new FakeDeveloperConsole();
            CommandParser parser = new CommandParser("\"spaced string test\"", console);

            CommandToken token = parser.Tokens[0];
            Assert.AreEqual(CommandTokenKind.String, token.Kind);
            Assert.AreEqual("spaced string test", token.String);
        }

        [TestMethod]
        public void cmdParser_HandlesSpacedCodeBlock() {
            FakeDeveloperConsole console = new FakeDeveloperConsole();
            CommandParser parser = new CommandParser("{return true}", console);

            CommandToken token = parser.Tokens[0];
            Assert.AreEqual(CommandTokenKind.CodeBlock, token.Kind);
        }

        [TestMethod]
        public void cmdParser_HandlesUnterminatedSpacedString() {
            FakeDeveloperConsole console = new FakeDeveloperConsole();
            CommandParser parser = new CommandParser("\"spaced string test", console);

            CommandToken token = parser.Tokens[0];
            Assert.AreEqual(CommandTokenKind.String, token.Kind);
            Assert.AreEqual("spaced string test", token.String);
        }

        [TestMethod]
        public void cmdParser_HandlesUnterminatedSpacedCodeBlock() {
            FakeDeveloperConsole console = new FakeDeveloperConsole();
            CommandParser parser = new CommandParser("{return true", console);

            CommandToken token = parser.Tokens[0];
            Assert.AreEqual(CommandTokenKind.CodeBlock, token.Kind);
        }

        [TestMethod]
        public void cmdParser_HandlesCSCommand() {
            FakeDeveloperConsole console = new FakeDeveloperConsole();
            CommandParser parser = new CommandParser("cs {PED.Velocity = new Vector3( 0, 0 ,1000 ); }", console);

            Assert.AreEqual(CommandTokenKind.Word, parser.Tokens[0].Kind);
            Assert.AreEqual("cs", parser.Tokens[0].String);

            Assert.AreEqual(CommandTokenKind.CodeBlock, parser.Tokens[1].Kind);
            Assert.AreEqual("PED.Velocity = new Vector3( 0, 0 ,1000 ); ", parser.Tokens[1].String);
        }

        [TestMethod]
        public void cmdParser_HandlesTPCommand() {
            FakeDeveloperConsole console = new FakeDeveloperConsole();
            CommandParser parser = new CommandParser("tp -1.392 100.349 100", console);

            Assert.AreEqual(CommandTokenKind.Word, parser.Tokens[0].Kind);
            Assert.AreEqual("tp", parser.Tokens[0].String);

            Assert.AreEqual(CommandTokenKind.Number, parser.Tokens[1].Kind);
            Assert.AreEqual("-1.392", parser.Tokens[1].String);

            Assert.AreEqual(CommandTokenKind.Number, parser.Tokens[2].Kind);
            Assert.AreEqual("100.349", parser.Tokens[2].String);

            Assert.AreEqual(CommandTokenKind.Number, parser.Tokens[3].Kind);
            Assert.AreEqual("100", parser.Tokens[3].String);
        }
	}
}

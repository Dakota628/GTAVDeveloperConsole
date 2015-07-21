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

			CommandToken token = parser.Next();
			Assert.AreEqual(CommandTokenKind.Number, token.Kind);
			Assert.AreEqual("12", token.String);
		}

		[TestMethod]
		public void cmdParser_HandlesDecimalNumbers() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("12.5", console);

			CommandToken token = parser.Next();
			Assert.AreEqual(CommandTokenKind.Number, token.Kind);
			Assert.AreEqual("12.5", token.String);
		}

		[TestMethod]
		public void cmdParser_HandlesNegativeNumbers() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("-12", console);

			CommandToken token = parser.Next();
			Assert.AreEqual(CommandTokenKind.Number, token.Kind);
			Assert.AreEqual("-12", token.String);
		}

		[TestMethod]
		public void cmdParser_HandlesNegativeDecimalNumbers() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("-12.5", console);

			CommandToken token = parser.Next();
			Assert.AreEqual(CommandTokenKind.Number, token.Kind);
			Assert.AreEqual("-12.5", token.String);
		}

		[TestMethod]
		public void cmdParser_HandlesSoloNegativeAsWord() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("-", console);

			CommandToken token = parser.Next();
			Assert.AreEqual(CommandTokenKind.Word, token.Kind);
			Assert.AreEqual("-", token.String);
		}

		[TestMethod]
		public void cmdParser_HandlesNegativePrefixedWordAsWord() {
			FakeDeveloperConsole console = new FakeDeveloperConsole();
			CommandParser parser = new CommandParser("-hello", console);

			CommandToken token = parser.Next();
			Assert.AreEqual(CommandTokenKind.Word, token.Kind);
			Assert.AreEqual("-hello", token.String);
		}
	}
}

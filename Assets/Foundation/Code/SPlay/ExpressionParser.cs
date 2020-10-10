using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SPlay {
	public static class ExpressionParser {

        internal class TokenType {
            readonly string id;

            public TokenType (string _id) { id = _id; }

            public override string ToString () {
                return id;
            }
        }

		public class ParseError {
			public TextRange range;
			public string msg;

			public override string ToString() {
				return $"{range.begin.Line}-{range.begin.Column}: {msg}";
			}
		}

        private class Context {
	        public Lexer lexer;
            public List<ParseError> errorList;

            public bool MatchToken(params TokenType[] types) {
                var nextContent = lexer.NextContent ();
                if (nextContent == null) {
                    errorList.Add(_ErrorEOF(types));
                    return false;
                } else {
                    if (types.Contains(lexer.Token)) {
                        return true;
                    } else {
                        errorList.Add(_ErrorUnexpected (types));
                        return false;
                    }
                }
            }

            public ParseError Error (TextRange r, string msg) {
                return new ParseError {range = r, msg = msg};
            }

            ParseError _ErrorEOF (params TokenType[] expected) {
                return Error(new TextRange(lexer.LastTokenStartCaret, lexer.Caret), $"Expected {_TokenTypeStr(expected)}, found EOF");
            }

            ParseError _ErrorUnexpected (params TokenType[] expected) {
                return Error(new TextRange(lexer.LastTokenStartCaret, lexer.Caret),
                    $"Expected {_TokenTypeStr(expected)}, found {lexer.Token}:{lexer.TokenContents}");
            }

            private static string _TokenTypeStr (TokenType[] expected) {
                string typeStr = "";
                for (int i = 0; i < expected.Length; ++i) {
                    typeStr += expected[i];
                    if (i != expected.Length - 1) {
                        typeStr += " or ";
                    } else {
                        typeStr += " ";
                    }
                }
                return typeStr;
            }
        }

		internal readonly static TokenType
			TK_SPACE = new TokenType("space"),
			TK_INT = new TokenType("int"),
			TK_FLOAT = new TokenType("float"),
			TK_STR = new TokenType("string"),
			TK_BOOL = new TokenType("bool"),
			TK_ID = new TokenType("id"),
			TK_LEFT_PAREN = new TokenType("'('"),
			TK_RIGHT_PAREN = new TokenType("')'"),
			TK_COMMENT = new TokenType("comment");

        readonly static TokenDefinition
            TD_SPACE = new TokenDefinition (@"[\n\r ]+", TK_SPACE, true),
            TD_COMMENT = new TokenDefinition (@"(?m)//.*$", TK_COMMENT, true),
            TD_INT = new TokenDefinition (@"[+\-]?([0-9])+", TK_INT),
            TD_FLOAT = new TokenDefinition (@"[+\-]?[0-9]+\.[0-9]*", TK_FLOAT),
            TD_STR = new TokenDefinition (@"""[^""]*""", TK_STR),
            TD_BOOL = new TokenDefinition(@"true|false", TK_BOOL),
            TD_ID = new TokenDefinition (@"[A-Za-z_][A-Za-z0-9\-_]*", TK_ID),
            TD_LEFT_PAREN = new TokenDefinition (@"\(", TK_LEFT_PAREN),
            TD_RIGHT_PAREN = new TokenDefinition (@"\)", TK_RIGHT_PAREN);

		public static Expression Parse(string s) {
			Lexer l = new Lexer(new StringReader(s),
				new[] { TD_SPACE, TD_COMMENT, TD_INT, TD_FLOAT, TD_STR, TD_BOOL, TD_ID, TD_LEFT_PAREN, TD_RIGHT_PAREN });
			var ctx = new Context {
				lexer = l,
				errorList = new List<ParseError>()
			};
			var exp = ParseImpl(ctx);
			if (ctx.errorList.Count > 0) {
				XDebug.Error("Failed parsing expression, errors: ");
				foreach (var err in ctx.errorList) {
					XDebug.Error($"    {err}");
				}
			}
			return exp;
		}

		private static Expression ParseImpl(Context c) {
			if (!c.MatchToken(TK_LEFT_PAREN)) {
				return null;
			}

			List<object> entries = new List<object>();
			while (true) {
				if (!c.MatchToken(TK_LEFT_PAREN, TK_RIGHT_PAREN, TK_ID, TK_INT, TK_STR, TK_FLOAT))
					return null;

				// Right paren closes the expression
				if (c.lexer.Token == TK_RIGHT_PAREN) {
					// XDebug.Assert(!c.lexer.Next(), $"Expected EOF, got '{c.lexer.TokenContents}'");
					return new Expression(entries.ToArray());
				} else if (c.lexer.Token == TK_ID) {
					entries.Add(new ID(c.lexer.TokenContents));
				} else if (c.lexer.Token == TK_INT) {
					entries.Add(int.Parse(c.lexer.TokenContents));
				} else if (c.lexer.Token == TK_FLOAT) {
					entries.Add(float.Parse(c.lexer.TokenContents));
				} else if (c.lexer.Token == TK_LEFT_PAREN) {
					c.lexer.PushCurrent();
					entries.Add(ParseImpl(c));
				} else if (c.lexer.Token == TK_STR) {
					var raw = c.lexer.TokenContents;
					entries.Add(raw.Substring(1, raw.Length - 2));
				}
			}
		}

	}
}
#undef DEBUG
#undef SIMPLE_DEBUG
// #define DEBUG
// #define SIMPLE_DEBUG

#region

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine; //using VRC.SDK3.Avatars.Components;

#endregion

// ReSharper disable once CheckNamespace
// ReSharper disable once IdentifierTypo
namespace tinycpp
{
	#region

	using size_t = UInt64;
	using off_t = Int64;
	using int8_t = SByte;
	using uint8_t = Byte;
	using int16_t = Int16;
	using uint16_t = UInt16;
	using int32_t = Int32;
	using uint32_t = UInt32;
	using int64_t = Int64;
	using uint64_t = UInt64;

	#endregion

	// #if UNITY_EDITOR
	public class Preproc : Tokenizer
	{
		public const int MAXRecursion = 32;

		private const int TtLand = TT_CUSTOM + 0;
		private const int TtLor = TT_CUSTOM + 1;
		private const int TtLte = TT_CUSTOM + 2;
		private const int TtGte = TT_CUSTOM + 3;
		private const int TtShl = TT_CUSTOM + 4;
		private const int TtShr = TT_CUSTOM + 5;
		private const int TtEq = TT_CUSTOM + 6;
		private const int TtNeq = TT_CUSTOM + 7;
		private const int TtLt = TT_CUSTOM + 8;
		private const int TtGt = TT_CUSTOM + 9;
		private const int TtBand = TT_CUSTOM + 10;
		private const int TtBor = TT_CUSTOM + 11;
		private const int TtXor = TT_CUSTOM + 12;
		private const int TtNeg = TT_CUSTOM + 13;
		private const int TtPlus = TT_CUSTOM + 14;
		private const int TtMinus = TT_CUSTOM + 15;
		private const int TtMul = TT_CUSTOM + 16;
		private const int TtDiv = TT_CUSTOM + 17;
		private const int TtMod = TT_CUSTOM + 18;
		private const int TtLparen = TT_CUSTOM + 19;
		private const int TtRparen = TT_CUSTOM + 20;
		private const int TtLnot = TT_CUSTOM + 21;
		private const int TtMAX = TT_CUSTOM + 22;

		private int[] _bplist;

		private readonly string[] _directives =
		{
			"include", "error", "warning", "define", "undef", "if", "elif", "else", "ifdef", "ifndef", "endif", "line",
			"pragma", ""
		};

		private IOutputInterface _iface;

		public List<string> Includedirs;
		public string LastFile;
		public int LastLine;
		public Dictionary<string, Macro> Macros;
		public Tokenizer[] Tchain = new Tokenizer[MAXRecursion];

		public Preproc()
		{
			Includedirs = new List<string>();
			cpp_add_includedir(".");
			Macros = new Dictionary<string, Macro>();
			Macro m = new Macro();
			m.ArgCount = 1;
			add_macro("defined", m);
			m = new Macro();
			m.ArgCount = 0;
			m.Objectlike = true;
			add_macro("__FILE__", m);
			m = new Macro();
			m.ArgCount = 0;
			m.Objectlike = true;
			add_macro("__LINE__", m);
		}

		private bool Objectlike(Macro m)
		{
			return m.Objectlike;
		}

		private bool Functionlike(Macro m)
		{
			return !m.Objectlike;
		}

		private int MACRO_ARGCOUNT(Macro m)
		{
			return m.ArgCount;
		}

		private bool MACRO_VARIADIC(Macro m)
		{
			return m.Variadic;
		}

		private int string_hash(string s)
		{
			uint h = 0;
			int i = 0;
			while (i < s.Length)
			{
				h = 16 * h + s[i++];
				h ^= (h >> 24) & 0xf0;
			}

			return (int)(h & 0xfffffff);
		}


		private bool token_needs_string(token tok)
		{
			switch (tok.type)
			{
				case TT_IDENTIFIER:
				case TT_WIDECHAR_LIT:
				case TT_WIDESTRING_LIT:
				case TT_SQSTRING_LIT:
				case TT_DQSTRING_LIT:
				case TT_ELLIPSIS:
				case TT_HEX_INT_LIT:
				case TT_OCT_INT_LIT:
				case TT_DEC_INT_LIT:
				case TT_FLOAT_LIT:
				case TT_UNKNOWN:
					return true;
				default:
					return false;
			}
		}

		private static void tokenizer_from_file(Tokenizer t, string f)
		{
			t.tokenizer_init(f, TF_PARSE_STRINGS);
			t.tokenizer_set_filename("<macro>");
			t.tokenizer_rewind();
		}

		private static void tokenizer_from_file(Tokenizer t, StringBuilder f)
		{
			t.tokenizer_init(f.ToString(), TF_PARSE_STRINGS);
			t.tokenizer_set_filename("<macro>");
			t.tokenizer_rewind();
		}

		private Macro get_macro(string name)
		{
			if (name == null)
			{
				return null;
			}

			if (!Macros.ContainsKey(name))
			{
				return null;
			}

			return Macros[name];
		}

		private void add_macro(string name, Macro m)
		{
			Macros[name] = m;
		}

		private bool undef_macro(string name)
		{
			if (!Macros.ContainsKey(name))
			{
				return false;
			}

			Macros.Remove(name);
			return true;
		}

		public void set_output_interface(IOutputInterface iface)
		{
			this._iface = iface;
		}

		private void error_or_warning(string err, string type, Tokenizer t, token curr, bool error)
		{
			int column = curr.column != 0 ? curr.column : t == null ? 0 : t.column;
			int line = curr.line != 0 ? curr.line : t == null ? 0 : t.line;
			StringBuilder ret = new StringBuilder(string.Format("<{0}> {1}:{2} {3}: '{4}': {5}\n",
				t == null ? "" : t.filename, line, column, type, err, curr.value));
			if (t != null)
			{
				ret.Append(t.buf + "\n");
				for (int i = 0; i < t.buf.Length; i++)
					ret.Append("^");
			}

			ret.Append("\n");
			if (error)
			{
				_iface.EmitError(ret.ToString());
			}
			else
			{
				_iface.EmitWarning(ret.ToString());
			}
		}

		private void Error(string err, Tokenizer t, token curr)
		{
			error_or_warning(err, "error", t, curr, true);
		}

		private void Warning(string err, Tokenizer t, token curr)
		{
			error_or_warning(err, "warning", t, curr, false);
		}

		private void Emit(string s, Tokenizer t, token curr)
		{
			_iface.Emit(s, t.filename, curr.line, curr.column);
		}

		private bool x_tokenizer_next_of(Tokenizer t, out token tok, bool failUnk)
		{
			bool ret = t.tokenizer_next(out tok);
			if (tok.type == TT_OVERFLOW)
			{
				Error("max token length of 4095 exceeded!", t, tok);
				return false;
			}

			if (failUnk && ret == false)
			{
				Error("Tokenizer encountered unknown token", t, tok);
				return false;
			}

			return true;
		}

		private bool tokenizer_next(Tokenizer t, out token tok)
		{
			return x_tokenizer_next_of(t, out tok, false);
		}

		private bool x_tokenizer_next(Tokenizer t, out token tok)
		{
			return x_tokenizer_next_of(t, out tok, true);
		}

		private bool is_whitespace_token(token token)
		{
			return token.type == TT_SEP &&
			       (token.value == ' ' || token.value == '\t');
		}

		/* return index of matching item in values array, or -1 on error */
		private int Expect(Tokenizer t, int tt, string[] values, out token token)
		{
			bool ret;
			bool gotoErr = false;
			do
			{
				// Debug.Log("Peeking " + t.peeking + "/ " + t.buf + " /" + t.column + " " + (int)t.peek_token.type+ "," + t.buf);
				ret = t.tokenizer_next(out token);
				// Debug.Log("Peeking " + t.peeking + ":" + token + "/ " + t.buf + " /" + t.column + " " + ret + " " + (int)token.type);
				if (!ret || token.type == TT_EOF)
				{
					gotoErr = true;
					break;
				}
			} while (is_whitespace_token(token));

			if (gotoErr || token.type != tt)
			{
				Error("unexpected token", t, token);
				return -1;
			}

			int i = 0;
			while (i < values.Length && values[i] != "")
			{
				// Debug.Log("Check expect " + i + ": " + (values[i]) + "==" + t.buf);
				if (values[i] == t.buf)
					return i;
				++i;
			}

			return -1;
		}

		private bool is_char(token tok, int ch)
		{
			return tok.type == TT_SEP && tok.value == ch;
		}

		private string flush_whitespace(ref int wsCount)
		{
			if (wsCount <= 0)
			{
				return "";
			}

			string xout = "".PadLeft(wsCount, ' ');
			wsCount = 0;
			return xout;
		}

		/* skips until the next non-whitespace token (if the current one is one too)*/
		private bool eat_whitespace(Tokenizer t, ref token token, out int count)
		{
			count = 0;
			bool ret = true;
			while (is_whitespace_token(token))
			{
				++count;
				ret = x_tokenizer_next(t, out token);
				if (!ret) break;
			}

			return ret;
		}

		/* fetches the next token until it is non-whitespace */
		private bool skip_next_and_ws(Tokenizer t, out token tok)
		{
			bool ret = t.tokenizer_next(out tok);
			if (!ret) return ret;
			int wsCount;
			ret = eat_whitespace(t, ref tok, out wsCount);
			return ret;
		}

		private void emit_token(token tok, Tokenizer t)
		{
			if (tok.type == TT_SEP)
			{
				Emit("" + (char)tok.value, t, tok);
			}
			else if (t.buf != "" && token_needs_string(tok))
			{
				Emit(t.buf, t, tok);
			}
			else if (tok.type != TT_EOF)
			{
				Error(string.Format("oops, dunno how to handle tt {0} ({1})\n", tok.type, t.buf), null, tok);
			}
		}

		private void emit_token_builder(StringBuilder builder, token tok, Tokenizer t)
		{
			if (tok.type == TT_SEP)
			{
				builder.Append((char)tok.value);
			}
			else if (t.buf != "" && token_needs_string(tok))
			{
				builder.Append(t.buf);
			}
			else if (tok.type != TT_EOF)
			{
				Error(string.Format("oops, dunno how to handle tt {0} ({1})\n", tok.type, t.buf), null, tok);
			}
		}

		private string emit_token_str(token tok, Tokenizer t)
		{
			if (tok.type == TT_SEP)
			{
				return "" + (char)tok.value;
			}

			if (t.buf != "" && token_needs_string(tok))
			{
				return t.buf;
			}

			if (tok.type != TT_EOF)
			{
				Error(string.Format("oops, dunno how to handle tt {0} ({1})\n", tok.type, t.buf), null, tok);
				return "";
			}

			return "";
		}

		private bool include_file(Tokenizer t)
		{
			string[] incChars = { "\"", "<", "" };
			string[] incCharsEnd = { "\"", ">", "" };
			token tok;
			t.tokenizer_set_flags(0); // disable string tokenization

			int inc1Sep = Expect(t, TT_SEP, incChars, out tok);
			if (inc1Sep == -1)
			{
				Error("expected one of [\"<]", t, tok);
				return false;
			}

			bool ret = t.tokenizer_read_until(incCharsEnd[inc1Sep], true);
			if (!ret)
			{
				Error("error parsing filename", t, tok);
				return false;
			}

			string fn = t.buf;
			string filebuf = _iface.IncludeFile(t.filename, ref fn);
			if (!(t.tokenizer_next(out tok) && is_char(tok, incCharsEnd[inc1Sep][0])))
			{
				Debug.LogError("assertion: Failed to get next token");
			}

			t.tokenizer_set_flags(TF_PARSE_STRINGS);
			return parse_file(fn, filebuf);
		}

		private bool emit_error_or_warning(Tokenizer t, bool isError)
		{
			int wsCount;
			bool ret = t.tokenizer_skip_chars(" \t", out wsCount);
			if (!ret) return ret;
			token tmp = new token();
			tmp.column = t.column;
			tmp.line = t.line;
			ret = t.tokenizer_read_until("\n", true);
			if (isError)
			{
				Error(t.buf, t, tmp);
				return false;
			}

			Warning(t.buf, t, tmp);
			return true;
		}

		private bool consume_nl_and_ws(Tokenizer t, out token tok, int expected)
		{
			if (!x_tokenizer_next(t, out tok))
			{
				Error("unexpected", t, tok);
				return false;
			}

			if (expected != 0)
			{
				if (tok.type != TT_SEP || tok.value != expected)
				{
					Error("unexpected", t, tok);
					return false;
				}

				switch (expected)
				{
					case '\\':
						expected = '\n';
						break;
					case '\n':
						expected = 0;
						break;
				}
			}
			else
			{
				if (is_whitespace_token(tok))
				{
				}
				else if (is_char(tok, '\\')) expected = '\n';
				else return true;
			}

			return consume_nl_and_ws(t, out tok, expected);
		}

		private bool parse_macro(Tokenizer t)
		{
			int wsCount;
			bool ret = t.tokenizer_skip_chars(" \t", out wsCount);
			if (!ret) return ret;
			token curr; //tmp = {.column = t.column, .line = t.line};
			ret = t.tokenizer_next(out curr) && curr.type != TT_EOF;
			if (!ret)
			{
				Error("parsing macro name", t, curr);
				return ret;
			}

			if (curr.type != TT_IDENTIFIER)
			{
				Error("expected identifier", t, curr);
				return false;
			}

			string macroname = t.buf;
			#if DEBUG
            Debug.Log("parsing macro " + macroname);
			#endif
			bool redefined = false;
			if (get_macro(macroname) != null)
			{
				if (macroname == "defined")
				{
					Error("\"defined\" cannot be used as a macro name", t, curr);
					return false;
				}

				redefined = true;
			}

			Macro mnew = new Macro();
			mnew.Objectlike = true;
			mnew.Variadic = false;
			mnew.Argnames = new List<string>();

			ret = x_tokenizer_next(t, out curr) && curr.type != TT_EOF;
			if (!ret) return ret;

			if (is_char(curr, '('))
			{
				// Debug.Log(macroname + " open paren " + curr.value);
				mnew.Objectlike = false;
				int expected = 0;
				while (true)
				{
					/* process next function argument identifier */
					ret = consume_nl_and_ws(t, out curr, expected);
					if (!ret)
					{
						Error("unexpected", t, curr);
						return ret;
					}

					expected = 0;
					if (curr.type == TT_SEP)
					{
						// Debug.Log(macroname + " Got cur value " + curr.value);
						switch (curr.value)
						{
							case '\\':
								expected = '\n';
								continue;
							case ',':
								continue;
							case ')':
								ret = t.tokenizer_skip_chars(" \t", out wsCount);
								if (!ret) return ret;
								goto break_loop1;
							default:
								Error("unexpected character", t, curr);
								return false;
						}
					}

					if (!(curr.type == TT_IDENTIFIER || curr.type == TT_ELLIPSIS))
					{
						Error("expected identifier for macro arg", t, curr);
						return false;
					}

					{
						if (curr.type == TT_ELLIPSIS)
						{
							if (mnew.Variadic)
							{
								Error("\"...\" isn't the last parameter", t, curr);
								return false;
							}

							mnew.Variadic = true;
						}

						string tmps = t.buf;
						mnew.Argnames.Add(tmps);
					}
					++mnew.ArgCount;
				}

				break_loop1: ;
			}
			else if (is_whitespace_token(curr))
			{
				ret = t.tokenizer_skip_chars(" \t", out wsCount);
				if (!ret) return ret;
			}
			else if (is_char(curr, '\n'))
			{
				/* content-less macro */
				goto done;
			}

			StringBuilder buf = new StringBuilder();

			bool backslashSeen = false;
			while (true)
			{
				/* ignore unknown tokens in macro body */
				ret = t.tokenizer_next(out curr);
				if (!ret) return false;
				if (curr.type == TT_EOF) break;
				if (curr.type == TT_SEP)
				{
					if (curr.value == '\\')
						backslashSeen = true;
					else
					{
						if (curr.value == '\n' && !backslashSeen) break;
						emit_token_builder(buf, curr, t);
						backslashSeen = false;
					}
				}
				else
				{
					emit_token_builder(buf, curr, t);
				}
			}

			if (mnew.Objectlike)
			{
				Tokenizer tmp = new Tokenizer();
				string[] visited = new string[MAXRecursion];
				StringBuilder sb = new StringBuilder();
				sb.Append("AAAA_");
				sb.Append(macroname);
				sb.Append("()\n");
				tokenizer_from_file(tmp, sb);
				token mname;
				tmp.tokenizer_next(out mname);
				Macro mtmp = new Macro();
				mtmp.ArgCount = 0;
				mtmp.Objectlike = false;
				mtmp.StrContentsBuf = buf.ToString();
				add_macro("AAAA_" + macroname, mtmp);
				buf.Clear();
				expand_macro(tmp, ref buf, "AAAA_" + macroname, 1, visited);
				undef_macro("AAAA_" + macroname);
			}

			mnew.StrContentsBuf = buf.ToString();
			done:
			if (redefined)
			{
				Macro old = get_macro(macroname);
				string sOld = old.StrContentsBuf;
				string sNew = mnew.StrContentsBuf;
				if (sOld != sNew)
				{
					Warning("redefinition of macro " + macroname, t, new token());
				}
			}

			// #if SIMPLE_DEBUG
			StringBuilder tmpstr = new StringBuilder();
			if (!mnew.Objectlike)
			{
				tmpstr.Append('(');
				bool f = true;
				for (int xi = 0; xi < mnew.ArgCount; xi++)
				{
					if (!f)
					{
						tmpstr.Append(',');
					}

					f = false;
					tmpstr.Append(mnew.Argnames[xi]);
				}

				tmpstr.Append(')');
			}

			Debug.Log("Defining " + macroname + tmpstr + "=" + mnew.StrContentsBuf);
			// #endif
			add_macro(macroname, mnew);
			return true;
		}

		private int macro_arglist_pos(Macro m, string iden)
		{
			int i;
			for (i = 0; i < m.Argnames.Count; i++)
			{
				string item = m.Argnames[i];
				if (item == iden) return i;
			}

			return -1;
		}

		private bool was_visited(string name, string[] visited, int recLevel)
		{
			int x;
			for (x = recLevel; x >= 0; --x)
			{
				if (visited[x] == name) return true;
			}

			return false;
		}

		private int get_macro_info(
			Tokenizer t,
			List<MacroInfo> miList,
			int nest, int tpos, string name,
			string[] visited, int recLevel)
		{
			int braceLvl = 0;
			while (true)
			{
				token tok;
				bool ret = t.tokenizer_next(out tok);
				if (!ret || tok.type == TT_EOF) break;
				// #if DEBUG
				Debug.Log(string.Format("({0}) nest {1}, brace {2} t: {3}", name, nest, braceLvl, t.buf));
				// #endif
				Macro m = new Macro();
				string newname = t.buf;
				if (tok.type == TT_IDENTIFIER && (m = get_macro(newname)) != null &&
				    !was_visited(newname, visited, recLevel))
				{
					if (Functionlike(m))
					{
						if (t.tokenizer_peek() == '(')
						{
							int tposSave = tpos;
							tpos = get_macro_info(t, miList, nest + 1, tpos + 1, newname, visited, recLevel);
							MacroInfo mi = new MacroInfo();
							mi.Name = newname;
							mi.Nest = nest + 1;
							mi.First = tposSave;
							mi.Last = tpos + 1;
							miList.Add(mi);
						}
					}
					else
					{
						MacroInfo mi = new MacroInfo();
						mi.Name = newname;
						mi.Nest = nest + 1;
						mi.First = tpos;
						mi.Last = tpos + 1;
						miList.Add(mi);
					}
				}
				else if (is_char(tok, '('))
				{
					++braceLvl;
				}
				else if (is_char(tok, ')'))
				{
					--braceLvl;
					if (braceLvl == 0 && nest != 0) break;
				}

				++tpos;
			}

			return tpos;
		}

		private int mem_tokenizers_join(
			FileContainer org, FileContainer inj,
			out FileContainer result,
			int first, long lastpos)
		{
			result = new FileContainer();
			result.Buf = new StringBuilder();
			int i;
			token tok;
			bool ret;
			org.T.tokenizer_rewind();
			for (i = 0; i < first; ++i)
			{
				ret = org.T.tokenizer_next(out tok);
				if (!ret)
				{
					//(!(ret && tok.type != TT_EOF)) {
					Debug.LogError("Assert fail ret tok.type != eof");
				}

				emit_token_builder(result.Buf, tok, org.T);
			}

			int cnt = 0, last = first;
			while (true)
			{
				ret = inj.T.tokenizer_next(out tok);
				if (!ret || tok.type == TT_EOF) break;
				emit_token_builder(result.Buf, tok, inj.T);
				++cnt;
			}

			while (org.T.tokenizer_ftello() < lastpos)
			{
				ret = org.T.tokenizer_next(out tok);
				last++;
			}

			int diff = cnt - (last - first);

			while (true)
			{
				ret = org.T.tokenizer_next(out tok);
				if (!ret || tok.type == TT_EOF) break;
				emit_token_builder(result.Buf, tok, org.T);
			}

			result.T = new Tokenizer();
			tokenizer_from_file(result.T, result.Buf);
			return diff;
		}

		private int tchain_parens_follows(int recLevel)
		{
			int i, c = 0;
			for (i = recLevel; i >= 0; --i)
			{
				c = Tchain[i].tokenizer_peek();
				if (c == EOF) continue;
				if (c == '(') return i;
				break;
			}

			return -1;
		}

		private string Stringify(Tokenizer t)
		{
			bool ret = true;
			token tok;
			StringBuilder output = new StringBuilder();
			output.Append('\"');
			while (true)
			{
				ret = t.tokenizer_next(out tok);
				if (!ret) return "";
				if (tok.type == TT_EOF) break;
				if (is_char(tok, '\n')) continue;
				if (is_char(tok, '\\') && t.tokenizer_peek() == '\n') continue;
				if (tok.type == TT_DQSTRING_LIT)
				{
					string s = t.buf;
					int i = 0;
					while (i < s.Length)
					{
						if (s[i] == '\"')
						{
							output.Append("\\\"");
						}
						else if (s[i] == '\\')
						{
							output.Append("\\\\");
						}
						else
						{
							output.Append(s[i]);
						}

						++i;
					}
				}
				else
					emit_token_builder(output, tok, t);
			}

			output.Append('\"');
			return output.ToString();
		}

		/* rec_level -1 serves as a magic value to signal we're using
		expand_macro from the if-evaluator code, which means activating
		the "define" macro */
		private bool expand_macro(Tokenizer t, ref StringBuilder buf, string name, int recLevel, string[] visited)
		{
			bool isDefine = name == "defined";
			Macro m;
			// if(is_define && rec_level != -1) {
			//     m = null;
			// } else {
			m = get_macro(name);
			// }
			if (m == null)
			{
				if (name.Contains("DEFAULT_UNITY"))
				{
					Debug.Log("m is null for " + name);
				}

				buf.Append(name);
				return true;
			}

			if (recLevel == -1) recLevel = 0;
			if (recLevel >= MAXRecursion)
			{
				Error("max recursion level reached", t, new token());
				return false;
			}
			#if DEBUG
            // Debug.Log(string.Format("lvl {0}: expanding macro {1} ({2})", rec_level, name, m.str_contents_buf));
			#endif

			if (recLevel == 0 && t.filename == "<macro>")
			{
				LastFile = t.filename;
				LastLine = t.line;
			}

			if (name == "__FILE__")
			{
				buf.Append('\"');
				buf.Append(LastFile);
				buf.Append('\"');
				return true;
			}

			if (name == "__LINE__")
			{
				buf.Append("" + LastLine);
				return true;
			}

			visited[recLevel] = name;
			Tchain[recLevel] = t;

			int i;
			token tok;
			int numArgs = MACRO_ARGCOUNT(m);
			// Debug.Log("Macro count " + num_args + " buf " + buf + " name " + name);
			FileContainer[] argvalues = new FileContainer[MACRO_VARIADIC(m) ? numArgs + 1 : numArgs];

			for (i = 0; i < numArgs; i++)
			{
				argvalues[i] = new FileContainer();
				argvalues[i].Buf = new StringBuilder();
			}

			int wsCount;
			/* replace named arguments in the contents of the macro call */
			if (Functionlike(m))
			{
				int iret;
				while (char.IsWhiteSpace((char)t.tokenizer_peek()))
				{
					t.tokenizer_getc();
				}

				if ((iret = t.tokenizer_peek()) != '(')
				{
					Debug.Log(name + ": Peeked a token " + t.filename + ":" + t.peek_token.line + " is " + (char)iret);
					/* function-like macro shall not be expanded if not followed by '(' */
					if (iret == EOF && recLevel > 0 && (iret = tchain_parens_follows(recLevel - 1)) != -1)
					{
						Warning("Replacement text involved subsequent text", t, t.peek_token);
						t = Tchain[iret];
					}
					else
					{
						buf.Append(name);
						Debug.Log("Go to cleanup: " + name);
						return true;
					}
				}

				bool xret = x_tokenizer_next(t, out tok);
				if (!(xret && is_char(tok, '(')))
				{
					Debug.LogError("Invalid token " + tok);
				}

				int currArg = 0;
				bool needArg = true;
				int parens = 0;

				if (!t.tokenizer_skip_chars(" \t", out wsCount)) return false;

				bool varargs = false;
				if (numArgs == 1 && MACRO_VARIADIC(m)) varargs = true;
				while (true)
				{
					xret = t.tokenizer_next(out tok);
					if (!xret) return false;
					if (tok.type == TT_EOF)
					{
						Warning("warning EOF\n", t, tok);
						break;
					}

					if (parens == 0 && is_char(tok, ',') && !varargs)
					{
						if (needArg && 0 == wsCount)
						{
							/* empty argument is OK */
						}

						needArg = true;
						if (!varargs) currArg++;
						if (currArg + 1 == numArgs && MACRO_VARIADIC(m))
						{
							varargs = true;
						}
						else if (currArg >= numArgs)
						{
							Error("too many arguments for function macro", t, tok);
							return false;
						}

						xret = t.tokenizer_skip_chars(" \t", out wsCount);
						if (!xret) return xret;
						continue;
					}

					if (is_char(tok, '('))
					{
						++parens;
					}
					else if (is_char(tok, ')'))
					{
						if (0 == parens)
						{
							if (0 != currArg + numArgs && currArg < numArgs - 1)
							{
								Error("too few args for function macro", t, tok);
								return false;
							}

							break;
						}

						--parens;
					}
					else if (is_char(tok, '\\'))
					{
						if (t.tokenizer_peek() == '\n') continue;
					}

					needArg = false;
					emit_token_builder(argvalues[currArg].Buf, tok, t);
				}
				// } // LYUMA

				for (i = 0; i < numArgs; i++)
				{
					argvalues[i].T = new Tokenizer();
					tokenizer_from_file(argvalues[i].T, argvalues[i].Buf);
					#if DEBUG
                Debug.Log(string.Format("macro argument {0}: {1}", (int) i, argvalues[i].buf));
					#endif
				}

				if (isDefine)
				{
					if (get_macro(argvalues[0].Buf.ToString()) != null)
						buf.Append('1');
					else
						buf.Append('0');
				}

				if (m.StrContentsBuf.Length == 0)
				{
					Debug.Log("buf contents empty for " + name);
					return true;
				}

				FileContainer cwae = new FileContainer(); /* contents_with_args_expanded */
				cwae.Buf = new StringBuilder();

				Tokenizer t2 = new Tokenizer();
				tokenizer_from_file(t2, m.StrContentsBuf);
				int hashCount = 0;
				wsCount = 0;
				while (true)
				{
					bool ret;
					ret = t2.tokenizer_next(out tok);
					if (!ret)
					{
						Debug.Log("Failed tokenizer " + name + ":" + m.StrContentsBuf);
						return false;
					}

					if (tok.type == TT_EOF) break;
					if (tok.type == TT_IDENTIFIER)
					{
						cwae.Buf.Append(flush_whitespace(ref wsCount));
						string id = t2.buf;
						if (MACRO_VARIADIC(m) && id == "__VA_ARGS__")
						{
							id = "...";
						}

						int argNr = macro_arglist_pos(m, id);
						if (argNr != -1)
						{
							argvalues[argNr].T.tokenizer_rewind();
							if (hashCount == 1)
							{
								cwae.Buf.Append(Stringify(argvalues[argNr].T));
								ret = cwae.Buf.Length > 0;
							}
							else
								while (true)
								{
									ret = argvalues[argNr].T.tokenizer_next(out tok);
									if (!ret) return ret;
									if (tok.type == TT_EOF) break;
									emit_token_builder(cwae.Buf, tok, argvalues[argNr].T);
								}

							hashCount = 0;
						}
						else
						{
							if (hashCount == 1)
							{
								Error("'#' is not followed by macro parameter", t2, tok);
								return false;
							}

							emit_token_builder(cwae.Buf, tok, t2);
						}
					}
					else if (is_char(tok, '#'))
					{
						if (hashCount != 0)
						{
							Error("'#' is not followed by macro parameter", t2, tok);
							return false;
						}

						while (true)
						{
							++hashCount;
							/* in a real cpp we'd need to look for '\\' first */
							while (t2.tokenizer_peek() == '\n')
							{
								x_tokenizer_next(t2, out tok);
							}

							if (t2.tokenizer_peek() == '#') x_tokenizer_next(t2, out tok);
							else break;
						}

						if (hashCount == 1) cwae.Buf.Append(flush_whitespace(ref wsCount));
						else if (hashCount > 2)
						{
							Error("only two '#' characters allowed for macro expansion", t2, tok);
							return false;
						}

						if (hashCount == 2)
							ret = t2.tokenizer_skip_chars(" \t\n", out wsCount);
						else
							ret = t2.tokenizer_skip_chars(" \t", out wsCount);

						if (!ret)
						{
							Debug.Log("End of line?" + name);
							return ret;
						}

						wsCount = 0;
					}
					else if (is_whitespace_token(tok))
					{
						wsCount++;
					}
					else
					{
						if (hashCount == 1)
						{
							Error("'#' is not followed by macro parameter", t2, tok);
							return false;
						}

						cwae.Buf.Append(flush_whitespace(ref wsCount));
						emit_token_builder(cwae.Buf, tok, t2);
					}
				}

				cwae.Buf.Append(flush_whitespace(ref wsCount));

				/* we need to expand macros after the macro arguments have been inserted */
				// if(true) { // LYUMA
				#if DEBUG
                Debug.Log("contents with args expanded: " + cwae.buf);
				#endif
				cwae.T = new Tokenizer();
				tokenizer_from_file(cwae.T, cwae.Buf);
				Debug.Log("Expanding " + cwae.Buf);
				// int mac_cnt = 0;
				// while (true) {
				//     bool ret = cwae.t.tokenizer_next(out tok);
				//     if(!ret) {
				//         Debug.Log("Failed null ret: " + name +" in " + cwae.buf);
				//         return ret;
				//     }
				//     if(tok.type == TT_EOF) break;
				//     if(tok.type == TT_IDENTIFIER && get_macro(cwae.t.buf) != null)
				//         ++mac_cnt;
				// }

				cwae.T.tokenizer_rewind();
				List<MacroInfo> mcs = new List<MacroInfo>();
				{
					get_macro_info(cwae.T, mcs, 0, 0, "null", visited, recLevel);
					/* some of the macros might not expand at this stage (without braces)*/
				}
				int depth = 0;
				int macCnt = mcs.Count;
				for (i = 0; i < macCnt; ++i)
				{
					if (mcs[i].Nest > depth) depth = mcs[i].Nest;
				}

				while (depth > -1)
				{
					Debug.Log("Looping " + name + ": depth=" + depth + " mac_cnt=" + macCnt);
					for (i = 0; i < macCnt; ++i)
						if (mcs[i].Nest == depth)
						{
							MacroInfo mi = mcs[i];
							cwae.T.tokenizer_rewind();
							int j;
							token utok;
							for (j = 0; j < mi.First + 1; ++j)
								cwae.T.tokenizer_next(out utok);
							FileContainer ct2 = new FileContainer();
							FileContainer tmp = new FileContainer();
							ct2.Buf = new StringBuilder();
							// cwae.t = new Tokenizer();
							if (!expand_macro(cwae.T, ref ct2.Buf, mi.Name, recLevel + 1, visited))
								return false;
							ct2.T = new Tokenizer();
							tokenizer_from_file(ct2.T, ct2.Buf);
							/* manipulating the stream in case more stuff has been consumed */
							long cwaePos = cwae.T.tokenizer_ftello();
							cwae.T.tokenizer_rewind();
							// #if DEBUG
							Debug.Log("merging " + cwae.Buf + " with " + ct2.Buf);
							// #endif
							int diff = mem_tokenizers_join(cwae, ct2, out tmp, mi.First, cwaePos);
							cwae = tmp;
							// #if DEBUG
							Debug.Log("result: " + cwae.Buf);
							// #endif
							if (diff == 0) continue;
							for (j = 0; j < macCnt; ++j)
							{
								if (j == i) continue;
								MacroInfo mi2 = mcs[j];
								/* modified element mi can be either inside, after or before
								another macro. the after case doesn't affect us. */
								if (mi.First >= mi2.First && mi.Last <= mi2.Last)
								{
									/* inside m2 */
									mi2.Last += diff;
								}
								else if (mi.First < mi2.First)
								{
									/* before m2 */
									mi2.First += diff;
									mi2.Last += diff;
								}
							}
						}

					--depth;
				}

				cwae.T.tokenizer_rewind();
				while (true)
				{
					Macro ma;
					cwae.T.tokenizer_next(out tok);
					// #if SIMPLE_DEBUG
					Debug.Log("Expanding ...:" + cwae.T.buf + " into " + buf);
					// #endif
					if (tok.type == TT_EOF) break;
					if (tok.type == TT_IDENTIFIER && cwae.T.tokenizer_peek() == EOF &&
					    (ma = get_macro(cwae.T.buf)) != null && Functionlike(ma) &&
					    tchain_parens_follows(recLevel) != -1
					)
					{
						bool ret = expand_macro(cwae.T, ref buf, cwae.T.buf, recLevel + 1, visited);
						Debug.Log("Failure:" + cwae.T.buf + " into " + buf);
						if (!ret) return ret;
					}
					else
					{
						emit_token_builder(buf, tok, cwae.T);
					}

					// #if SIMPLE_DEBUG
					Debug.Log("Keep going: " + buf);
					// #endif
				}

				// #if SIMPLE_DEBUG
				Debug.Log("Expanded function: " + cwae.Buf + " to " + buf);
				// #endif
			}
			else
			{
				buf.Append(m.StrContentsBuf);
				Debug.Log("Expanded object: " + name + " to " + buf);
			}

			return true;
		}

		private int Ttint(int x)
		{
			return x - TT_CUSTOM;
		}

		private void Ttent(int[] list, int x, int y)
		{
			list[Ttint(x)] = y;
		}

		private int Bp(int tokentype)
		{
			if (_bplist == null)
			{
				_bplist = new int[TtMAX];
				Ttent(_bplist, TtLor, 1 << 4);
				Ttent(_bplist, TtLand, 1 << 5);
				Ttent(_bplist, TtBor, 1 << 6);
				Ttent(_bplist, TtXor, 1 << 7);
				Ttent(_bplist, TtBand, 1 << 8);
				Ttent(_bplist, TtEq, 1 << 9);
				Ttent(_bplist, TtNeq, 1 << 9);
				Ttent(_bplist, TtLte, 1 << 10);
				Ttent(_bplist, TtGte, 1 << 10);
				Ttent(_bplist, TtLt, 1 << 10);
				Ttent(_bplist, TtGt, 1 << 10);
				Ttent(_bplist, TtShl, 1 << 11);
				Ttent(_bplist, TtShr, 1 << 11);
				Ttent(_bplist, TtPlus, 1 << 12);
				Ttent(_bplist, TtMinus, 1 << 12);
				Ttent(_bplist, TtMul, 1 << 13);
				Ttent(_bplist, TtDiv, 1 << 13);
				Ttent(_bplist, TtMod, 1 << 13);
				Ttent(_bplist, TtNeg, 1 << 14);
				Ttent(_bplist, TtLnot, 1 << 14);
				Ttent(_bplist, TtLparen, 1 << 15);
				//      TTENT(bplist, TT_RPAREN, 1 << 15);
				//      TTENT(bplist, TT_LPAREN, 0);
				Ttent(_bplist, TtRparen, 0);
			}

			// Debug.Log("ttint " + TTINT(tokentype) + " / " + bplist.Length);
			if (Ttint(tokentype) < 0)
			{
				return 0;
			}

			if (Ttint(tokentype) < _bplist.Length) return _bplist[Ttint(tokentype)];
			return 0;
		}

		private int charlit_to_int(string lit)
		{
			if (lit[1] == '\\')
				switch (lit[2])
				{
					case '0': return 0;
					case 'n': return 10;
					case 't': return 9;
					case 'r': return 13;
					case 'x':
					{
						return byte.Parse(lit.Substring(3, 2), NumberStyles.HexNumber);
					}
					default: return lit[2];
				}

			return lit[1];
		}

		private int Nud(Tokenizer t, token tok, ref int err)
		{
			switch (tok.type)
			{
				case TT_IDENTIFIER: return 0;
				case TT_WIDECHAR_LIT:
				case TT_SQSTRING_LIT: return charlit_to_int(t.buf);
				case TT_HEX_INT_LIT:
				case TT_OCT_INT_LIT:
				case TT_DEC_INT_LIT:
					return (int)long.Parse(t.buf);
				case TtNeg: return ~ Expr(t, Bp(tok.type), ref err);
				case TtPlus: return Expr(t, Bp(tok.type), ref err);
				case TtMinus: return -Expr(t, Bp(tok.type), ref err);
				case TtLnot: return 0 == Expr(t, Bp(tok.type), ref err) ? 1 : 0;
				case TtLparen:
				{
					// Debug.Log("nud paren before " + t.tokenizer_ftello());
					int inner = Expr(t, 0, ref err);
					// Debug.Log("nud paren after " + t.tokenizer_ftello());
					if (0 != Expect(t, TtRparen, new[] { ")", "" }, out tok))
					{
						Error("missing ')'", t, tok);
						return 0;
					}

					return inner;
				}
				case TT_FLOAT_LIT:
					Error("floating constant in preprocessor expression", t, tok);
					err = 1;
					return 0;
				case TtRparen:
				default:
					Error("unexpected tokens", t, tok);
					err = 1;
					return 0;
			}
		}

		private int Led(Tokenizer t, int left, token tok, ref int err)
		{
			int right;
			// Debug.Log("led before " + t.tokenizer_ftello() + " " + tok.column);
			switch (tok.type)
			{
				case TtLand:
				case TtLor:
					right = Expr(t, Bp(tok.type), ref err);
					if (tok.type == TtLand) return left != 0 && right != 0 ? 1 : 0;
					return left != 0 || right != 0 ? 1 : 0;
				case TtLte: return left <= Expr(t, Bp(tok.type), ref err) ? 1 : 0;
				case TtGte: return left >= Expr(t, Bp(tok.type), ref err) ? 1 : 0;
				case TtShl: return left << Expr(t, Bp(tok.type), ref err);
				case TtShr: return left >> Expr(t, Bp(tok.type), ref err);
				case TtEq: return left == Expr(t, Bp(tok.type), ref err) ? 1 : 0;
				case TtNeq: return left != Expr(t, Bp(tok.type), ref err) ? 1 : 0;
				case TtLt: return left < Expr(t, Bp(tok.type), ref err) ? 1 : 0;
				case TtGt: return left > Expr(t, Bp(tok.type), ref err) ? 1 : 0;
				case TtBand: return left & Expr(t, Bp(tok.type), ref err);
				case TtBor: return left | Expr(t, Bp(tok.type), ref err);
				case TtXor: return left ^ Expr(t, Bp(tok.type), ref err);
				case TtPlus: return left + Expr(t, Bp(tok.type), ref err);
				case TtMinus: return left - Expr(t, Bp(tok.type), ref err);
				case TtMul: return left * Expr(t, Bp(tok.type), ref err);
				case TtDiv:
				case TtMod:
					right = Expr(t, Bp(tok.type), ref err);
					if (right == 0)
					{
						Error("eval: div by zero", t, tok);
						err = 1;
					}
					else if (tok.type == TtDiv) return left / right;
					else if (tok.type == TtMod) return left % right;

					return 0;
				default:
					Error("eval: unexpect token", t, tok);
					err = 1;
					return 0;
			}
		}


		private bool tokenizer_peek_next_non_ws(Tokenizer t, out token tok)
		{
			bool ret;
			while (true)
			{
				ret = t.tokenizer_peek_token(out tok);
				if (is_whitespace_token(tok))
					x_tokenizer_next(t, out tok);
				else break;
			}

			return ret;
		}

		private int Expr(Tokenizer t, int rbp, ref int err)
		{
			token tok = new token();
			bool ret = skip_next_and_ws(t, out tok);
			Debug.Log("expr before " + t.tokenizer_ftello() + " " + tok.column);
			if (tok.type == TT_EOF) return 0;
			int left = Nud(t, tok, ref err);
			while (true)
			{
				ret = tokenizer_peek_next_non_ws(t, out tok);
				Debug.Log("expr loop " + t.tokenizer_ftello() + " " + tok.column + "," + tok.value + "," + rbp + ":" +
				          tok.type + "," + Bp(tok.type) + " ," + t.peeking + "," + t.peek_token.value + "," + t.buf);
				if (Bp(tok.type) <= rbp) break;
				ret = t.tokenizer_next(out tok);
				Debug.Log("got next expr " + t.tokenizer_ftello() + " " + tok.column + "," + tok.value + "," + rbp +
				          ":" + tok.type + "," + Bp(tok.type) + " ," + t.peeking + "," + t.peek_token.value + "," +
				          t.buf);
				if (tok.type == TT_EOF) break;
				left = Led(t, left, tok, ref err);
			}

			return left;
		}

		private bool do_eval(Tokenizer t, out int result)
		{
			t.tokenizer_register_custom_token(TtLand, "&&");
			t.tokenizer_register_custom_token(TtLor, "||");
			t.tokenizer_register_custom_token(TtLte, "<=");
			t.tokenizer_register_custom_token(TtGte, ">=");
			t.tokenizer_register_custom_token(TtShl, "<<");
			t.tokenizer_register_custom_token(TtShr, ">>");
			t.tokenizer_register_custom_token(TtEq, "==");
			t.tokenizer_register_custom_token(TtNeq, "!=");

			t.tokenizer_register_custom_token(TtLt, "<");
			t.tokenizer_register_custom_token(TtGt, ">");

			t.tokenizer_register_custom_token(TtBand, "&");
			t.tokenizer_register_custom_token(TtBor, "|");
			t.tokenizer_register_custom_token(TtXor, "^");
			t.tokenizer_register_custom_token(TtNeg, "~");

			t.tokenizer_register_custom_token(TtPlus, "+");
			t.tokenizer_register_custom_token(TtMinus, "-");
			t.tokenizer_register_custom_token(TtMul, "*");
			t.tokenizer_register_custom_token(TtDiv, "/");
			t.tokenizer_register_custom_token(TtMod, "%");

			t.tokenizer_register_custom_token(TtLparen, "(");
			t.tokenizer_register_custom_token(TtRparen, ")");
			t.tokenizer_register_custom_token(TtLnot, "!");

			int err = 0;
			result = Expr(t, 0, ref err);
			#if DEBUG
            Debug.Log("eval result: " + result);
			#endif
			return err == 0;
		}

		private bool evaluate_condition(Tokenizer t, ref int result, string[] visited)
		{
			bool ret, backslashSeen = false;
			token curr;
			StringBuilder bufp = new StringBuilder();
			int tflags = t.tokenizer_get_flags();
			t.tokenizer_set_flags(tflags | TF_PARSE_WIDE_STRINGS);
			ret = t.tokenizer_next(out curr);
			if (!ret) return ret;
			if (!is_whitespace_token(curr))
			{
				Error("expected whitespace after if/elif", t, curr);
				return false;
			}

			while (true)
			{
				ret = t.tokenizer_next(out curr);
				if (!ret) return ret;
				if (curr.type == TT_IDENTIFIER)
				{
					if (!expand_macro(t, ref bufp, t.buf, -1, visited)) return false;
				}
				else if (curr.type == TT_SEP)
				{
					if (curr.value == '\\')
						backslashSeen = true;
					else
					{
						if (curr.value == '\n')
						{
							if (!backslashSeen) break;
						}
						else
						{
							emit_token_builder(bufp, curr, t);
						}

						backslashSeen = false;
					}
				}
				else
				{
					emit_token_builder(bufp, curr, t);
				}
			}

			if (bufp.Length == 0)
			{
				Error("#(el)if with no expression", t, curr);
				return false;
			}
			#if DEBUG
            Debug.Log("evaluating condition " + bufp);
			#endif
			Tokenizer t2 = new Tokenizer();
			tokenizer_from_file(t2, bufp);
			ret = do_eval(t2, out result);
			t.tokenizer_set_flags(tflags);
			return ret;
		}

		public bool parse_file(string fn, string buf)
		{
			Tokenizer t = new Tokenizer();
			token curr;
			t.tokenizer_init(buf, TF_PARSE_STRINGS);
			t.tokenizer_set_filename(fn);
			t.tokenizer_register_marker(MT_MULTILINE_COMMENT_START, "/*"); /**/
			t.tokenizer_register_marker(MT_MULTILINE_COMMENT_END, "*/");
			t.tokenizer_register_marker(MT_SINGLELINE_COMMENT_START, "//");
			bool ret, newline = false;
			int wsCount = 0;

			int ifLevel = 0, ifLevelActive = 0, ifLevelSatisfied = 0;
			int xxdi = 0;

			while ((ret = t.tokenizer_next(out curr)) && curr.type != TT_EOF)
			{
				newline = curr.column == 0;
				if (newline)
				{
					ret = eat_whitespace(t, ref curr, out wsCount);
					if (!ret) return ret;
				}

				if (curr.type == TT_EOF) break;
				if (ifLevel > ifLevelActive && !(newline && is_char(curr, '#'))) continue;
				if (is_char(curr, '#'))
				{
					if (!newline)
					{
						Error("stray #", t, curr);
						return false;
					}

					int index = Expect(t, TT_IDENTIFIER, _directives, out curr);
					#if SIMPLE_DEBUG
                    Debug.Log("Preprocessor at " + t.filename + ":" + curr.line + " [" + if_level + "/" + if_level_active + "/" + if_level_satisfied + "]: #" + t.buf + " (type " + index + ")");
					#endif
					if (index == -1)
					{
						if (ifLevel > ifLevelActive) continue;
						Error("invalid preprocessing directive", t, curr);
						return false;
					}

					if (ifLevel > ifLevelActive)
						switch (index)
						{
							case 0:
							case 1:
							case 2:
							case 3:
							case 4:
							case 11:
							case 12:
								continue;
						}

					switch (index)
					{
						case 0:
							ret = include_file(t);
							if (!ret) return ret;
							break;
						case 1:
							ret = emit_error_or_warning(t, true);
							if (!ret) return ret;
							break;
						case 2:
							ret = emit_error_or_warning(t, false);
							if (!ret) return ret;
							break;
						case 3:
							ret = parse_macro(t);
							if (!ret) return ret;
							break;
						case 4:
							if (!skip_next_and_ws(t, out curr)) return false;
							if (curr.type != TT_IDENTIFIER)
							{
								Error("expected identifier", t, curr);
								return false;
							}

							undef_macro(t.buf);
							break;
						case 5: // if
						{
							int newlevel = 0;
							if (ifLevelActive == ifLevel)
							{
								string[] visited = new string[MAXRecursion];
								int tmp = ret ? 1 : 0;
								if (!evaluate_condition(t, ref tmp, visited)) return false;
								newlevel = tmp;
							}

							if (ifLevelActive > ifLevel + 1) ifLevelActive = ifLevel + 1;
							if (ifLevelSatisfied > ifLevel + 1) ifLevelSatisfied = ifLevel + 1;
							if (newlevel != 0) ifLevelActive = ifLevel + 1;
							else if (ifLevelActive == ifLevel + 1) ifLevelActive = ifLevel;
							if (newlevel != 0 && ifLevelActive == ifLevel + 1) ifLevelSatisfied = ifLevel + 1;
							ifLevel = ifLevel + 1;
							break;
						}
						case 6: // elif
							if (ifLevelActive == ifLevel - 1 && ifLevelSatisfied < ifLevel)
							{
								string[] visited = new string[MAXRecursion];
								int tmp = ret ? 1 : 0;
								if (!evaluate_condition(t, ref tmp, visited)) return false;
								ret = tmp != 0;
								if (ret)
								{
									ifLevelActive = ifLevel;
									ifLevelSatisfied = ifLevel;
								}
							}
							else if (ifLevelActive == ifLevel)
							{
								--ifLevelActive;
							}

							break;
						case 7: // else
							if (ifLevelActive == ifLevel - 1 && ifLevelSatisfied < ifLevel)
							{
								if (true)
								{
									ifLevelActive = ifLevel;
									ifLevelSatisfied = ifLevel;
								}
							}
							else if (ifLevelActive == ifLevel)
							{
								--ifLevelActive;
							}

							break;
						case 8: // ifdef
						case 9: // ifndef
							if (!skip_next_and_ws(t, out curr) || curr.type == TT_EOF) return false;
							ret = null != get_macro(t.buf);
							if (index == 9)
							{
								ret = !ret;
							}

						{
							int newlevel = 0;
							if (ifLevelActive == ifLevel)
							{
								newlevel = ret ? 1 : 0;
							}

							if (ifLevelActive > ifLevel + 1) ifLevelActive = ifLevel + 1;
							if (ifLevelSatisfied > ifLevel + 1) ifLevelSatisfied = ifLevel + 1;
							if (newlevel != 0) ifLevelActive = ifLevel + 1;
							else if (ifLevelActive == ifLevel + 1) ifLevelActive = ifLevel;
							if (newlevel != 0 && ifLevelActive == ifLevel + 1) ifLevelSatisfied = ifLevel + 1;
							ifLevel = ifLevel + 1;
						}
							break;
						case 10: // endif
							if (ifLevelActive > ifLevel - 1) ifLevelActive = ifLevel - 1;
							if (ifLevelSatisfied > ifLevel - 1) ifLevelSatisfied = ifLevel - 1;
							ifLevel = ifLevel - 1;
							break;
						case 11: // line
							ret = t.tokenizer_read_until("\n", true);
							if (!ret)
							{
								Error("unknown", t, curr);
								return false;
							}

							break;
						case 12: // pragma
							ret = t.tokenizer_read_until("\n", true);
							// emit("#pragma", t, curr); // FIXME: pragma?
							// while((ret = x_tokenizer_next(t, out curr)) && curr.type != TT_EOF) {
							//     emit_token(curr, t);
							//     if(is_char(curr, '\n')) break;
							// }
							// if(!ret) return ret;
							break;
					}
					#if SIMPLE_DEBUG
                    Debug.Log("Preprocessor done at " + t.filename + ":" + curr.line + " [" + if_level + "/" + if_level_active + "/" + if_level_satisfied + "]");
					#endif
					continue;
				}

				while (wsCount != 0)
				{
					Emit(" ", t, curr);
					--wsCount;
				}
				#if DEBUG
                if(curr.type == TT_SEP)
                    Debug.Log(string.Format("(stdin:{0},{1}) ", curr.line, curr.column) + string.Format("separator: {0}", curr.value == '\n'? ' ' : (char)curr.value));
                else
                    Debug.Log(string.Format("(stdin:{0},{1}) ", curr.line, curr.column) + string.Format("{0}: {0}", tokentype_to_str(curr.type), t.buf));
				#endif
				if (curr.type == TT_IDENTIFIER)
				{
					string[] visited = new string[MAXRecursion];
					StringBuilder tmpout = new StringBuilder();
					if (!expand_macro(t, ref tmpout, t.buf, 0, visited))
						return false;
					Emit(tmpout.ToString(), t, curr); // FIXME: Expanded macro line number?
				}
				else
				{
					emit_token(curr, t);
				}

				xxdi++;
				if (xxdi > 10000)
				{
					break;
				}
			}

			if (ifLevel != 0)
			{
				Error("unterminated #if", t, curr);
				return false;
			}

			return true;
		}

		public void cpp_add_includedir(string includedir)
		{
			Includedirs.Add(includedir);
		}

		public bool cpp_add_define(string mdecl)
		{
			FileContainer tmp = new FileContainer();
			tmp.Buf = new StringBuilder();
			tmp.Buf.Append(mdecl);
			tmp.Buf.Append('\n');
			tmp.T = new Tokenizer();
			tokenizer_from_file(tmp.T, tmp.Buf);
			bool ret = parse_macro(tmp.T);
			return ret;
		}

		public interface IOutputInterface
		{
			void EmitError(string msg);
			void EmitWarning(string msg);
			void Emit(string s, string filename, int line, int column);
			string IncludeFile(string fileContext, ref string filename);
		}

		public class Macro
		{
			public int ArgCount;
			public List<string> Argnames = new List<string>();
			public bool Objectlike;
			public string StrContentsBuf = "";
			public bool Variadic;
		}


		private class MacroInfo
		{
			public int First;
			public int Last;
			public string Name = "Unknown";
			public int Nest;
		}

		private struct FileContainer
		{
			public StringBuilder Buf;
			public Tokenizer T;
		}
	}
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emu5
{
    public enum RVTokenType
    {
        Invalid = 0,
        Label,
        Integer,
        Decimal,
        String,
        Char,
        Separator,
        DataType,
        Instruction,
        Register
    }

    public struct RVToken
    {
        public RVTokenType type;
        public uint line;
        public uint column;
        public object value;
    }

    class RVParser
    {
        private enum ParserState
        {
            Idle = 0,
            DetectingIdentifier,
            DetectingHex,
            DetectingOct,
            DetectingBin,
            DetectingDec,
            DetectingFloat,
            DetectingAddr,
            DetectingString,
            DetectingChar,
            DetectingEscapeS,
            DetectingEscapeC,
            Read0,
        }

        private RVParser() { }

        public static RVToken[][] Tokenize(String code)
        {
            if (String.IsNullOrWhiteSpace(code))
            {
                throw new RVAssemblyException("The file is empty.", 1, 0);
            }

            String[] l_delimiters = new String[2];
            l_delimiters[0] = "\r\n";
            l_delimiters[1] = "\n";

            String[] l_lines = code.Split(l_delimiters, StringSplitOptions.None);
            RVToken[][] l_tokensVect = new RVToken[l_lines.Length][];

            for (int i_lineIndex = 0; i_lineIndex < l_lines.Length; ++i_lineIndex)
            {
                List<RVToken> l_tokenList = new List<RVToken>();
                ParserState l_state = ParserState.Idle;

                uint l_startIndex = 0;
                object l_data = null;

                for (int i_characterIndex = 0; i_characterIndex < l_lines[i_lineIndex].Length; ++i_characterIndex)
                {
                    char l_character = l_lines[i_lineIndex][i_characterIndex];

                    bool l_breakFor = false;

                    switch (l_state)
                    {
                        case ParserState.Idle:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                continue; // ignore white spaces
                            }
                            else if (l_character == '#') // start of comment
                            {
                                l_breakFor = true; // ignore rest of the line
                                break;
                            }
                            else if (l_character == '_' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                l_startIndex = (uint)i_characterIndex;
                                l_data = new String(l_character, 1);

                                l_state = ParserState.DetectingIdentifier;
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-') // detected a separator
                            {
                                RVToken l_separatorToken;
                                l_separatorToken.type = RVTokenType.Separator;
                                l_separatorToken.line = (uint)i_lineIndex + 1;
                                l_separatorToken.column = (uint)i_characterIndex;
                                l_separatorToken.value = l_character;

                                l_tokenList.Add(l_separatorToken);
                            }
                            else if (l_character == '0')
                            {
                                l_startIndex = (uint)i_characterIndex;

                                l_state = ParserState.Read0;
                            }
                            else if (l_character >= '1' && l_character <= '9')
                            {
                                l_startIndex = (uint)i_characterIndex;
                                l_data = (UInt64)(l_character - '0');

                                l_state = ParserState.DetectingDec;
                            }
                            else if (l_character == '.')
                            {
                                l_startIndex = (uint)i_characterIndex;
                                l_data = ".";

                                l_state = ParserState.DetectingFloat;
                            }
                            else if (l_character == '\"')
                            {
                                l_startIndex = (uint)i_characterIndex;
                                l_data = "";

                                l_state = ParserState.DetectingString;
                            }
                            else if (l_character == '\'')
                            {
                                l_startIndex = (uint)i_characterIndex;
                                l_data = null;

                                l_state = ParserState.DetectingChar;
                            }
                            else
                            {
                                throw new RVAssemblyException("Illegal character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                        }
                        break;

                        case ParserState.DetectingIdentifier:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                RVToken l_identifierToken;
                                l_identifierToken.type = RVTokenType.Label;
                                l_identifierToken.line = (uint)i_lineIndex + 1;
                                l_identifierToken.column = l_startIndex;
                                l_identifierToken.value = l_data;

                                l_tokenList.Add(l_identifierToken);

                                l_state = ParserState.Idle;
                            }
                            else if (l_character == '#')
                            {
                                RVToken l_identifierToken;
                                l_identifierToken.type = RVTokenType.Label;
                                l_identifierToken.line = (uint)i_lineIndex + 1;
                                l_identifierToken.column = l_startIndex;
                                l_identifierToken.value = l_data;

                                l_tokenList.Add(l_identifierToken);

                                l_breakFor = true; // ignore rest of the line
                                break;
                            }
                            else if (l_character == '_' || l_character == '.' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z') || (l_character >= '0' && (l_character <= '9')))
                            {
                                l_data = ((String)l_data) + l_character;
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-')
                            {
                                RVToken l_identifierToken;
                                l_identifierToken.type = RVTokenType.Label;
                                l_identifierToken.line = (uint)i_lineIndex + 1;
                                l_identifierToken.column = l_startIndex;
                                l_identifierToken.value = l_data;

                                l_tokenList.Add(l_identifierToken);

                                RVToken l_separatorToken;
                                l_separatorToken.type = RVTokenType.Separator;
                                l_separatorToken.line = (uint)i_lineIndex + 1;
                                l_separatorToken.column = (uint)i_characterIndex;
                                l_separatorToken.value = l_character;

                                l_tokenList.Add(l_separatorToken);

                                l_state = ParserState.Idle;
                            }
                            else if (l_character == '\"')
                            {
                                RVToken l_identifierToken;
                                l_identifierToken.type = RVTokenType.Label;
                                l_identifierToken.line = (uint)i_lineIndex + 1;
                                l_identifierToken.column = l_startIndex;
                                l_identifierToken.value = l_data;

                                l_tokenList.Add(l_identifierToken);

                                l_startIndex = (uint)i_characterIndex;
                                l_data = "";

                                l_state = ParserState.DetectingString;
                            }
                            else if (l_character == '\'')
                            {
                                RVToken l_identifierToken;
                                l_identifierToken.type = RVTokenType.Label;
                                l_identifierToken.line = (uint)i_lineIndex + 1;
                                l_identifierToken.column = l_startIndex;
                                l_identifierToken.value = l_data;

                                l_tokenList.Add(l_identifierToken);

                                l_startIndex = (uint)i_characterIndex;
                                l_data = null;

                                l_state = ParserState.DetectingChar;
                            }
                            else
                            {
                                throw new RVAssemblyException("Illegal character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                        }
                        break;
                    }

                    if (l_breakFor)
                    {
                        break;
                    }
                }
            }

            return l_tokensVect;
        }
    }
}

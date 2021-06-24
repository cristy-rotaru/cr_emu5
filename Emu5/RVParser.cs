using System;
using System.Collections.Generic;

namespace Emu5
{
    public enum RVTokenType
    {
        Invalid = 0,
        Label,
        Address,
        Integer,
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

    public class RVParser
    {
        private enum ParserState
        {
            Idle = 0,
            DetectingIdentifier,
            DetectingHex,
            DetectingOct,
            DetectingBin,
            DetectingDec,
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

                for (int i_characterIndex = 0; i_characterIndex <= l_lines[i_lineIndex].Length; ++i_characterIndex)
                {
                    char l_character = i_characterIndex == l_lines[i_lineIndex].Length ? ' ' : l_lines[i_lineIndex][i_characterIndex];

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
                            else if (l_character == '_' || l_character == '.' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                l_startIndex = (uint)i_characterIndex;
                                l_data = new String(l_character, 1);

                                l_state = ParserState.DetectingIdentifier;
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-' || l_character == '(' || l_character == ')') // detected a separator
                            {
                                RVToken l_separatorToken;
                                l_separatorToken.type = RVTokenType.Separator;
                                l_separatorToken.line = (uint)i_lineIndex + 1;
                                l_separatorToken.column = (uint)i_characterIndex;
                                l_separatorToken.value = l_character;

                                l_tokenList.Add(l_separatorToken);
                            }
                            else if (l_character == '@')
                            {
                                l_startIndex = (uint)i_characterIndex;
                                l_data = null;

                                l_state = ParserState.DetectingAddr;
                            }
                            else if (l_character == '0')
                            {
                                l_startIndex = (uint)i_characterIndex;

                                l_state = ParserState.Read0;
                            }
                            else if (l_character >= '1' && l_character <= '9')
                            {
                                l_startIndex = (uint)i_characterIndex;
                                l_data = (UInt32)(l_character - '0');

                                l_state = ParserState.DetectingDec;
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
                            else if (l_character == ',' || l_character == ':' || l_character == '-' || l_character == '(' || l_character == ')')
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
                            else if (l_character == '\"' || l_character == '\'' || l_character == '@')
                            {
                                throw new RVAssemblyException("Illegal character in identifier name.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else
                            {
                                throw new RVAssemblyException("Illegal character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                        }
                        break;

                        case ParserState.DetectingHex:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else if (l_character == '#')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    l_breakFor = true; // ignore rest of the line
                                    break;
                                }
                            }
                            else if (l_character >= '0' && l_character <= '9')
                            {
                                UInt32 l_number = l_data == null ? 0 : (UInt32)l_data;

                                if ((l_number & 0xF0000000) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number <<= 4;
                                l_number |= (UInt16)(l_character - '0');

                                l_data = l_number;
                            }
                            else if ((l_character >= 'a' && l_character <= 'f') || (l_character >= 'A' && l_character <= 'F'))
                            {
                                l_character = Char.ToLower(l_character);

                                UInt32 l_number = l_data == null ? 0 : (UInt32)l_data;

                                if ((l_number & 0xF0000000) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number <<= 4;
                                l_number |= (UInt16)(l_character - 'a' + 10);

                                l_data = l_number;
                            }
                            else if (l_character == '_' || l_character == '\"' || l_character == '\'' || l_character == '.' || l_character == '@' || (l_character >= 'G' && l_character <= 'Z') || (l_character >= 'g' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-' || l_character == '(' || l_character == ')')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    RVToken l_separatorToken;
                                    l_separatorToken.type = RVTokenType.Separator;
                                    l_separatorToken.line = (uint)i_lineIndex + 1;
                                    l_separatorToken.column = (uint)i_characterIndex;
                                    l_separatorToken.value = l_character;

                                    l_tokenList.Add(l_separatorToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else
                            {
                                throw new RVAssemblyException("Illegal character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                        }
                        break;

                        case ParserState.DetectingOct:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else if (l_character == '#')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    l_breakFor = true; // ignore rest of the line
                                    break;
                                }
                            }
                            else if (l_character >= '0' && l_character <= '7')
                            {
                                UInt32 l_number = l_data == null ? 0 : (UInt32)l_data;

                                if ((l_number & 0xE0000000) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number <<= 3;
                                l_number |= (UInt16)(l_character - '0');

                                l_data = l_number;
                            }
                            else if (l_character == '_' || l_character == '\"' || l_character == '\'' || l_character == '.' || l_character == '@' || l_character == '8' || l_character == '9' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-' || l_character == '(' || l_character == ')')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    RVToken l_separatorToken;
                                    l_separatorToken.type = RVTokenType.Separator;
                                    l_separatorToken.line = (uint)i_lineIndex + 1;
                                    l_separatorToken.column = (uint)i_characterIndex;
                                    l_separatorToken.value = l_character;

                                    l_tokenList.Add(l_separatorToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else
                            {
                                throw new RVAssemblyException("Illegal character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                        }
                        break;

                        case ParserState.DetectingBin:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else if (l_character == '#')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    l_breakFor = true; // ignore rest of the line
                                    break;
                                }
                            }
                            else if (l_character == '0' || l_character == '1')
                            {
                                UInt32 l_number = l_data == null ? 0 : (UInt32)l_data;

                                if ((l_number & 0x80000000) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number <<= 1;
                                l_number |= (UInt16)(l_character - '0');

                                l_data = l_number;
                            }
                            else if (l_character == '_' || l_character == '\"' || l_character == '\'' || l_character == '.' || l_character == '@' || (l_character >= '2' && l_character <= '9') || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-' || l_character == '(' || l_character == ')')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_integerToken;
                                    l_integerToken.type = RVTokenType.Integer;
                                    l_integerToken.line = (uint)i_lineIndex + 1;
                                    l_integerToken.column = l_startIndex;
                                    l_integerToken.value = l_data;

                                    l_tokenList.Add(l_integerToken);

                                    RVToken l_separatorToken;
                                    l_separatorToken.type = RVTokenType.Separator;
                                    l_separatorToken.line = (uint)i_lineIndex + 1;
                                    l_separatorToken.column = (uint)i_characterIndex;
                                    l_separatorToken.value = l_character;

                                    l_tokenList.Add(l_separatorToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else
                            {
                                throw new RVAssemblyException("Illegal character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                        }
                        break;

                        case ParserState.DetectingDec:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                RVToken l_integerToken;
                                l_integerToken.type = RVTokenType.Integer;
                                l_integerToken.line = (uint)i_lineIndex + 1;
                                l_integerToken.column = l_startIndex;
                                l_integerToken.value = l_data;

                                l_tokenList.Add(l_integerToken);

                                l_state = ParserState.Idle;
                            }
                            else if (l_character == '#')
                            {
                                RVToken l_integerToken;
                                l_integerToken.type = RVTokenType.Integer;
                                l_integerToken.line = (uint)i_lineIndex + 1;
                                l_integerToken.column = l_startIndex;
                                l_integerToken.value = l_data;

                                l_tokenList.Add(l_integerToken);

                                l_breakFor = true; // ignore rest of the line
                                break;
                            }
                            else if (l_character >= '0' && l_character <= '9')
                            {
                                UInt32 l_number = (UInt32)l_data;

                                if (l_number > 429496729) // any number larger than that will overflow when multiplying by 10
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number *= 10;

                                if (l_number == 429497290 && l_character > '5')
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number += (UInt16)(l_character - '0');

                                l_data = l_number;
                            }
                            else if (l_character == '_' || l_character == '.' || l_character == '\"' || l_character == '\'' || l_character == '@' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-' || l_character == '(' || l_character == ')')
                            {
                                RVToken l_integerToken;
                                l_integerToken.type = RVTokenType.Integer;
                                l_integerToken.line = (uint)i_lineIndex + 1;
                                l_integerToken.column = l_startIndex;
                                l_integerToken.value = l_data;

                                l_tokenList.Add(l_integerToken);

                                RVToken l_separatorToken;
                                l_separatorToken.type = RVTokenType.Separator;
                                l_separatorToken.line = (uint)i_lineIndex + 1;
                                l_separatorToken.column = (uint)i_characterIndex;
                                l_separatorToken.value = l_character;

                                l_tokenList.Add(l_separatorToken);

                                l_state = ParserState.Idle;
                            }
                            else
                            {
                                throw new RVAssemblyException("Illegal character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                        }
                        break;

                        case ParserState.DetectingAddr:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_addressToken;
                                    l_addressToken.type = RVTokenType.Address;
                                    l_addressToken.line = (uint)i_lineIndex + 1;
                                    l_addressToken.column = l_startIndex;
                                    l_addressToken.value = l_data;

                                    l_tokenList.Add(l_addressToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else if (l_character == '#')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_addressToken;
                                    l_addressToken.type = RVTokenType.Address;
                                    l_addressToken.line = (uint)i_lineIndex + 1;
                                    l_addressToken.column = l_startIndex;
                                    l_addressToken.value = l_data;

                                    l_tokenList.Add(l_addressToken);

                                    l_breakFor = true; // ignore rest of the line
                                    break;
                                }
                            }
                            else if (l_character >= '0' && l_character <= '9')
                            {
                                UInt32 l_number = l_data == null ? 0 : (UInt32)l_data;

                                if ((l_number & 0xF0000000) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number <<= 4;
                                l_number |= (UInt16)(l_character - '0');

                                l_data = l_number;
                            }
                            else if ((l_character >= 'a' && l_character <= 'f') || (l_character >= 'A' && l_character <= 'F'))
                            {
                                l_character = Char.ToLower(l_character);

                                UInt32 l_number = l_data == null ? 0 : (UInt32)l_data;

                                if ((l_number & 0xF000000000000000) != 0)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number <<= 4;
                                l_number |= (UInt16)(l_character - 'a' + 10);

                                l_data = l_number;
                            }
                            else if (l_character == '_' || l_character == '\"' || l_character == '\'' || l_character == '.' || l_character == '@' || (l_character >= 'G' && l_character <= 'Z') || (l_character >= 'g' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-' || l_character == '(' || l_character == ')')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Incomplete numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_addressToken;
                                    l_addressToken.type = RVTokenType.Address;
                                    l_addressToken.line = (uint)i_lineIndex + 1;
                                    l_addressToken.column = l_startIndex;
                                    l_addressToken.value = l_data;

                                    l_tokenList.Add(l_addressToken);

                                    RVToken l_separatorToken;
                                    l_separatorToken.type = RVTokenType.Separator;
                                    l_separatorToken.line = (uint)i_lineIndex + 1;
                                    l_separatorToken.column = (uint)i_characterIndex;
                                    l_separatorToken.value = l_character;

                                    l_tokenList.Add(l_separatorToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else
                            {
                                throw new RVAssemblyException("Illegal character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                        }
                        break;

                        case ParserState.DetectingString:
                        {
                            if (l_character == '\"')
                            {
                                RVToken l_stringToken;
                                l_stringToken.type = RVTokenType.String;
                                l_stringToken.line = (uint)i_lineIndex + 1;
                                l_stringToken.column = l_startIndex;
                                l_stringToken.value = l_data;

                                l_tokenList.Add(l_stringToken);

                                l_state = ParserState.Idle;
                            }
                            else if (l_character == '\\')
                            {
                                l_state = ParserState.DetectingEscapeS;
                            }
                            else
                            {
                                l_data = (String)l_data + l_character;
                            }
                        }
                        break;

                        case ParserState.DetectingChar:
                        {
                            if (l_character == '\'')
                            {
                                if (l_data == null)
                                {
                                    throw new RVAssemblyException("Empty character.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }
                                else
                                {
                                    RVToken l_charToken;
                                    l_charToken.type = RVTokenType.Char;
                                    l_charToken.line = (uint)i_lineIndex + 1;
                                    l_charToken.column = l_startIndex;
                                    l_charToken.value = l_data;

                                    l_tokenList.Add(l_charToken);

                                    l_state = ParserState.Idle;
                                }
                            }
                            else if (l_character == '\\')
                            {
                                if (l_data != null)
                                {
                                    throw new RVAssemblyException("Character stream too long.", (uint)i_lineIndex + 1, l_startIndex);
                                }
                                else
                                {
                                    l_state = ParserState.DetectingEscapeC;
                                }
                            }
                            else
                            {
                                if (l_data != null)
                                {
                                    throw new RVAssemblyException("Character stream too long.", (uint)i_lineIndex + 1, l_startIndex);
                                }
                                else
                                {
                                    l_data = l_character;
                                }
                            }
                        }
                        break;

                        case ParserState.DetectingEscapeS:
                        {
                            if (l_character == '0')
                            {
                                l_data = (String)l_data + '\0';
                            }
                            else if (l_character == 'n')
                            {
                                l_data = (String)l_data + '\n';
                            }
                            else if (l_character == 'r')
                            {
                                l_data = (String)l_data + '\r';
                            }
                            else if (l_character == 't')
                            {
                                l_data = (String)l_data + '\t';
                            }
                            else if (l_character == 'b')
                            {
                                l_data = (String)l_data + '\b';
                            }
                            else if (l_character == 'f')
                            {
                                l_data = (String)l_data + '\f';
                            }
                            else
                            {
                                l_data = (String)l_data + l_character;
                            }

                            l_state = ParserState.DetectingString;
                        }
                        break;

                        case ParserState.DetectingEscapeC:
                        {
                            if (l_character == '0')
                            {
                                l_data = '\0';
                            }
                            else if (l_character == 'n')
                            {
                                l_data = '\n';
                            }
                            else if (l_character == 'r')
                            {
                                l_data = '\r';
                            }
                            else if (l_character == 't')
                            {
                                l_data = '\t';
                            }
                            else if (l_character == 'b')
                            {
                                l_data = '\b';
                            }
                            else if (l_character == 'f')
                            {
                                l_data = '\f';
                            }
                            else
                            {
                                l_data = l_character;
                            }

                            l_state = ParserState.DetectingChar;
                        }
                        break;

                        case ParserState.Read0:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                RVToken l_integerToken;
                                l_integerToken.type = RVTokenType.Integer;
                                l_integerToken.line = (uint)i_lineIndex + 1;
                                l_integerToken.column = l_startIndex;
                                l_integerToken.value = (UInt32)0;

                                l_tokenList.Add(l_integerToken);

                                l_state = ParserState.Idle;
                            }
                            else if (l_character == '#')
                            {
                                RVToken l_integerToken;
                                l_integerToken.type = RVTokenType.Integer;
                                l_integerToken.line = (uint)i_lineIndex + 1;
                                l_integerToken.column = l_startIndex;
                                l_integerToken.value = (UInt32)0;

                                l_tokenList.Add(l_integerToken);

                                l_breakFor = true; // ignore rest of the line
                                break;
                            }
                            else if (l_character == 'b')
                            {
                                l_data = null;

                                l_state = ParserState.DetectingBin;
                            }
                            else if (l_character == 'o')
                            {
                                l_data = null;

                                l_state = ParserState.DetectingOct;
                            }
                            else if (l_character == 'x')
                            {
                                l_data = null;

                                l_state = ParserState.DetectingHex;
                            }
                            else if (l_character == '_' || l_character == '.' || l_character == '\"' || l_character == '\'' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-' || l_character == '(' || l_character == ')')
                            {
                                RVToken l_integerToken;
                                l_integerToken.type = RVTokenType.Integer;
                                l_integerToken.line = (uint)i_lineIndex + 1;
                                l_integerToken.column = l_startIndex;
                                l_integerToken.value = (UInt32)0;

                                l_tokenList.Add(l_integerToken);

                                RVToken l_separatorToken;
                                l_separatorToken.type = RVTokenType.Separator;
                                l_separatorToken.line = (uint)i_lineIndex + 1;
                                l_separatorToken.column = (uint)i_characterIndex;
                                l_separatorToken.value = l_character;

                                l_tokenList.Add(l_separatorToken);

                                l_state = ParserState.Idle;
                            }
                            else if (l_character >= '0' && l_character <= '9')
                            {
                                l_data = (UInt32)(l_character - '0');

                                l_state = ParserState.DetectingDec;
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

                for (int i_listIndex = l_tokenList.Count - 1; i_listIndex >= 0; --i_listIndex)
                {
                    RVToken l_token = l_tokenList[i_listIndex];

                    if (l_token.type == RVTokenType.Label)
                    {
                        String l_identifier = (String)l_token.value;
                        
                        if (l_identifier == "_")
                        {
                            throw new RVAssemblyException("Illegal label name", l_token.line, l_token.column);
                        }

                        RVInstructionDescription? l_instructionDescription = RVInstructions.GetInstructionByString(l_identifier);
                        if (l_instructionDescription != null)
                        {
                            RVToken l_instructionToken;
                            l_instructionToken.type = RVTokenType.Instruction;
                            l_instructionToken.line = l_token.line;
                            l_instructionToken.column = l_token.column;
                            l_instructionToken.value = (RVInstructionDescription)l_instructionDescription;

                            l_tokenList[i_listIndex] = l_instructionToken;
                            continue;
                        }

                        RVRegister? l_register = RVInstructions.GetRegisterByString(l_identifier);
                        if (l_register != null)
                        {
                            RVToken l_registerToken;
                            l_registerToken.type = RVTokenType.Register;
                            l_registerToken.line = l_token.line;
                            l_registerToken.column = l_token.column;
                            l_registerToken.value = (RVRegister)l_register;

                            l_tokenList[i_listIndex] = l_registerToken;
                            continue;
                        }

                        RVDataType? l_dataType = RVAssembler.GetDataTypeByString(l_identifier);
                        if (l_dataType != null)
                        {
                            RVToken l_dataTypeToken;
                            l_dataTypeToken.type = RVTokenType.DataType;
                            l_dataTypeToken.line = l_token.line;
                            l_dataTypeToken.column = l_token.column;
                            l_dataTypeToken.value = (RVDataType)l_dataType;

                            l_tokenList[i_listIndex] = l_dataTypeToken;
                            continue;
                        }
                    }
                    else if (l_token.type == RVTokenType.Separator && (Char)l_token.value == '-')
                    {
                        if (i_listIndex + 1 < l_tokenList.Count)
                        {
                            RVToken l_nextToken = l_tokenList[i_listIndex + 1];

                            if (l_nextToken.type == RVTokenType.Integer)
                            {
                                UInt32 l_number = (UInt32)l_nextToken.value;
                                if (l_number > 0x80000000)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", l_token.line, l_token.column);
                                }
                                else if (l_number < 0x80000000)
                                {
                                    l_number = (UInt32)(-(Int32)l_number);
                                }

                                l_nextToken.value = l_number;
                                l_tokenList[i_listIndex + 1] = l_nextToken;

                                l_tokenList.RemoveAt(i_listIndex);
                            }
                        }
                    }
                }

                l_tokensVect[i_lineIndex] = l_tokenList.ToArray();
            }

            return l_tokensVect;
        }
    }
}

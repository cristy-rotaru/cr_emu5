﻿using System;
using System.Collections.Generic;

namespace Emu5
{
    public enum RVTokenType
    {
        Invalid = 0,
        Label,
        Address,
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
                                UInt64 l_number = l_data == null ? 0 : (UInt64)l_data;

                                if ((l_number & 0xF000000000000000) != 0)
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

                                UInt64 l_number = l_data == null ? 0 : (UInt64)l_data;

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
                            else if (l_character == ',' || l_character == ':' || l_character == '-')
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
                                UInt64 l_number = l_data == null ? 0 : (UInt64)l_data;

                                if ((l_number & 0xE000000000000000) != 0)
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
                            else if (l_character == ',' || l_character == ':' || l_character == '-')
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
                                UInt64 l_number = l_data == null ? 0 : (UInt64)l_data;

                                if ((l_number & 0x8000000000000000) != 0)
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
                            else if (l_character == ',' || l_character == ':' || l_character == '-')
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
                                UInt64 l_number = (UInt64)l_data;

                                if (l_number > 1844674407370955161) // any number larger than that will overflow when multiplying by 10
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number *= 10;

                                if (l_number == 18446744073709551610 && l_character > '5')
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                                }

                                l_number += (UInt16)(l_character - '0');

                                l_data = l_number;
                            }
                            else if (l_character == '_' || l_character == '\"' || l_character == '\'' || l_character == '@' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == '.')
                            {
                                l_data = ((UInt64)l_data).ToString() + ".";

                                l_state = ParserState.DetectingFloat;
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-')
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

                        case ParserState.DetectingFloat:
                        {
                            if (Char.IsWhiteSpace(l_character))
                            {
                                Decimal l_number;
                                if (Decimal.TryParse((String)l_data, out l_number) == false)
                                {
                                    throw new RVAssemblyException("Invalid number.", (uint)i_lineIndex + 1, (uint)l_startIndex);
                                }

                                RVToken l_decimalToken;
                                l_decimalToken.type = RVTokenType.Decimal;
                                l_decimalToken.line = (uint)i_lineIndex + 1;
                                l_decimalToken.column = l_startIndex;
                                l_decimalToken.value = l_number;

                                l_tokenList.Add(l_decimalToken);

                                l_state = ParserState.Idle;
                            }
                            else if (l_character == '#')
                            {
                                Decimal l_number;
                                if (Decimal.TryParse((String)l_data, out l_number) == false)
                                {
                                    throw new RVAssemblyException("Invalid number.", (uint)i_lineIndex + 1, (uint)l_startIndex);
                                }

                                RVToken l_decimalToken;
                                l_decimalToken.type = RVTokenType.Decimal;
                                l_decimalToken.line = (uint)i_lineIndex + 1;
                                l_decimalToken.column = l_startIndex;
                                l_decimalToken.value = l_number;

                                l_tokenList.Add(l_decimalToken);

                                l_breakFor = true; // ignore rest of the line
                                break;
                            }
                            else if ((l_character >= '0' && l_character <= '9') || l_character == 'e' || l_character == 'E' || l_character == '+' || l_character == '-' || l_character == '.')
                            {
                                l_data = (String)l_data + l_character;
                            }
                            else if (l_character == '_' || l_character == '\"' || l_character == '\'' || l_character == '@' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-')
                            {
                                Decimal l_number;
                                if (Decimal.TryParse((String)l_data, out l_number) == false)
                                {
                                    throw new RVAssemblyException("Invalid number.", (uint)i_lineIndex + 1, (uint)l_startIndex);
                                }

                                RVToken l_decimalToken;
                                l_decimalToken.type = RVTokenType.Decimal;
                                l_decimalToken.line = (uint)i_lineIndex + 1;
                                l_decimalToken.column = l_startIndex;
                                l_decimalToken.value = l_number;

                                l_tokenList.Add(l_decimalToken);

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
                                UInt64 l_number = l_data == null ? 0 : (UInt64)l_data;

                                if ((l_number & 0xF000000000000000) != 0)
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

                                UInt64 l_number = l_data == null ? 0 : (UInt64)l_data;

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
                            else if (l_character == ',' || l_character == ':' || l_character == '-')
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
                                l_integerToken.value = (UInt64)0;

                                l_tokenList.Add(l_integerToken);

                                l_state = ParserState.Idle;
                            }
                            else if (l_character == '#')
                            {
                                RVToken l_integerToken;
                                l_integerToken.type = RVTokenType.Integer;
                                l_integerToken.line = (uint)i_lineIndex + 1;
                                l_integerToken.column = l_startIndex;
                                l_integerToken.value = (UInt64)0;

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
                            else if (l_character == '_' || l_character == '\"' || l_character == '\'' || (l_character >= 'A' && l_character <= 'Z') || (l_character >= 'a' && l_character <= 'z'))
                            {
                                throw new RVAssemblyException("Illegal character in numeric sequence.", (uint)i_lineIndex + 1, (uint)i_characterIndex);
                            }
                            else if (l_character == ',' || l_character == ':' || l_character == '-')
                            {
                                RVToken l_integerToken;
                                l_integerToken.type = RVTokenType.Integer;
                                l_integerToken.line = (uint)i_lineIndex + 1;
                                l_integerToken.column = l_startIndex;
                                l_integerToken.value = (UInt64)0;

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
                                l_data = (UInt64)(l_character - '0');

                                l_state = ParserState.DetectingDec;
                            }
                            else if (l_character == '.')
                            {
                                l_data = "0.";

                                l_state = ParserState.DetectingFloat;
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
                        // TODO: recognize instruction, registers and data types
                    }
                    else if (l_token.type == RVTokenType.Separator && (Char)l_token.value == '-')
                    {
                        if (i_listIndex + 1 < l_tokenList.Count)
                        {
                            RVToken l_nextToken = l_tokenList[i_listIndex + 1];

                            if (l_nextToken.type == RVTokenType.Integer)
                            {
                                UInt64 l_number = (UInt64)l_nextToken.value;
                                if (l_number > 0x8000000000000000)
                                {
                                    throw new RVAssemblyException("Number exceeds encodable range.", l_token.line, l_token.column);
                                }
                                else if (l_number < 0x8000000000000000)
                                {
                                    l_number = (UInt64)(-(Int64)l_number);
                                }

                                l_nextToken.value = l_number;
                                l_tokenList[i_listIndex + 1] = l_nextToken;

                                l_tokenList.RemoveAt(i_listIndex);
                            }
                            else if (l_nextToken.type == RVTokenType.Decimal)
                            {
                                l_nextToken.value = -(Decimal)l_nextToken.value;
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

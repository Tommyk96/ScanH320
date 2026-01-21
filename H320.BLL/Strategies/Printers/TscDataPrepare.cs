using H320.BLL.Interfaces;
using System.Collections.Generic;
using System.Text;

namespace H320.BLL.Strategies.Printers
{
    internal class TscDataPrepare : IPrinterDataStrategy
    {
        public List<byte> PreparedData(string data)
        {
            string result = EscapeForTspl(data);
            return [.. Encoding.UTF8.GetBytes(result)];
        }

        public List<byte> PreparedDmxData(string data)
        {
            string result = EscapeForTsplShiftControl(data);
            return [.. Encoding.UTF8.GetBytes(result)];
        }

        //public List<byte> GetKiguGostData(string data)
        //{
        //    //contCode.Replace("\u001d", "~1")
        //    return [.. Encoding.UTF8.GetBytes(data.Replace("\u001d", "~1"))];
        //}

        //public List<byte> GetKiguGSData(string data)
        //{
        //    //Replace("\u001d", "~]"
        //    return [.. Encoding.UTF8.GetBytes(data.Replace("\u001d", "~]"))];
        //}

        //public List<byte> GetSscc18Data(string data)
        //{
        //    //Replace("\u001d", "!102")
        //    return [.. Encoding.UTF8.GetBytes(data.Replace("\u001d", "!102"))];
        //}

        /// <summary>
        /// Экранирует специальные символы в строке согласно официальному руководству TSPL/TSPL2.
        /// Поддерживаются:
        ///   "  →  \["]
        ///   \r →  \[R]
        ///   \n →  \[L]
        /// </summary>
        /// <param name="input">Исходная строка</param>
        /// <returns>Экранированная строка для безопасного использования в TSPL-командах</returns>
        private static string EscapeForTspl(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder(input.Length * 2);

            foreach (char c in input)
            {
                switch (c)
                {
                    case '"':
                        sb.Append(@"\[""]");
                        break;
                    case '\r':
                        sb.Append(@"\[R]");
                        break;
                    case '\n':
                        sb.Append(@"\[L]");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Экранирует управляющие символы (0x00–0x1F) по таблице (1) "~X is shift character for control characters"
        /// из руководства TSPL/TSPL2 (стр. 94 / 76, раздел DMATRIX).
        /// Пример: NUL (0x00) -> "~@", CR (0x0D) -> "~M", ESC (0x1B) -> "~[", US (0x1F) -> "~_"
        /// </summary>
        public static string EscapeForTsplShiftControl(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var sb = new StringBuilder(input.Length * 2);

            foreach (char c in input)
            {
                if (c < 0x20) // 0x00–0x1F
                {
                    sb.Append(GetTsplShiftEscape(c));
                }
                else if(c == '"')
                    sb.Append(@"\[""]");
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Возвращает TSPL escape-последовательность из таблицы "~X" для управляющего символа.
        /// </summary>
        private static string GetTsplShiftEscape(char c)
        {
            return c switch
            {
                (char)0x00 => "~@",
                (char)0x01 => "~A",
                (char)0x02 => "~B",
                (char)0x03 => "~C",
                (char)0x04 => "~D",
                (char)0x05 => "~E",
                (char)0x06 => "~F",
                (char)0x07 => "~G",
                (char)0x08 => "~H",
                (char)0x09 => "~I",
                (char)0x0A => "~J",
                (char)0x0B => "~K",
                (char)0x0C => "~L",
                (char)0x0D => "~M",
                (char)0x0E => "~N",
                (char)0x0F => "~O",
                (char)0x10 => "~P",
                (char)0x11 => "~Q",
                (char)0x12 => "~R",
                (char)0x13 => "~S",
                (char)0x14 => "~T",
                (char)0x15 => "~U",
                (char)0x16 => "~V",
                (char)0x17 => "~W",
                (char)0x18 => "~X",
                (char)0x19 => "~Y",
                (char)0x1A => "~Z",
                (char)0x1B => "~[",
                (char)0x1C => "~\\",
                (char)0x1D => "~]",
                (char)0x1E => "~^",
                (char)0x1F => "~_",
                _ => c.ToString()
            };
        }
    }
}

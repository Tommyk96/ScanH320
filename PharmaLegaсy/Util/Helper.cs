using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Util
{
    public static class Helper
    {
        /// <summary>
        /// Возвращает слова в падеже, зависимом от заданного числа 
        /// </summary>
        /// <param name="number">Число от которого зависит выбранное слово</param>
        /// <param name="nominativ">Именительный падеж слова. Например "день"</param>
        /// <param name="genetiv">Родительный падеж слова. Например "дня"</param>
        /// <param name="plural">Множественное число слова. Например "дней"</param>
        /// <returns></returns>
        public static string GetDeclension(int number, string nominativ, string genetiv, string plural)
        {
            number = number % 100;
            if (number >= 11 && number <= 19)
            {
                return plural;
            }

            var i = number % 10;
            switch (i)
            {
                case 1:
                    return nominativ;
                case 2:
                case 3:
                case 4:
                    return genetiv;
                default:
                    return plural;
            }

        }

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789~!@#$%^&*()_+,.?|;\'\'\"\"";
        public static string RandomMilkPackNum(string gtin, int numlen = 6)
        {
            int MaxNum = chars.Length - 1;
            using (RandomNumberGenerator randGen = RandomNumberGenerator.Create())
            {

                string num = new string(Enumerable.Repeat(chars, numlen)
                    .Select(s => s[randGen.Next(0, MaxNum)]).ToArray());

                string crypt = new string(Enumerable.Repeat(chars, 4)
                    .Select(s => s[randGen.Next(0, MaxNum)]).ToArray());

                return $"01{gtin}21{num}93{crypt}";
            }
        }
        static int Next(this RandomNumberGenerator generator, int min, int max)
        {
            if (generator == null)
                return 0;
            // match Next of Random
            // where max is exclusive
            max = max - 1;

            var bytes = new byte[sizeof(int)]; // 4 bytes
            generator.GetNonZeroBytes(bytes);
            var val = BitConverter.ToInt32(bytes, 0);
            // constrain our values to between our min and max
            // https://stackoverflow.com/a/3057867/86411
            var result = ((val - min) % (max - min + 1) + (max - min + 1)) % (max - min + 1) + min;
            return result;
        }
    }
}

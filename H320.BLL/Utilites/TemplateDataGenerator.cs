using H320.BLL.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Util;

namespace H320.BLL.Utilites
{
    /// <summary>
    /// Оптимизированный генератор шаблонных данных для печати этикеток
    /// </summary>
    public static class TemplateDataGenerator
    {
        // Кэш для байтовых паттернов для улучшения производительности
        private static readonly Dictionary<string, byte[]> _patternCache = new Dictionary<string, byte[]>();

        /// <summary>
        /// Генерирует байтовые данные для шаблона печати с заменой плейсхолдеров
        /// </summary>
        public static byte[] CreateTemplateDataMax(
            string number,
            string user,
            string numPacksInBox,
            string lotNo,
            string expDate,
            List<FSerialization.LabelField> boxLabelFields,
            string sStacker,
            int labelCount,
            string createDate,
            string nettoBox,
            string nettoPack,
            string nettoPal,
            string strGtin,
            string serial,
            string palleteCounter,
            string boxInPalDiapason,
            string contCode,
            string hCONTAINERCODE,
            int productOnPalletCount,
            double nettoBoxVal,
            int containerCounter,
            IPrinterDataStrategy prnStrategy,
            string tmplName = "box.tmpl")
        {
            // Валидация входных параметров
            ValidateParameters(boxLabelFields, prnStrategy, contCode);

            // Нормализация параметров
            user = user ?? string.Empty;
            sStacker = sStacker ?? string.Empty;

            string templatePath = GetTemplatePath(tmplName);

            if (!System.IO.File.Exists(templatePath))
                throw new Exception($"Файл шаблона не найден: {templatePath}");

            try
            {
                // Загрузка шаблона
                byte[] templateData = System.IO.File.ReadAllBytes(templatePath);
                if (templateData == null || templateData.Length == 0)
                    throw new Exception("Ошибка - файл шаблона пуст или поврежден.");

                List<byte> result = new List<byte>(templateData);

                // Создание словаря замен для оптимизации
                var replacements = CreateReplacementDictionary(
                    number, user, numPacksInBox, lotNo, expDate, sStacker, labelCount,
                    createDate, nettoBox, nettoPack, nettoPal, strGtin, serial,
                    palleteCounter, boxInPalDiapason, contCode, hCONTAINERCODE,
                    productOnPalletCount, nettoBoxVal, containerCounter, prnStrategy);

                // Замена полей из boxLabelFields
                ReplaceLabelFields(result, boxLabelFields, prnStrategy);

                // Применение всех замен
                ApplyReplacements(result, replacements);

                return result.ToArray();
            }
            catch (Exception ex)
            {
                Log.Write($"Ошибка обработки шаблона {tmplName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Валидация входных параметров
        /// </summary>
        private static void ValidateParameters(
            List<FSerialization.LabelField> boxLabelFields,
            IPrinterDataStrategy prnStrategy,
            string contCode)
        {
            if (prnStrategy == null)
                throw new ArgumentNullException(nameof(prnStrategy), "Не определена стратегия IPrinterDataStrategy!");

            if (boxLabelFields == null)
                throw new ArgumentNullException(nameof(boxLabelFields), "Массив полей пуст!");

            if (string.IsNullOrWhiteSpace(contCode))
                throw new ArgumentException("Код контейнера не может быть пустым!", nameof(contCode));
        }

        /// <summary>
        /// Получение пути к файлу шаблона
        /// </summary>
        private static string GetTemplatePath(string tmplName)
        {
            string basePath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly()
                .GetModules()[0].FullyQualifiedName);
            return System.IO.Path.Combine(basePath, "tmp", tmplName);
        }

        /// <summary>
        /// Создание словаря замен с ленивыми вычислениями
        /// </summary>
        private static Dictionary<string, Func<List<byte>>> CreateReplacementDictionary(
            string number, string user, string numPacksInBox, string lotNo, string expDate,
            string sStacker, int labelCount, string createDate, string nettoBox, string nettoPack,
            string nettoPal, string strGtin, string serial, string palleteCounter,
            string boxInPalDiapason, string contCode, string hCONTAINERCODE,
            int productOnPalletCount, double nettoBoxVal, int containerCounter, IPrinterDataStrategy prnStrategy)
        {

            string humanViewSSCC18 = number.Length == 20 ? "(00)"+number.Substring(2, number.Length - 2) : number;
            string sscc18WithGroupCode = number.Length == 20 ? number : "00" + number;

            return new Dictionary<string, Func<List<byte>>>()
            {
                
                // SSCC18 баркоды
                ["00000000000000000000"] = () => CreateBytes(sscc18WithGroupCode), // NiceLabel
                ["0000000000000000000"] = () => CreateBytes(sscc18WithGroupCode),  // Bartender
                ["00000000000000000017"] = () => CreateBytes(sscc18WithGroupCode),
                ["!105!10200000000000000000000"] = () => CreateBytes("!105!10200" + contCode),
                ["#SSCC18#"] = () => CreateBytes(humanViewSSCC18),
                ["(00)000000000000000000"] = () => CreateBytes(humanViewSSCC18),
                ["(00)000000000000000017"] = () => CreateBytes(humanViewSSCC18),
                ["123456789012345678"] = () => CreateBytes(sscc18WithGroupCode),

                // Sato коды
                [">F010000000000000011000000100000>F370000>F210000"] = () => CreateBytes(Create128GS1CodForSato(contCode)),
                [">F010000000000000011000000100000>F370000>F2100000"] = () => CreateBytes(Create128GS1CodForSato(contCode)),
                ["#satoBoxCode#"] = () => CreateBytes(">F" + contCode.Replace("\u001d", ">F")),

                // ZPL коды
                ["#zplSscc18#"] = () => CreateBytes(Create128GS1CodForZebra(contCode)),
                [">80100000000000000110000001000>83700>60>82>5100000"] = () => CreateBytes(Create128GS1CodForZebra(contCode)),

                // TSC коды
                ["#TsplSscc18#"] = () => CreateBytes(contCode.Replace("\u001d", "!102")),
                ["#TsplKiguGost#"] = () => prnStrategy.PreparedDmxData(contCode.Replace("\u001d", "~1")),// CreateBytes(contCode.Replace("\u001d", "~1")),
                ["#TsplKiguGs#"] = () => prnStrategy.PreparedDmxData(contCode.Replace("\u001d", "~]")),//CreateBytes(contCode.Replace("\u001d", "~]")),
                ["010000000000000011000000100000!102370000!102210000"] = () => CreateBytes(contCode.Replace("\u001d", "!102")),

                // Контейнеры и счетчики
                ["#ContCode#"] = () => CreateBytes(contCode),
                ["#HCONTAINERCODE#"] = () => CreateBytes(hCONTAINERCODE ?? string.Empty),
                ["#CN#"] = () => CreateBytes(containerCounter.ToString(CultureInfo.InvariantCulture)),
                ["#NL#"] = () => CreateBytes(containerCounter.ToString(CultureInfo.InvariantCulture)),

                // Масса нетто
                ["#ntBox#"] = () => CreateBytes(nettoBox ?? string.Empty),
                ["#ntBox1#"] = () => CreateBytes(nettoBoxVal.ToString("0.0", CultureInfo.InvariantCulture)),
                ["#ntBox2#"] = () => CreateBytes(nettoBoxVal.ToString("0.00", CultureInfo.InvariantCulture)),
                ["#ntBox3#"] = () => CreateBytes(nettoBoxVal.ToString("0.000", CultureInfo.InvariantCulture)),
                ["#ntPack#"] = () => CreateBytes(nettoPack ?? string.Empty),
                ["#ntPal#"] = () => CreateBytes(nettoPal ?? string.Empty),

                // Паллеты
                ["#PNUM#"] = () => CreateBytes(palleteCounter ?? string.Empty),
                ["#PBM#"] = () => CreateBytes(boxInPalDiapason ?? string.Empty),
                ["#PQ#"] = () => CreateBytes(productOnPalletCount.ToString(CultureInfo.InvariantCulture)),

                // Даты
                ["#TODAY#"] = () => CreateBytes(DateTime.Now.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture)),
                ["#MDATE#"] = () => CreateBytes(createDate ?? string.Empty),

                // Продукт
                ["#GTIN#"] = () => CreateBytes(strGtin ?? string.Empty),
                ["#PARTNUM#"] = () => CreateBytes(lotNo ?? string.Empty),
                ["#EXPDATE#"] = () => CreateBytes(expDate ?? string.Empty),
                ["#SERIALNUM#"] = () => CreateBytes(serial ?? string.Empty),
                ["#Q#"] = () => CreateBytes(numPacksInBox ?? string.Empty),
                ["#ST#"] = () => CreateBytes("--"),

                // Печать
                ["Q0001"] = () => CreateBytes("Q" + labelCount.ToString("0000", CultureInfo.InvariantCulture))
            };
        }

        /// <summary>
        /// Замена полей из boxLabelFields
        /// </summary>
        private static void ReplaceLabelFields(List<byte> result, List<FSerialization.LabelField> boxLabelFields, IPrinterDataStrategy prnStrategy)
        {
            const int maxReplacements = 20;

            foreach (var field in boxLabelFields)
            {
                byte[] key = GetPatternBytes(field.FieldName);
                int replacementsCount = 0;

                while (replacementsCount < maxReplacements)
                {
                    int position = BoyerMoore.PatternSearch(key, result.ToArray());
                    if (position < 0) break;

                    List<byte> data = prnStrategy.PreparedData(field.FieldData);
                    result.RemoveRange(position, key.Length);
                    result.InsertRange(position, data);
                    replacementsCount++;
                }
            }
        }

        /// <summary>
        /// Применение всех замен из словаря
        /// </summary>
        private static void ApplyReplacements(List<byte> result, Dictionary<string, Func<List<byte>>> replacements)
        {
            foreach (var replacement in replacements)
            {
                string pattern = replacement.Key;
                Func<List<byte>> valueFactory = replacement.Value;

                byte[] patternBytes = GetPatternBytes(pattern);

                int position = BoyerMoore.PatternSearch(patternBytes, result.ToArray());
                if (position >= 0)
                {
                    List<byte> replacementData = valueFactory();
                    result.RemoveRange(position, patternBytes.Length);
                    result.InsertRange(position, replacementData);
                }
            }
        }

        /// <summary>
        /// Получение байтового представления паттерна с кэшированием
        /// </summary>
        private static byte[] GetPatternBytes(string pattern)
        {
            if (!_patternCache.TryGetValue(pattern, out byte[] bytes))
            {
                bytes = Encoding.UTF8.GetBytes(pattern);
                _patternCache[pattern] = bytes;
            }
            return bytes;
        }

        /// <summary>
        /// Создание списка байтов из строки
        /// </summary>
        private static List<byte> CreateBytes(string value)
        {
            return new List<byte>(Encoding.UTF8.GetBytes(value));
        }

        /// <summary>
        /// Создание GS1-128 кода для принтера Sato
        /// </summary>
        private static string Create128GS1CodForSato(string contCode)
        {
            try
            {
                string[] fields = contCode.Split('\u001d');
                if (fields.Length != 3) return string.Empty;

                var oddFields = fields.Where(f => (f.Length % 2) > 0);

                if (oddFields.Count() > 2)
                {
                    // Все поля нечетные - переключаемся в тип D с первого поля
                    string result = ">F";
                    result += fields[0].Insert(fields[0].Length - 1, ">D") + ">F";
                    result += fields[1] + ">F";
                    result += fields[2].Insert(1, ">C");
                    return result;
                }
                else if ((fields[0].Length % 2) == 0 && (fields[1].Length % 2) != 0)
                {
                    // Первое поле четное, второе нечетное
                    string result = ">F";
                    result += fields[0] + ">F";
                    result += fields[1].Insert(fields[1].Length - 1, ">D") + ">F";
                    result += fields[2].Insert(1, ">C");
                    return result;
                }
                else
                {
                    // Старый алгоритм для обратной совместимости
                    string result = ">F";
                    for (int i = 0; i < fields.Length; i++)
                    {
                        string field = fields[i];
                        int digitCount = field.Count(char.IsDigit);

                        if ((digitCount % 2) > 0)
                        {
                            field = field.Insert(field.Length - 1, ">D");
                            if (i != fields.Length - 1)
                                field += ">C>F";
                            result += field;
                        }
                        else
                        {
                            if (i != fields.Length - 1)
                                result += field + ">F";
                            else
                                result += field;
                        }
                    }
                    return result;
                }
            }
            catch
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Создание GS1-128 кода для принтера Zebra
        /// </summary>
        private static string Create128GS1CodForZebra(string contCode)
        {
            try
            {
                string[] fields = contCode.Split('\u001d');
                if (fields.Length != 3) return string.Empty;

                const string codeB = ">6";
                const string codeC = ">5";

                var oddFields = fields.Where(f => (f.Length % 2) > 0);

                if (oddFields.Count() > 2)
                {
                    // Все поля нечетные
                    string result = ">8";
                    result += fields[0].Insert(fields[0].Length - 1, codeB) + ">8";
                    result += fields[1] + ">8";
                    result += fields[2].Insert(1, codeC);
                    return result;
                }
                else if ((fields[0].Length % 2) == 0 && (fields[1].Length % 2) != 0)
                {
                    // Первое поле четное, второе нечетное
                    string result = ">8";
                    result += fields[0] + ">8";
                    result += fields[1].Insert(fields[1].Length - 1, codeB) + ">8";
                    result += fields[2].Insert(1, codeC);
                    return result;
                }
                else
                {
                    // Старый алгоритм для обратной совместимости
                    string result = ">8";
                    for (int i = 0; i < fields.Length; i++)
                    {
                        string field = fields[i];
                        int digitCount = field.Count(char.IsDigit);

                        if ((digitCount % 2) > 0)
                        {
                            field = field.Insert(field.Length - 1, codeB);
                            if (i != fields.Length - 1)
                                field += ">5>8";
                            result += field;
                        }
                        else
                        {
                            if (i != fields.Length - 1)
                                result += field + ">8";
                            else
                                result += field;
                        }
                    }
                    return result;
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
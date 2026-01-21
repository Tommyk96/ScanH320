using AgrBox.data;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using BoxAgr.DAL.Extensions;
using BoxAgr.DAL.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Util;

namespace BoxAgr.DAL
{

    public class AppDbContext : IBoxRepository
    {
        private const int MAIN_ERROR_CODE = 12000;
        static readonly Lazy<SQLiteAsyncConnection> lazyInitializer = new Lazy<SQLiteAsyncConnection>(() =>
        {
            return new SQLiteAsyncConnection(Constants.DatabasePath, Constants.Flags);
        });

        static SQLiteAsyncConnection Database => lazyInitializer.Value;
        static bool initialized;
        private readonly static object dbLock = new object();
        public AppDbContext() 
        {
            InitializeAsync().SafeFireAndForget(false);
        }
        static async Task InitializeAsync()
        {
            if (!initialized)
            {
                try
                {
                    await Database.CreateTablesAsync(CreateFlags.AutoIncPK | CreateFlags.ImplicitIndex, typeof(SqlBox)).ConfigureAwait(false);

                    initialized = true;
                }
                catch (SQLite.SQLiteException ex)
                {
                    Log.Write("DBS", $"Сбой иницилиализации {ex.Message}", EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                }

                catch (Exception ex)

                {
                    Log.Write("DBS", $"Сбой иницилиализации {ex.Message}", EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                }
            }
        }
        public static async Task<bool> ClearAndInitDb()
        {
            try
            {
                    //удалить все талицы
                    await Database.DropTableAsync<SqlBox>().ConfigureAwait(false);

                    initialized = false;
                    //создать таблицы
                    await InitializeAsync().ConfigureAwait(false);
                    return true;
                
            }
            catch (SQLite.SQLiteException ex)
            {
                Log.Write("DBS", $"Сбой иницилиализации {ex.Message}", EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
            }

            catch (Exception ex)

            {
                Log.Write("DBS", $"Сбой иницилиализации {ex.Message}", EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
            }
            return false;
        }

        private static readonly string queryGetCode = $"SELECT * from SqlBox WHERE Num like ?";
        public bool IsExist(string unitNum)
        {
            try
            {
                lock (dbLock)
                {

                    List<SqlBox> number = Database.QueryAsync<SqlBox>(queryGetCode, new object[1] { $"{unitNum}%" }).Result;

                    if (number?.Count > 0)
                        return true;

                    return false;
                }

            }
            catch (SQLite.SQLiteException ex)
            {
                Log.Write("DBS", ex.Message, EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
            }
            catch (Exception ex)
            {
                Log.Write("DBS", ex.Message, EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
            }
            return false;
        }
        public Unit? GetUnitByBarcode(string unitNum)
        {
            try
            {
                lock (dbLock)
                {
                    List<SqlBox> number = Database.QueryAsync<SqlBox>(queryGetCode, new object[1] { $"{unitNum}%" }).Result;

                    if (number?.Count > 0)
                        return new Unit() { Barcode = number[0].Num, CodeState = number[0].CodeState };

                }

            }
            catch (SQLite.SQLiteException ex)
            {
                Log.Write("DBS", ex.Message, EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
            }
            catch (Exception ex)
            {
                Log.Write("DBS", ex.Message, EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
            }
            return null;
        }
        public bool AddBoxAndVerify(string boxNum, List<ScanCode> codes, out string msg, bool unicOnly)
        {
            lock (dbLock)
            {
                msg = "";
                string curentCode = "";
             

                try
                {
                    List<SqlBox> newItems = new ();
                    //Всталять по одному ненадо!!! надо массивом!! ИСПРАВЬТЕ ЭТО!!!!!!!!!!!!!!!!!
                    int b = Database.ExecuteAsync("BEGIN").Result;
                    //string query = makePlaceholders(codes.Count);

                    foreach (ScanCode s in codes)
                    {
                        //ItemPackDb itm = s.GetItemPack(0);
                        //itm.BoxNum = bNum;
                        //curentCode = itm.Num;

                        ////вставляем по одному с проверкой на уникальнойсть . если есть повторы то в зависимости от unicOnly игнорируем их или нет
                        //try
                        //{
                        //    int a = Database.InsertAsync(itm).Result;
                        //}
                        //catch (AggregateException ex)
                        //{

                        //    //проигнорировать повтор если отключен контроль уникальности
                        //    if (!string.IsNullOrEmpty(ex.Message))
                        //    {
                        //        if (ex.Message.Contains("UNIQUE constraint failed: ItemPackDb.Num") && !unicOnly)
                        //        {
                        //            s.CodeState = CodeState.Repeat;
                        //            continue;
                        //        }
                        //    }
                        //    throw;
                        //}

                    }
                    b = Database.ExecuteAsync("COMMIT").Result;
                    return true;
                }
                catch (Exception ex)
                {
                    int z = Database.ExecuteAsync("ROLLBACK").Result;

                    if (!string.IsNullOrEmpty(ex.Message))
                    {
                        if (ex.Message.Contains("UNIQUE constraint failed: SqlBox.Num"))
                            msg = $"Повтор номера продукта: {curentCode}";
                        else
                            msg = ex.Message;
                    }
                    Log.Write("DBS", ex.Message, EventLogEntryType.Error, MAIN_ERROR_CODE + 1);
                }
            }
            return false;
        }
    }
}

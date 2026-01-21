using AgrBox.data;
using BoxAgr.BLL.Controllers;
using BoxAgr.BLL.Interfaces;
using BoxAgr.BLL.Models;
using System;
using System.Collections.Generic;
using Util;

namespace BoxAgr.DAL
{
    public class JsonDbContext : IBoxRepository
    {
        private const int MAIN_ERROR_CODE = 17000;
      
        private readonly static object dbLock = new object();

        private readonly JobController _job ;
        public JsonDbContext(JobController job)
        {
            _job = job;
        }

        public bool IsExist(string unitNum)
        {
            try
            {
                lock (dbLock)
                {
                    Util.GsLabelData ld = new Util.GsLabelData(unitNum);
                    //проверить в отбракованных
                    if (_job.IsAlreadyInBrack(ld.SerialNumber))
                        return true;

                    //проверить в уже верифицированных номерах
                    if (_job.IsAlreadyInProcessedBox(unitNum, out string  boxNum))
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
                    Util.GsLabelData ld = new Util.GsLabelData(unitNum);
                    //проверить в отбракованных
                    if (_job.IsAlreadyInBrack(ld.SerialNumber))
                        return new Unit() { Barcode = unitNum, CodeState = FSerialization.CodeState.Bad };

                    //проверить в уже верифицированных номерах
                    if (_job.IsAlreadyInProcessedBox(unitNum, out string boxNum))
                        return new Unit() { Barcode = unitNum, CodeState = FSerialization.CodeState.Verify, BoxNumber = boxNum };


                    return default;
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
            throw new NotImplementedException();
        }
    }

}

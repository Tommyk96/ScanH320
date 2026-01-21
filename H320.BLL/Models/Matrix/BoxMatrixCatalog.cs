using FSerialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Util;

namespace BoxAgr.BLL.Models.Matrix
{

    [DataContract]
    public class BoxMatrixCatalog
    {
        private static int MAIN_ERROR_CODE = 3400;
        private static ReaderWriterLockSlim fileSync = new();

        [DataMember]
        private List<BoxMatrix> catalog = new();


        [IgnoreDataMember]
        public List<BoxMatrix> Catalog
        {
            get
            {
                return catalog;
            }
        }



        #region Func
        private static string cfgFileName = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + @"\BoxMatrixCatalog";
        public static BoxMatrixCatalog Load()
        {
            BoxMatrixCatalog r = new();


            try
            {
    
                if (!System.IO.File.Exists(cfgFileName))
                {
                    r.Save();
                    return r;
                }
      
                using (System.IO.TextReader tmpFile = new System.IO.StreamReader(cfgFileName))
                {
                    string s = tmpFile.ReadToEnd();
                    tmpFile.Close();
                    tmpFile.Dispose();

                    BoxMatrixCatalog bj = Archive.DeserializeJSon<BoxMatrixCatalog>(s);
                    if (bj != null)
                    {
                          return bj;
                    }
                }
            }
            catch (SecurityException ex)
            {
                Log.Write("NCG", "SecurityException " + ex.Message + "\n " + cfgFileName, EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
            }
            catch (InvalidOperationException ex)
            {
                Log.Write("NCG", "InvalidOperationException " + ex.Message + "\n " + cfgFileName, EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
            }
            catch (ArgumentException ex)
            {
                Log.Write("NCG", "ArgumentException " + ex.Message + "\n " + cfgFileName, EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
            }
            catch (Exception ex)
            {
                Log.Write("NCG", "Exception " + ex.Message + "\n " + cfgFileName, EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
            }
            return r;

        }
        
        /// <summary>
        /// Возвращат матрицу по гтину
        /// </summary>
        /// <param name="gtin"></param>
        /// <returns></returns>
        public static BoxMatrix? GetMatrixAtGtin(string gtin)
        {
            try
            {
                if (string.IsNullOrEmpty(gtin))
                    return default;

                if (BoxMatrixCatalog.Load() is not BoxMatrixCatalog catalog)
                    return default;

                return catalog.Catalog.FirstOrDefault(x=>x.GTIN == gtin);
            }
            catch (Exception ex)
            {
                Log.Write("NCG", ex.ToString(), EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
            }
            return default;
        }

        public void Save()
        {
           if (fileSync.TryEnterWriteLock(700))
            {
                try
                {
                    using (System.IO.TextWriter tmpFile = new System.IO.StreamWriter(cfgFileName, false))
                    {
                        using (System.IO.MemoryStream stream = new())
                        {
                            DataContractJsonSerializer ds = new(typeof(BoxMatrixCatalog));
                            DataContractJsonSerializerSettings s = new();
                            ds.WriteObject(stream, this);
                            string jsonString = Encoding.UTF8.GetString(stream.ToArray());

                            tmpFile.Write(jsonString);
                            tmpFile.Close();
                           
                        }
                    }
                }
                catch (SecurityException ex)
                {
                    Log.Write("NCG", "SecurityException " + ex.Message + "\n " + cfgFileName, EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
                }
                catch (InvalidOperationException ex)
                {
                    Log.Write("NCG", "InvalidOperationException " + ex.Message + "\n " + cfgFileName, EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
                }
                catch (ArgumentException ex)
                {
                    Log.Write("NCG", "ArgumentException " + ex.Message + "\n " + cfgFileName, EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
                }
                catch (Exception ex)
                {
                    Log.Write("NCG", "Exception " + ex.Message + "\n " + cfgFileName, EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
                }
                finally
                {
                    fileSync.ExitWriteLock();
                }
            }
            else
            {
                Log.Write("NCG.", "Критическая ошибка очереди", EventLogEntryType.Error, MAIN_ERROR_CODE + 2);
            }
        }
        public Task SaveAsync()
        {
            return Task.Factory.StartNew(() => Save());
        }

        
        public bool UpdateNomenclatureCatalog(List<BoxMatrix> uUpdates)
        {
            if (uUpdates == null)
                return false;

            //добавить\обновить записи          
            foreach (BoxMatrix uu in uUpdates)
            {
                BoxMatrix? nm = Catalog.FirstOrDefault(x => x.Name == uu.Name);
                if (nm == default(BoxMatrix))
                {
                    nm = new ();
                    Catalog.Add(nm);
                }

                nm.CopyFrom(uu);
            }

            return true;
        }
        
        public bool UpdateOrAddNomenclatureUnit(BoxMatrix unit)
        {
            if (unit == null)
                return false;

            BoxMatrix? nm = Catalog.FirstOrDefault(x => x.Name == unit.Name);
            if (nm == default(BoxMatrix))
            {
                nm = new ();
                catalog.Add(nm);
            }

            nm.CopyFrom(unit);
            return true;
        }
        public bool UpdateNomenclatureUnit(BoxMatrix unit, BoxMatrix oldUnit)
        {
            if (unit == null)
                return false;

            BoxMatrix? nm = Catalog.FirstOrDefault(x => x.Name == oldUnit.Name);
            if (nm == default(BoxMatrix))
                return false;

            nm.CopyFrom(unit);
            return true;
        }
        public bool DeleteNomenclature(BoxMatrix unit)
        {
            if (unit == null)
                return false;

            BoxMatrix? nm = Catalog.FirstOrDefault(x => x.Name == unit.Name);
            if (nm == default(BoxMatrix))
                return false;

            return Catalog.Remove(nm);
        }
        #endregion;

    }
}

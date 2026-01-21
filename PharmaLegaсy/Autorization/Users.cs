using System;
using System.Xml.Serialization;
using System.IO;

using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Util;

namespace Autorization
{
    [Serializable]
    [XmlRootAttribute("user", Namespace = "", IsNullable = false)]
    public class User
    {
        public User() : this("-1") { }
        public User(string id)
        {
            ID = id;
        }
        public string Name { get; set; }
        public string Password { get; set; }
        public string ID { get; set; }
        public string Hash { get; set; }
        public bool IsControler { get; set; }
        public bool IsMaster { get; set; }
        public bool IsServiceMen { get; set; }

    }

    [Serializable]
    public class UsersCatalog
    {
        // обявление класса типа ArrayList для авто сохранения хмл"
        [XmlArray("users"), XmlArrayItem("user", typeof(User))]
        public System.Collections.ArrayList PositionsDataArray = new System.Collections.ArrayList();

        public UsersCatalog()
        {
        }
        public static bool UpdateUser(User user)
        {
            try
            {
                if (user == null)
                    return false;

                //return true;

                string filename = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\usersCatalog.xml";
                bool updateUser = false;
                UsersCatalog catalog = new UsersCatalog();
                XmlSerializer serializer = new XmlSerializer(typeof(UsersCatalog));

                //если файла нет создать его
                if (!File.Exists(filename))
                {

                }
                else
                {
                    FileStream fs = new FileStream(filename, FileMode.Open);
                    if (fs != null)
                    {

                        catalog = (UsersCatalog)serializer.Deserialize(fs);

                        fs.Close();


                        for (int i = 0; i < catalog.PositionsDataArray.Count; i++)
                        {
                            if (((User)catalog.PositionsDataArray[i]).ID == user.ID)
                            {
                                updateUser = true;
                                catalog.PositionsDataArray[i] = user;

                            }

                        }
                    }
                    else
                        throw new Exception("Не возможно открыть файл каталога: " + filename);
                }

                if (!updateUser)
                {
                    catalog.PositionsDataArray.Add(user);
                }

                //сохранить на винт
                TextWriter writer = new StreamWriter(filename);
                serializer.Serialize(writer, catalog);
                writer.Flush();
                writer.Close();
                return true;

            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString());
            }
            return false;
        }
        public static User GetUser(string name, string pass)
        {
            try
            {
                string filename = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\usersCatalog.xml";

                using (FileStream fs = new FileStream(filename, FileMode.Open))
                {
                    if (fs != null)
                    {
                        XmlSerializer serializer = new XmlSerializer(typeof(UsersCatalog));
                        UsersCatalog catalog;
                        catalog = (UsersCatalog)serializer.Deserialize(fs);

                        foreach (User u in catalog.PositionsDataArray)
                        {
                            if (u.Password == null)//авторизация по хешу
                            {
                                string token = Autorization.AuthUser1C.CalcUserToken1C(name, pass);
                                if ((u.Name == name) && (u.Hash == token))
                                    return u;

                            }
                            else //авторизация по паролю старая. на всяк оставлена
                            {
                                if ((u.Name == name) && (u.Password == pass))
                                    return u;
                            }
                        }
                    }
                    else
                        throw new Exception("Не возможно открыть файл каталога: " + filename);


                }
            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString());
            }
            return null;
        }
        public static bool CreateDefault()
        {
            // сформировать хмл
            UsersCatalog uc = new UsersCatalog();
            /*
            User user = new User();
            user.Name = "m";
            user.Password = "m";
            user.ID = "1";
            user.IsMaster = true;
            user.IsControler = true;
            user.IsServiceMen = true;
            uc.PositionsDataArray.Add(user);*/

            try
            {
                //получить пусть к программе
                string filename = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\usersCatalog.xml";
                XmlSerializer serializer = new XmlSerializer(typeof(UsersCatalog));
                TextWriter writer = new StreamWriter(filename);
                serializer.Serialize(writer, uc);
                writer.Close();
                return true;
            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString());

            }
            return false;
        }

        public static bool CheckCatalog()
        {
            try
            {
                string filename = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName) + "\\usersCatalog.xml";

                if (!File.Exists(filename))
                    return UsersCatalog.CreateDefault();
            }
            catch (Exception ex)
            {
                Log.Write(ex.ToString());
            }
            return true;


        }

    }

    class UserAutorization
    {
        public static User GetUser(string login1cSrv,string pass1cSrv,string name, string pin, string upAuthCentrUrl, out bool RemoteSuccess)
        {
            User user;
            RemoteSuccess = true;

            try
            {

                //авторизация по 1С
                Autorization.HttpRequestResult result;
                Autorization.User1C us = Autorization.AuthUser1C.GetReguest<Autorization.User1C>(upAuthCentrUrl, out result,
                   login1cSrv,pass1cSrv, name, pin);
                if (us != null)
                {
                    user = new Autorization.User(us.ID);
                    user.IsControler = us.Сontroller;
                    user.IsMaster = us.Master;
                    user.IsServiceMen = us.ServiceMan;
                    user.Name = name;
                    user.Hash = us.Hash;

                    //получили пользователя теперь обновляем его в локальном кеше
                    Task.Factory.StartNew(() => { Autorization.UsersCatalog.UpdateUser(user); });
                    return user;
                }
                else if (result.resultCode == System.Net.HttpStatusCode.Unauthorized)
                {
                    return null;
                }

                Log.Write("Ошибка доступа к удаленному серверу: " + result.resultData);
                RemoteSuccess = false;

                //если не прошла авторизируем по внутреннему справочнику
                user = Autorization.UsersCatalog.GetUser(name, pin);
                if (user != null)
                    return user;


            }
            catch (Exception ex)
            {
                Log.Write("Ошибка обработки  каталога пользователей:" + ex.Message);
            }
            return null;
        }
    }

    [Serializable]
    public class RemoteServerErrorException : Exception
    {
        public RemoteServerErrorException(string message) : base(message)
        {
            //Message = msg;
        }
    }
}
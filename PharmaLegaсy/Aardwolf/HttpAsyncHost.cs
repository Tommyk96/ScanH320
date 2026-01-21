using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

using System.Threading;
using System.Threading.Tasks;

namespace Aardwolf
{
    public sealed class HttpAsyncHost : IHttpAsyncHost
    {
        HttpListener _listener;
        IHttpAsyncHandler _handler;
        HostContext _hostContext;
        ConfigurationDictionary _configValues;
        readonly int _accepts;

        /// <summary>
        /// Creates an asynchronous HTTP host.
        /// </summary>
        /// <param name="handler">Handler to serve requests with</param>
        /// <param name="accepts">
        /// Higher values mean more connections can be maintained yet at a much slower average response time; fewer connections will be rejected.
        /// Lower values mean less connections can be maintained yet at a much faster average response time; more connections will be rejected.
        /// </param>
        public HttpAsyncHost(IHttpAsyncHandler handler, int accepts = 4)
        {
            _handler = handler ?? NullHttpAsyncHandler.Default;
            _listener = new HttpListener();
            // Multiply by number of cores:
            _accepts = accepts * Environment.ProcessorCount;
        }

        class HostContext : IHttpAsyncHostHandlerContext
        {
            public IHttpAsyncHost Host { get; private set; }
            public IHttpAsyncHandler Handler { get; private set; }

            public HostContext(IHttpAsyncHost host, IHttpAsyncHandler handler)
            {
                Host = host;
                Handler = handler;
            }
        }

        public List<string> Prefixes
        {
            get { return _listener.Prefixes.ToList(); }
        }

        public void SetConfiguration(ConfigurationDictionary values)
        {
            _configValues = values;
        }

        public void Run(params string[] uriPrefixes)
        {
            // Establish a host-handler context:
            _hostContext = new HostContext(this, _handler);

            _listener.IgnoreWriteExceptions = true;

            // Add the server bindings:
            foreach (var prefix in uriPrefixes)
                _listener.Prefixes.Add(prefix);

            Task.Run(async () =>
            {
                Thread.CurrentThread.Name = "HttpServer";
                // Configure the handler:
                if (_configValues != null)
                {
                    var config = _handler as IConfigurationTrait;
                    if (config != null)
                    {
                        var task = config.Configure(_hostContext, _configValues);
                        if (task != null)
                            if (!await task) return;
                    }
                }

                // Initialize the handler:
                var init = _handler as IInitializationTrait;
                if (init != null)
                {
                    var task = init.Initialize(_hostContext);
                    if (task != null)
                        if (!await task) return;
                }

                try
                {
                    // Start the HTTP listener:
                    _listener.Start();
                }
                catch (HttpListenerException hlex)
                {
                    //при получении запрета изза прав. попытатся прописать права.
                    if (hlex.NativeErrorCode == 5)
                    {
                        foreach (var prefix in uriPrefixes)
                            NetAclChecker.AddAddress(prefix);

                       
                        try {

                            #region разблокировать порт
                            string result = System.Text.RegularExpressions.Regex.Matches(uriPrefixes[0], @"\:([0-9]+)")
                            .Cast<System.Text.RegularExpressions.Match>()
                            .Aggregate("", (s, e) => s + e.Value, s => s);

                            if (result?.Length > 1)
                                result = result.Remove(0, 1);

                            NetAclChecker.AddPort(result);
                            #endregion

                            _listener.Start();
                        }
                        catch (HttpListenerException hlex1)
                        {
                            if (hlex.NativeErrorCode == 5)
                            {
                                string msg = "Ошибка создания листенера: " + hlex1.Message + @"Возможно не настроены сетевые параметры http.
Пример: 
    netsh http add iplisten 0.0.0.0
    netsh http add urlacl url = http://*:7080// user=\Все
    netsh http add urlacl url = https://*:7080/jobs/ user=\Все
--
Создать сертификат:
    makecert -n " + '"' + "CN = Line" + '"' + @" -r -sv c:\Line.pvk c:\Line.cer" +
          @"makecert -sk LineSigned -iv c:\Line.pvk -n " + '"' + "CN = LineSigned" + '"' + @" -ic c:\Line.cer LineSigned.cer -sr localmachine -ss My" +
          @"выбор сертификата:
    netsh http add sslcert ipport = 0.0.0.0:7080 certhash = 585947f104b5bce53239f02d1c6fed06832f47dc appid = {df8c8073-5a4b-4810-b469-5975a9c95230}";

                                Util.Log.Write(msg);
                                return;
                            }

                            Util.Log.Write("Ошибка создания листенера: " + hlex1.Message);
                            return;
                        }
                        catch (Exception ex)
                        {
                            Util.Log.Write("Ошибка создания листенера: " + ex.Message);
                        }
                    }
                    else
                    {
                        Util.Log.Write("Ошибка создания листенера: " + hlex.Message);
                        return;
                    }
                }
                catch (Exception ex)
                {
                    Util.Log.Write("Ошибка создания листенера: " + ex.Message);
                }

            // Accept connections:
            // Higher values mean more connections can be maintained yet at a much slower average response time; fewer connections will be rejected.
            // Lower values mean less connections can be maintained yet at a much faster average response time; more connections will be rejected.
            var sem = new Semaphore(_accepts, _accepts);

                while (true)
                {
                    sem.WaitOne(); 

                    //524 43 52 - справочная терапии
                    //529 54 43 - заведующий первой терапии.


#pragma warning disable 4014
                    _listener.GetContextAsync().ContinueWith(async (t) =>
                    {
                        string errMessage;

                        try
                        {
                            sem.Release();

                            var ctx = await t;
                            await ProcessListenerContext(ctx, this);
                            return;
                        }
                        catch (Exception ex)
                        {
                            errMessage = ex.ToString();
                        }

                        await Console.Error.WriteLineAsync(errMessage);
                    });
#pragma warning restore 4014
                }
            }).Wait();
        }

        static async Task ProcessListenerContext(HttpListenerContext listenerContext, HttpAsyncHost host)
        {
            Debug.Assert(listenerContext != null);

            try
            {
                // Get the response action to take:
                var requestContext = new HttpRequestContext(host._hostContext, listenerContext.Request, listenerContext.User);
                var action = await host._handler.Execute(requestContext);
                if (action != null)
                {
                    // Take the action and await its completion:
                    var responseContext = new HttpRequestResponseContext(requestContext, listenerContext.Response);
                    var task = action.Execute(responseContext);
                    if (task != null) await task;
                }

                // Close the response and send it to the client:
                listenerContext.Response.Close();
            }
            catch (HttpListenerException)
            {
                // Ignored.
            }
            catch (Exception ex)
            {
                // TODO: better exception handling
                Trace.WriteLine(ex.ToString());
            }
        }
    }
}

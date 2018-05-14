using Engine;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;

namespace SimpleWebServer
{
    /// <summary>
    /// HTTP Server
    /// </summary>
    public class Server : IDisposable
    {
        /// <summary>
        /// Gets the base URL of the HTTP listener
        /// </summary>
        public string BaseURL { get; private set; }
        /// <summary>
        /// Gets if the HTTP server is listening
        /// </summary>
        public bool IsListening
        {
            get
            {
                return L != null && L.IsListening;
            }
        }

        private Controller[] Controllers;
        private HttpListener L;

        /// <summary>
        /// Checks if the given Port is valid
        /// </summary>
        /// <param name="Port">Port number</param>
        /// <returns>true if value</returns>
        /// <remarks>Excludes the lowest and highest possible values. Ignores admin requirement for low ports</remarks>
        public static bool IsValidPort(int Port)
        {
            return Port > ushort.MinValue && Port < ushort.MaxValue;
        }

        /// <summary>
        /// Creates and starts a HTTP listener
        /// </summary>
        /// <param name="Port">Port number</param>
        /// <param name="StartBrowser">true to launch the users web browser after a sucessful start</param>
        /// <param name="CertBasePath">Base path for certificate and key files</param>
        public Server(int Port, Controller[] Controllers, bool StartBrowser = false)
        {
            if (!IsValidPort(Port))
            {
                throw new ArgumentOutOfRangeException("Port");
            }

            this.Controllers = Controllers;

            BaseURL = $"http://localhost:{Port}/";
            Logger.Info("HTTP: Starting Webserver on {0}", BaseURL);
            L = new HttpListener();
            L.Prefixes.Add(BaseURL);
            L.IgnoreWriteExceptions = true;
            try
            {
                L.Start();
            }
            catch (Exception ex)
            {
                Logger.Error("Unable to start HTTP server. Reason: {0}", ex.Message);
            }
            if (L.IsListening)
            {
                L.BeginGetContext(conin, L);
                if (StartBrowser)
                {
                    try
                    {
                        //Calling Dispose() yourself will somehow throw an exception
                        //but with the using(...) it does not.
                        using (Process.Start(BaseURL)) { }
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn("HTTP: Unable to start browser for {0}. Reason: {1}", Port, ex.Message);
                    }
                }
            }
        }

        /// <summary>
        /// Stops and disposes the component
        /// </summary>
        public void Dispose()
        {
            Logger.Debug("HTTP: Disposing Webserver");
            Shutdown();
        }

        /// <summary>
        /// Shuts down the server
        /// </summary>
        /// <remarks>As of now there is no way to restart an existing instance</remarks>
        public void Shutdown()
        {
            lock (this)
            {
                if (L != null)
                {
                    if (L.IsListening)
                    {
                        Logger.Info("HTTP: Server shutdown");
                        L.Stop();
                        L = null;
                    }
                    else
                    {
                        Logger.Debug("HTTP: Shutdown skipped. Server was never created successfully");
                    }
                }
                else
                {
                    Logger.Info("HTTP: Server shutdown attempt but was already");
                }
            }
        }

        #region HTTP Connection

        private void conin(IAsyncResult ar)
        {
            var L = (HttpListener)ar.AsyncState;
            if (L != null && L.IsListening)
            {
                var ctx = L.EndGetContext(ar);
                HandleRequest(ctx);
                L.BeginGetContext(conin, L);
            }
        }

        private void HandleRequest(HttpListenerContext ctx)
        {
            if (ctx != null)
            {
                Thread T = new Thread(Answer);
                T.Priority = ThreadPriority.BelowNormal;
                T.IsBackground = true;
                T.Start(ctx);
            }
        }

        private void Answer(object o)
        {
            var ctx = (HttpListenerContext)o;
            if (ctx != null)
            {
                var Parts = ctx.Request.Url.Segments.Select(m => m.Trim().Trim('/')).ToArray();
                var Controller = "Home";
                var Method = "Index";

                if (Parts.Length > 1)
                {
                    Controller = Parts[1];
                }
                if (Parts.Length > 2)
                {
                    Method = Parts[2];
                }

                Logger.Log("HTTP: {0} {1}/{2}", ctx.Request.HttpMethod, Controller, Method);
                try
                {
                    Deliver(ctx, Controller, Method);
                }
                catch (Exception ex)
                {
                    Logger.Warn("HTTP: Unexpected Error: {0}\r\n\r\nTrace:\r\n{1}", ex.Message, ex.StackTrace);
                    HTTP.HTTP500(ctx, ex);
                }
            }
        }

        private void Deliver(HttpListenerContext ctx, string Controller, string Method)
        {
            var C = Controllers.FirstOrDefault(m => m.Name == Controller);
            if (C != null)
            {
                if (C.Methods.Any(m => m == Method))
                {
                    if (!C.Call(Method, ctx))
                    {
                        HTTP.HTTP404(ctx);
                    }
                    return;
                }
                else
                {
                    Logger.Debug("Controller {0} has no method {1}", Controller, Method);
                }
            }
            else
            {
                Logger.Debug("No Controller {0}", Controller);
            }
            if (Controller != "_")
            {
                Deliver(ctx, "_", "HTTP404");
            }
            else
            {
                HTTP.HTTP404(ctx);
            }
        }

        #endregion
    }
}

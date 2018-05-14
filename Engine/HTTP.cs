using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Engine
{
    public class HTTP
    {
        public static void SendBinary(HttpListenerContext ctx, byte[] Content, string FakeName, bool IsUtf8 = false)
        {
            Logger.Debug("HTTP: Sending {0} ({1} bytes)", FakeName, Content == null ? 0 : Content.Length);
            ctx.Response.ContentType = MimeTypeLookup.GetMimeType(FakeName);
            ctx.Response.ContentEncoding = IsUtf8 ? Encoding.UTF8 : Encoding.Default;
            ctx.Response.Close(Content, false);
        }

        public static void SendString(HttpListenerContext ctx, string Content, string FakeName)
        {
            SendBinary(ctx, Encoding.UTF8.GetBytes(Content), FakeName, true);
        }

        public static void SendFile(HttpListenerContext ctx, string FileName, bool Cache = true)
        {
            Logger.Debug("HTTP: Sending {0}", FileName);
            var Hdr = Tools.StrOrDefault(ctx.Request.Headers["If-None-Match"]);
            var DT = File.GetLastWriteTimeUtc(FileName).Ticks;
            if (Cache)
            {
                ctx.Response.AddHeader("ETag", DT.ToString());
            }
            if (Cache && Hdr == DT.ToString())
            {
                ctx.Response.StatusCode = 304;
                ctx.Response.Close();
            }
            else
            {
                ctx.Response.ContentType = MimeTypeLookup.GetMimeType(FileName);
                ctx.Response.ContentLength64 = (new FileInfo(FileName)).Length;
                using (var FS = File.OpenRead(FileName))
                {
                    FS.CopyTo(ctx.Response.OutputStream);
                }
                ctx.Response.Close();
            }
        }

        public static void SendJson(HttpListenerContext ctx, object O)
        {
            Logger.Debug("HTTP: Sending JSON for {0}", O);
            ctx.Response.ContentType = "application/json";
            ctx.Response.ContentEncoding = Encoding.UTF8;
            ctx.Response.Close(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(O)), false);
        }

        public static void Redirect(HttpListenerContext ctx, string NewUrl, bool Permanent = false)
        {
            Logger.Debug("HTTP: Redirecting to {0} Permanent={1}", NewUrl, Permanent);
            ctx.Response.StatusCode = Permanent ? 301 : 307;
            ctx.Response.Headers.Add(HttpResponseHeader.Location, NewUrl);
            ctx.Response.Close();
        }

        public static void HTTP404(HttpListenerContext ctx)
        {
            Logger.Debug("HTTP: Sending 404");
            ctx.Response.StatusCode = 404;
            ctx.Response.Close();
        }

        public static void HTTP500(HttpListenerContext ctx, Exception ex)
        {
            var BaseResult = @"HTTP 500 - I screwed up.
Due to an unforseen error we are unable to execute your current request.

Details
";
            if (ex == null)
            {
                ex = new Exception("Unknown error");
            }
            while (ex != null)
            {
                BaseResult += string.Format(@"==================================

Error: {0}

Location:
{1}
", ex.Message, ex.StackTrace);
                ex = ex.InnerException;
            }
            Logger.Warn("HTTP: Sending 500 due to server error.");
            try
            {
                ctx.Response.StatusCode = 500;
                ctx.Response.ContentType = "text/plain";
                ctx.Response.ContentEncoding = Encoding.UTF8;
                ctx.Response.Close(Encoding.UTF8.GetBytes(BaseResult), false);
            }
            catch (Exception E)
            {
                Logger.Error("HTTP: Unable to send HTTP 500. Message: {0}", E.Message);
                try
                {
                    ctx.Response.Abort();
                }
                catch
                {
                    //At this point we no longer care
                }
            }
        }
    }
}

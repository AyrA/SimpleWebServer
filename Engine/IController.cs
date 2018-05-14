using System.Net;

namespace Engine
{
    public class IController
    {
        [Hidden]
        public static void NotFound(HttpListenerContext ctx)
        {
            HTTP.HTTP404(ctx);
        }

        [Hidden]
        public static void Redirect(HttpListenerContext ctx, string URL, bool Permanent = false)
        {
            HTTP.Redirect(ctx, URL, Permanent);
        }

        [Hidden]
        public static void SendJson(HttpListenerContext ctx, object O)
        {
            HTTP.SendJson(ctx, O);
        }
    }
}

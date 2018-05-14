using Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;

namespace SimpleWebServer
{
    /// <summary>
    /// Represents a controller
    /// </summary>
    public class Controller
    {
        /// <summary>
        /// Gets the underlying Controller Type
        /// </summary>
        public Type Type { get; private set; }
        /// <summary>
        /// Gets the Controller Name
        /// </summary>
        public string Name { get; private set; }
        /// <summary>
        /// Gets the Method Name
        /// </summary>
        public string[] Methods { get; private set; }

        /// <summary>
        /// Initializes a controller from the given Type
        /// </summary>
        /// <param name="T">Controller Type</param>
        public Controller(Type T)
        {
            Type = T;
            Name = T.Name;
            Methods = T.GetMethods()
                .Where(m => IsWebMethod(m))
                .Select(m => m.Name)
                .ToArray();
            Logger.Log("Controller : {0}", Name);
            foreach (var M in Methods)
            {
                Logger.Debug("Export: {0}/{1}", Name, M);
            }
        }

        /// <summary>
        /// Gets if a Method is suitable for HTTP usage
        /// </summary>
        /// <param name="MI">Method Information</param>
        /// <returns>true if Web method</returns>
        public static bool IsWebMethod(MethodInfo MI)
        {
            //Public + Static + !HiddenAttribute
            if (MI.IsPublic && MI.IsStatic && !MI.CustomAttributes.Any(m => m.AttributeType == typeof(HiddenAttribute)))
            {
                //Signature is Method(HttpListenerContext)
                var Params = MI.GetParameters();
                return
                    Params != null &&
                    Params.Length == 1 &&
                    Params[0].ParameterType == typeof(HttpListenerContext);
            }
            return false;
        }

        /// <summary>
        /// Gets all controllers from an Assembly
        /// </summary>
        /// <param name="A">Assembly</param>
        /// <returns>Array of Controller</returns>
        public static Controller[] GetControllers(Assembly A)
        {
            return A.DefinedTypes
                .Where(m => m.IsPublic && m.IsClass && GetTypeTree(m).Any(n => n == typeof(IController)))
                .Select(m => new Controller(m))
                .ToArray();
        }

        private static Type[] GetTypeTree(object o)
        {
            if (o == null)
            {
                return new Type[0];
            }
            return GetTypeTree(o.GetType());
        }

        private static Type[] GetTypeTree(Type T)
        {
            var L = new List<Type>();
            while (T != null)
            {
                L.Add(T);
                T = T.BaseType;
            }
            return L.ToArray();
        }

        /// <summary>
        /// Calls a method from this controller
        /// </summary>
        /// <param name="Method">Method name</param>
        /// <param name="ctx">Context</param>
        /// <returns>true if method found and called</returns>
        public bool Call(string Method, HttpListenerContext ctx)
        {
            if (Methods.Any(m => m == Method))
            {
                Type.GetMethod(Method).Invoke(null, new object[] { ctx });
                return true;
            }
            return false;
        }
    }
}

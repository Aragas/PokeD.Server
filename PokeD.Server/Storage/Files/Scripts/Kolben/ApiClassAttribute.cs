using System;

namespace PokeD.Server.Storage.Files
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ApiClassAttribute : Attribute
    {
        public string ClassName { get; set; }

        public ApiClassAttribute(string className) => ClassName = className;
    }
}
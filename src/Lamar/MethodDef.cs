using System;
using System.Collections.Generic;

namespace Lamar
{
    public class MethodDef
    {
        public string Name { get; private set; }
        public MemberAccess Access = MemberAccess.Public;
        public readonly IList<ArgDef> Args = new List<ArgDef>();

        public Type ReturnType;
        public string ReturnTypeName;

        public MethodDef(string name)
        {
            Name = name;
        }

        public MethodDef Returns<T>()
        {
            ReturnType = typeof(T);
            return this;
        }

        public MethodDef Returns(string typeName)
        {
            ReturnTypeName = typeName;
            return this;
        }

        public MethodDef WithArg<T>(string name)
        {
            var arg = new ArgDef(name) { Type = typeof(T) };
            Args.Add(arg);
            return this;
        }

        public MethodDef WithArg(string name, string typeName)
        {
            var arg = new ArgDef(name) { TypeName = typeName };
            Args.Add(arg);

            return this;
        }

        public void WriteDeclaration(SourceWriter sourceWriter)
        {
            throw new NotImplementedException();
        }
    }
}
using System;

namespace Lamar
{
    public class ArgDef
    {
        public Type Type { get; set; }
        public string TypeName { get; set; }
        public string Name { get; set; }

        public ArgDef(string name)
        {
            Name = name;
        }
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Baseline;

namespace Lamar
{
    public class AssemblyGenerator
    {
        private readonly IList<MetadataReference> _references = new List<MetadataReference>();

        public static string[] HintPaths { get; set; }

        public AssemblyGenerator()
        {
            ReferenceAssemblyContainingType<object>();
            ReferenceAssembly(typeof(Enumerable).Assembly);
        }

        public void ReferenceAssembly(Assembly assembly)
        {
            try
            {
                var referencePath = CreateAssemblyReference(assembly);

                if (referencePath == null)
                {
                    Console.WriteLine($"Could not make an assembly reference to {assembly.FullName}");
                    return;
                }

                var alreadyReferenced = _references.Any(x => x.Display == referencePath);
                if (alreadyReferenced)
                    return;

                var reference = MetadataReference.CreateFromFile(referencePath);

                _references.Add(reference);

                foreach (var assemblyName in assembly.GetReferencedAssemblies())
                {
                    var referencedAssembly = Assembly.Load(assemblyName);
                    ReferenceAssembly(referencedAssembly);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Could not make an assembly reference to {assembly.FullName}\n\n{e}");
            }
        }

        private static String CreateAssemblyReference(Assembly assembly)
        {
            if (string.IsNullOrEmpty(assembly.Location))
            {
                var path = GetPath(assembly);
                return path != null ? path : null;
            }
            return assembly.Location;
        }

        private static String GetPath(Assembly assembly)
        {
            return HintPaths?
                .Select(FindFile(assembly))
                .FirstOrDefault(file => file.IsNotEmpty());
        }

        private static Func<String, String> FindFile(Assembly assembly)
        {
            return hintPath =>
            {
                var name = assembly.GetName().Name;
                Console.WriteLine($"Find {name}.dll in {hintPath}");
                var files = Directory.GetFiles(hintPath, name + ".dll", SearchOption.AllDirectories);
                var firstOrDefault = files.FirstOrDefault();
                if (firstOrDefault != null)
                {
                    Console.WriteLine($"Found {name}.dll in {firstOrDefault}");
                }
                return firstOrDefault;
            };
        }

        public void ReferenceAssemblyContainingType<T>()
        {
            ReferenceAssembly(typeof(T).Assembly);
        }

        public Assembly Generate(string code)
        {
            var assemblyName = Path.GetRandomFileName();
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            var references = _references.ToArray();
            var compilation = CSharpCompilation.Create(assemblyName, new[] { syntaxTree }, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));


            using (var stream = new MemoryStream())
            {
                var result = compilation.Emit(stream);

                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(diagnostic =>
                        diagnostic.IsWarningAsError ||
                        diagnostic.Severity == DiagnosticSeverity.Error);


                    var message = failures.Select(x => $"{x.Id}: {x.GetMessage()}").Join("\n");
                    throw new InvalidOperationException("Compilation failures!\n\n" + message + "\n\nCode:\n\n" + code);
                }

                stream.Seek(0, SeekOrigin.Begin);
                return Assembly.Load(stream.ToArray());
            }
        }
    }

    public class SourceWriter
    {
        private readonly StringWriter _writer = new StringWriter();

        private int _level = 0;
        private string _leadingSpaces = "";

        public int IndentionLevel
        {
            get { return _level; }
            set
            {
                _level = value;
                _leadingSpaces = "".PadRight(_level * 4);
            }
        }

        public void WriteLine(string text)
        {
            _writer.WriteLine(_leadingSpaces + text);
        }

        public void BlankLine()
        {
            _writer.WriteLine();
        }

        public void Write(string text = null)
        {
            if (text.IsEmpty())
            {
                BlankLine();
                return;
            }

            text.ReadLines(line =>
            {
                line = line.Replace('`', '"');

                if (line.IsEmpty())
                {
                    BlankLine();
                }
                else if (line.StartsWith("BLOCK:"))
                {
                    WriteLine(line.Substring(6));
                    StartBlock();
                }
                else if (line.StartsWith("END"))
                {
                    FinishBlock(line.Substring(3));
                }
                else
                {
                    WriteLine(line);
                }

            });


        }

        public void StartNamespace(string @namespace)
        {
            WriteLine($"namespace {@namespace}");
            StartBlock();
        }

        private void StartBlock()
        {

            WriteLine("{");
            IndentionLevel++;
        }

        public void FinishBlock(string extra = null)
        {
            IndentionLevel--;

            if (extra.IsEmpty())
            {
                WriteLine("}");
            }
            else
            {
                WriteLine("}" + extra);
            }


            BlankLine();
        }

        public IDisposable WriteMethod(MethodDef method)
        {
            method.WriteDeclaration(this);
            return InBlock();
        }

        public IDisposable InBlock(string declaration = null)
        {
            if (declaration.IsNotEmpty())
            {
                WriteLine(declaration);
            }
            StartBlock();
            return new BlockMarker(this);
        }

        public IDisposable StartClass(string declaration)
        {
            WriteLine(declaration);
            return InBlock();
        }

        public string Code()
        {
            return _writer.ToString();
        }
    }

    internal class BlockMarker : IDisposable
    {
        private readonly SourceWriter _parent;

        public BlockMarker(SourceWriter parent)
        {
            _parent = parent;
        }

        public void Dispose()
        {
            _parent.FinishBlock();
        }
    }



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

    public enum MemberAccess
    {
        Public,
        Private,
        Internal
    }

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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using NSwag;
using NSwag.CodeGeneration.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;


namespace AutomotiveWorld.TBD
{
    internal class CodeGenerator
    {
        public static async Task Generate()
        {
            string url = "https://raw.githubusercontent.com/Azure/azure-rest-api-specs/main/specification/iotsecurity/resource-manager/Microsoft.IoTSecurity/preview/2021-02-01-preview/devices.json";

            //Device device = new Device();
            CodeGenerator codeGenerator = new CodeGenerator();
            Assembly x = await codeGenerator.AssemblyFromUrlAsync(url);
        }

        private static Assembly GenerateAssembly(string code)
        {
            Assembly asm = null;
            var runtimeAssemblyDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            string fileName = "mylib.dll";
            // Detect the file location for the library that defines the object type
            var systemRefLocation = typeof(object).GetTypeInfo().Assembly.Location;
            // Create a reference to the library
            var systemReference = MetadataReference.CreateFromFile(systemRefLocation);
            // A single, immutable invocation to the compiler
            // to produce a library
            var compilation = CSharpCompilation.Create(fileName)
              .WithOptions(
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
              .AddReferences(systemReference)
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "netstandard.dll")))
              //.AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "System.dll")))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "System.Runtime.dll")))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "System.Runtime.Serialization.dll")))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "System.Net.Http.dll")))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "System.Linq.dll")))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "Newtonsoft.Json.dll")))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "System.ComponentModel.dll")))
              .AddReferences(MetadataReference.CreateFromFile(Path.Combine(runtimeAssemblyDirectory, "System.Private.Uri.dll")))
              .AddSyntaxTrees(tree);
            string path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            EmitResult compilationResult = compilation.Emit(path);
            if (compilationResult.Success)
            {
                asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                //// Invoke the RoslynCore.Helper.CalculateCircleArea method passing an argument
                //double radius = 10;
                //object result =
                //  asm.GetType("RoslynCore.Helper").GetMethod("CalculateCircleArea").
                //  Invoke(null, new object[] { radius });
                //Console.WriteLine($"Circle area with radius = {radius} is {result}");
            }
            else
            {
                foreach (Diagnostic codeIssue in compilationResult.Diagnostics)
                {
                    string issue = $"ID: {codeIssue.Id}, Message: {codeIssue.GetMessage()}, Location: {codeIssue.Location.GetLineSpan()}, Severity: {codeIssue.Severity}";
                    Console.WriteLine(issue);
                }
                //throw new Exception("Failed to generate assembly");
            }

            return asm;
        }

        public async Task<Assembly> AssemblyFromUrlAsync(string url)
        {
            string code = null;

            try
            {
                var document = await OpenApiDocument.FromUrlAsync(url);

                var settings = new CSharpClientGeneratorSettings
                {
                    ClassName = "Device",

                    CSharpGeneratorSettings =
                    {
                        Namespace = "Microsoft.IoTSecurity.Devices"
                    },
                    InjectHttpClient = false
                };

                var generator = new CSharpClientGenerator(document, settings);
                code = generator.GenerateFile();
                var usings = new List<string>()
                {
                    "using System;",
                    "using System.ComponentModel;",
                    "using Newtonsoft.Json;",
                    "using Newtonsoft.Json.Converters;",
            };
                code = "using System;\nusing System.ComponentModel;\nusing Newtonsoft.Json;\nusing Newtonsoft.Json.Converters;" + code;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Assembly asm = GenerateAssembly(code);

            return asm;
        }
    }
}

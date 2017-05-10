using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WhatProp
{
    class Program
    {
        static Logger logger = new Logger(){ Level=Logger.LogLevel.INFO };

        static readonly string version = Properties.Resources.Version.TrimEnd(System.Environment.NewLine.ToCharArray());

        static int Main(string[] args)
        {
            if (args.Length < 1)
            {
                Console.WriteLine("whatprop/{0}\nUsage: [mono] WhatProp.exe [--debug] <assembly>", version);
                logger.Warning("Not enough arguments");
                return 100; // not enough arguments?
            }

            // control verbosity
            if (args[0] == "--debug")
            {
                logger.Level = Logger.LogLevel.DEBUG;
                args = args.Skip(1).ToArray();
            }

            // again, check if we have enough arguments
            if (args.Length < 1)
            {
                logger.Warning("Not enough arguments");
                return 100; // not enough arguments?
            }

            // first arg is the path to the main assembly being processed
            string fileName = args[0];

            // load module and assembly resolver
            ModuleDefinition module;
            //CustomAssemblyResolver customResolver;
            try
            {
                // second arg and onwards should be paths to reference assemblies
                // instantiate custom assembly resolver that loads reference assemblies into cache
                // note: ONLY these assemblies will be available to the resolver
                //customResolver = new CustomAssemblyResolver(args.Skip(1));

                // load the plugin module (with the custom assembly resolver)
                // TODO: perhaps we should load the plugin assembly then iterate through all modules
                module = ModuleDefinition.ReadModule(fileName, new ReaderParameters
                {
                    //AssemblyResolver = customResolver
                });
            }
            catch (BadImageFormatException)
            {
                logger.Error(fileName + " is not a .NET assembly");
                return 110;
            }
            catch (FileNotFoundException e)
            {
                logger.Error("Couldn't find {0}. Are you sure it exists?", e.FileName);
                return 111;
            }

            if (module.Assembly.Name.Name == "")
            {
                logger.Error ("Assembly has no name. This is unexpected.");
                return 120;
            }

            // extract cached reference assemblies from custom assembly resolver
            // we'll query these later to make sure we only attempt to resolve a reference when the
            // definition is defined in an assembly in this list
            //IDictionary<string, AssemblyDefinition> cache = customResolver.Cache;

            // print assembly name
            logger.Info("{0}\n", module.Assembly.FullName);

            // global failure tracker
            bool failure = false;

            int memberCount = 0;
            // int typeCount = 0;

            List<TypeDefinition> types = GetTypesRecursive(module).ToList();
            //Console.WriteLine("{0} types", types.Count);
            //Console.WriteLine("{0} types", module.Types.Where(x => x.IsEnum == false).Count());
            // Console.WriteLine("types:");

            // iterate over all the TYPES
            foreach (TypeDefinition type in types.OrderBy(x => x.Namespace).ThenBy(y => y.FullName))
            {
                // skip
                if (!type.IsPublic) { continue; }
                if (type.FullName.StartsWith("<") || type.Name.StartsWith("<")) { continue; }

                // prefix
                string prefix = "class";
                if (type.IsInterface) prefix = "interface";
                else if (type.IsEnum) prefix = "enum";
                else if (type.IsValueType) prefix = "struct";
                if (type.IsSealed) prefix = "sealed " + prefix;
                if (!type.IsInterface && type.IsAbstract) prefix = "abstract " + prefix;

                string suffix = type.BaseType != null ? ": " + type.BaseType.FullName : "";

                Console.WriteLine("{1} {0} {2}", type.FullName, prefix, suffix);

                // enum (special case)

                if (type.IsEnum)
                {
                    foreach (FieldDefinition field in type.Fields.OrderBy(x => x.Constant))
                    {
                        //if (!field.IsPublic) continue;
                        if (field.Name == "value__") continue;
                        //if (field.IsStatic) Console.Write("static ");
                        //if (field.HasConstant) Console.Write("const ");
                        Console.WriteLine("  {0} = {1}", field.FullName, field.Constant);
                        //memberCount++;
                    }
                    continue;
                }

                // methods

                foreach (MethodDefinition method in type.Methods.OrderBy(x => x.Name))
                {
                    if (!method.IsPublic) continue;

                    // treat vb parameterized properties as methods
                    if (method.IsGetter && method.Parameters.Count == 0) continue;
                    if (method.IsSetter && method.Parameters.Count == 1) continue;

                    // but allow Item and Value properties, since they work in c#
                    // TODO: check first param is int
                    var allowed_properties = new string[] { "get_Item", "set_Item", "get_Value", "set_Value" };
                    if (allowed_properties.Contains(method.Name)) continue;

                    Console.Write("  ");

                    PrintObsolete(method);

                    prefix = method.IsStatic ? "static " : "";
                    prefix = method.IsVirtual ? "virtual " : prefix;
                    Console.WriteLine("{1}{0}", method.FullName, prefix);
                    //                    if (!method.HasBody) // skip if no body
                    //                        continue;
                    memberCount++;

                    // TODO: out param
                    //foreach (var param in method.Parameters)
                    //{
                    //    Console.WriteLine(param.IsOut);
                    //}
                }

                // properties

                // rename Value property to Item so things are consistent between vb.net and c#
                // do this before sorting properties by name...
                for (int i = 0; i < type.Properties.Count; i++)
                {
                    if (type.Properties[i].Name == "Value")
                        type.Properties[i].Name = "Item";
                }

                foreach (PropertyDefinition property in type.Properties.OrderBy(x => x.Name))
                {
                    //break;
                    bool has_public_getter = property.GetMethod != null && property.GetMethod.IsPublic;
                    bool has_public_setter = property.SetMethod != null && property.SetMethod.IsPublic;
                    if (!(has_public_getter || has_public_setter)) continue;

                    // treat vb parameterized properties as methods (see above)
                    if (property.HasParameters)
                    {
                        // but allow item "Item" property, since it's special
                        if (property.Name != "Item")
                        {
                            continue;
                        }
                    }




                    Console.Write("  ");

                    PrintObsolete(property);

                    if (property.GetMethod != null && property.GetMethod.IsStatic) Console.Write("static ");
                    if (property.GetMethod != null && property.GetMethod.IsVirtual) Console.Write("virtual ");

                    //var prop_string_builder = new StringBuilder("Property: ");
                    var prop_string_builder = new StringBuilder();
                    //prop_string_builder.Append(property.PropertyType.Name + " ");
                    //prop_string_builder.Append(property.Name + " { ");
                    //prop_string_builder.Append(property.FullName.TrimEnd('(', ')') + " { ");
                    prop_string_builder.Append(property.FullName + " { ");
                    if (has_public_getter)
                        prop_string_builder.Append("get; ");
                    if (has_public_setter)
                        prop_string_builder.Append("set; ");
                    prop_string_builder.Append("}");
                    Console.WriteLine(prop_string_builder.ToString());
                    //Console.WriteLine("  - {0}", property.FullName);
                    //Console.WriteLine(property.GetMethod != null);
                    //if (property.GetMethod != null)
                    //    Console.WriteLine(property.GetMethod.IsPublic);
                    //Console.WriteLine(property.SetMethod != null);
                    //if (property.SetMethod != null)
                    //    Console.WriteLine(property.SetMethod.IsPublic);
                    memberCount++;
                }

                // fields

                foreach (FieldDefinition field in type.Fields.OrderBy(x => x.Name))
                {
                    if (!field.IsPublic) continue;
                    //if (field.Name == "value__") continue;
                    if (field.IsStatic) Console.Write("static ");
                    if (field.HasConstant) Console.Write("const ");
                    Console.WriteLine("  {0}", field.FullName);
                    //if (type.IsEnum)
                    //{
                    //    Console.WriteLine(field.Constant);
                    //}
                    memberCount++;
                }

                // events

                foreach (EventDefinition evnt in type.Events.OrderBy(x => x.Name))
                {
                    Console.WriteLine("  {0}", evnt.FullName);
                }
            }

            //Console.WriteLine(memberCount);

            // exit code
            if (failure)
                return 2; // unhandled exception returns 1
            else
                return 0;
        }

        static IEnumerable<TypeDefinition> GetTypesRecursive(ModuleDefinition module)
        {
            return GetAllTypesAndNestedTypesHelper(module.Types);
        }

        /// <summary>
        /// Gets all types and nested types recursively.
        /// </summary>
        /// <param name="types">A bunch of types.</param>
        /// <returns>All the types and their nested types (and their nested types...).</returns>
        static IEnumerable<TypeDefinition> GetAllTypesAndNestedTypesHelper(IEnumerable<TypeDefinition> types)
        {
            // var list = new List<TypeDefinition>();
            foreach (TypeDefinition type in types)
            {
                yield return type;
                if (type.HasNestedTypes)
                {
                    foreach (TypeDefinition nestedType in GetAllTypesAndNestedTypesHelper(type.NestedTypes)) // recursive!
                    {
                        yield return nestedType;
                    }
                }
            }
            // return list;
        }

        /// <summary>
        /// Prints the obsolete.
        /// </summary>
        /// <param name="member">Member.</param>
        static void PrintObsolete(IMemberDefinition member)
        {
            if (member.HasCustomAttributes)
            {
                foreach (CustomAttribute attrib in member.CustomAttributes)
                {
                    if (attrib.AttributeType.Name == "ObsoleteAttribute")
                    {
                        if (attrib.HasConstructorArguments)
                            Console.Write("[Obsolete: {0}]", attrib.ConstructorArguments[0].Value);
                    }
                }
            }
        }
    }

//    class Def
//    {
//        string name;
//
//        Def(string name)
//        {
//            this.name = name;
//        }
//
//        void Print()
//        {
//            Console.WriteLine(this.name);
//        }
//    }
//
//    class TypeDef : Def
//    {
//    }
//
//    class AbstractTypeDef : TypeDef
//    {
//    }
//
//    class InterfaceDef : TypeDef
//    {
//    }
//
//    class MethodDef : Def
//    {
//    }
//
//    class PropertyDef : Def
//    {
//    }



    /// <summary>
    /// Logger.
    /// </summary>
    class Logger
    {
        public enum LogLevel
        {
            ERROR,
            WARNING,
            INFO,
            DEBUG
        }

        public LogLevel Level = LogLevel.DEBUG;

        public void Debug(string format, params object[] args)
        {
            if (Level >= LogLevel.DEBUG)
                Console.WriteLine("DEBUG " + format, args);
        }

        public void Info(string format, params object[] args)
        {
            if (Level >= LogLevel.INFO)
                Console.WriteLine(format, args);
        }

        public void Warning(string format, params object[] args)
        {
            if (Level >= LogLevel.WARNING)
                WriteLine(string.Format(format, args), ConsoleColor.DarkYellow);
        }

        public void Error(string format, params object[] args)
        {
            if (Level >= LogLevel.ERROR)
                WriteLine(string.Format(format, args), ConsoleColor.Red);
        }

        void WriteLine(string message, ConsoleColor color)
        {
            Console.ForegroundColor = color;
            try
            {
                Console.WriteLine(message);
            } finally
            {
                Console.ResetColor();
            }
        }
    }
}

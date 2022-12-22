using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace BaseApp.Tools
{
    public class CodeFile : CodePart
    {
        public string Path { get; set; }
        public bool CreateFile { get; set; }

        public List<string> Usings { get; set; }
        public bool IncludeStandardUsings { get; set; }

        public string NameSpace { get; set; }
        public bool HasNameSpace => !IsNullOrWhitespace(NameSpace);

        public List<Class> Classes { get; set; }

        public List<Member> Members { get; set; }

        public CodeFile()
        {
            Classes = new List<Class>();
            Members = new List<Member>();
        }

        public override string Write(byte tabCount)
        {
            string fileText = string.Empty;

            Usings = Usings ?? new List<string>();
            if (IncludeStandardUsings)
            {
                Usings.Add("System");
                Usings.Add("System.Collections.Generic");
            }

            foreach (string usingItem in Usings)
            {
                fileText += WriteLine(tabCount, JoinWithSpaces("using", usingItem) + GetSemiColon());
            }

            if (Usings.Any())
            {
                fileText += GetNewLine();
            }

            if (HasNameSpace)
            {
                fileText += WriteLine(tabCount, JoinWithSpaces("namespace", NameSpace));
                fileText += WriteLine(tabCount, "{");
                tabCount++;
            }

            if (Classes.Any())
            {
                foreach (Class writeClass in Classes)
                {
                    if (Classes.IndexOf(writeClass) == Classes.Count - 1)
                    {
                        fileText += writeClass.Write(tabCount);
                    }
                    else
                    {
                        fileText += writeClass.WriteLine(tabCount);
                    }
                }
                fileText += GetNewLine();
            }
           
            if (Members.Any())
            {
                foreach (Member member in Members)
                {
                    if (Members.IndexOf(member) == Members.Count - 1)
                    {
                        fileText += member.Write(tabCount);
                    }
                    else
                    {
                        fileText += member.WriteLine(tabCount);
                    }
                }
                fileText += GetNewLine();
            }

            if (HasNameSpace)
            {
                tabCount--;
                fileText += PrependTabs(tabCount, "}");
            }

            return fileText;
        }

        public void WriteToFile()
        {
            if (IsNullOrWhitespace(Path))
            {
                return;
            }

            if (!CreateFile && !File.Exists(Path))
            {
                return;
            }

            StreamWriter writer = new StreamWriter(Path);
            writer.Write(Write(0));
            writer.Flush();
            writer.Close();
        }
    }

    public abstract class CodePart : ICodePart
    {
        public string Name { get; set; }

        public abstract string Write(byte tabCount);

        public virtual string WriteLine(byte tabCount)
        {
            return Write(tabCount) + GetNewLine();
        }

        public static implicit operator string(CodePart codePart)
        {
            return codePart?.Write(0);
        }

        protected static string WriteLine(byte tabCount, string value, bool endWithSemiColon = false)
        {
            string line = $"{PrependTabs(tabCount, value)}";

            if (endWithSemiColon)
            {
                line += GetSemiColon();
            }

            line += GetNewLine();

            return line;
        }

        protected static string GetSemiColon()
        {
            return $";";
        }

        protected static string JoinWithSpaces(params string[] parts)
        {
            return JoinWithSpaces(parts.Select(p => p));
        }

        public static string JoinWithSpaces(IEnumerable<string> parts)
        {
            return string.Join(" ", parts.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim(' ')));
        }

        protected static string PrependTabs(byte tabCount, string value)
        {
            string tabs = GetTabs(tabCount);
            return $"{tabs}{value}";
        }

        protected static bool IsNullOrWhitespace(string value)
        {
            return string.IsNullOrWhiteSpace(value);
        }

        protected static string GetNewLine()
        {
            return "\r\n";
        }

        protected static string GetTabs(byte tabs)
        {
            string text = string.Empty;
            for (int i = 0; i < tabs; i++)
            {
                text += "\t";
            }
            return text;
        }
    }

    public class CodeType
    {
        public string StringType { get; set; }
        public Type Type { get; set; }

        public string TypeNameToUse
        {
            get
            {
                return string.IsNullOrWhiteSpace(StringType)
                    ? Type == null
                        ? string.Empty
                        : Type.FullName
                    : StringType;
            }
        }

        public CodeType(Type type)
        {
            Type = type;
        }

        public CodeType(string stringType)
        {
            StringType = stringType;
        }

        public static implicit operator CodeType(string typeName)
        {
            return new CodeType(typeName);
        }

        public static implicit operator CodeType(Type type)
        {
            return new CodeType(type);
        }

        public static implicit operator string(CodeType codeType)
        {
            return codeType?.TypeNameToUse;
        }
    }

    public abstract class TypedCodePart : CodePart, ITypedCodePart
    {
        public virtual AccessModifiers AccessModifier { get; set; }
        public virtual ConstStaticReadonly ConstStatic { get; set; }
        public virtual CodeType Type { get; set; }

        public virtual string AccessModifierStringToUse
        {
            get
            {
                if (AccessModifier == AccessModifiers.None)
                {
                    return string.Empty;
                }
                return AccessModifier.ToString().ToLower(); ;
            }
        }

        public virtual string ConstStaticStringToUse
        {
            get
            {
                if (ConstStatic == ConstStaticReadonly.None)
                {
                    return string.Empty;
                }
                return ConstStatic.ToString().ToLower();
            }
        }

        public override string Write(byte tabCount)
        {
            return PrependTabs(tabCount, JoinWithSpaces(AccessModifierStringToUse, ConstStaticStringToUse, Type, Name));
        }
    }

    public class Class : TypedCodePart
    {
        public string BaseClass { get; set; }
        public bool HasBaseClass => !IsNullOrWhitespace(BaseClass);
        public List<string> Interfaces { get; set; }
        public bool HasInterfaces => Interfaces != null && Interfaces.Any();
        public string GenericArgument { get; set; }
        public bool HasGenericArgument => !IsNullOrWhitespace(GenericArgument);
        public string GenericTypeRestriction { get; set; }
        public bool HasGenericTypeRestriction => !IsNullOrWhitespace(GenericTypeRestriction);

        public override CodeType Type { get => "class"; set { } }

        public List<Member> Members { get; set; }
        public bool HasMembers => Members != null && Members.Any();

        public List<Class> SubClasses { get; set; }
        public bool HasSubClasses => SubClasses != null && SubClasses.Any();

        public Class() : base()
        {
            Members = new List<Member>();
            SubClasses = new List<Class>(); 
        }

        public override string Write(byte tabCount)
        {
            string classText = base.Write(tabCount);

            if (HasGenericArgument)
            {
                classText += $"<{GenericArgument}>";
            }

            if (HasBaseClass || HasInterfaces)
            {
                classText = $"{classText} : ";
            }

            if (HasBaseClass)
            {
                classText += BaseClass;
            }

            if (HasInterfaces)
            {
                if (HasBaseClass)
                {
                    classText += ", ";
                }

                classText += string.Join(", ", Interfaces);
            }

            if (HasGenericArgument && HasGenericTypeRestriction)
            {
                classText = JoinWithSpaces(classText, "where", GenericArgument, ":", GenericTypeRestriction);
            }

            classText += GetNewLine();
            classText += WriteLine(tabCount, "{");

            tabCount++;

            if (HasMembers)
            {
                foreach (Member member in Members)
                {
                    if (Members.IndexOf(member) == Members.Count - 1)
                    {
                        classText += member.Write(tabCount);
                    }
                    else
                    {
                        classText += member.WriteLine(tabCount);
                    }
                }
                classText += GetNewLine();
            }


            if (HasSubClasses)
            {
                foreach (Class classGenerator in SubClasses)
                {
                    if (SubClasses.IndexOf(classGenerator) == SubClasses.Count - 1)
                    {
                        classText += classGenerator.Write(tabCount);
                    }
                    else
                    {
                        classText += classGenerator.WriteLine(tabCount);
                    }
                }
                classText += GetNewLine();
            }

            tabCount--;

            classText += PrependTabs(tabCount, "}");

            return classText;
        }
    }

    public class Enum : Collection
    {
        public override CodeType Type { get => "enum" ; set { } }
        public override ConstStaticReadonly ConstStatic { get =>  ConstStaticReadonly.None; set { } }

        public List<EnumItem> Items { get; set; }

        public struct EnumItem
        {
            public string Name { get; set; }
            public string OptionalValue { get; set; }

            public static implicit operator EnumItem(string name)
            {
                return new EnumItem()
                {
                    Name = name
                };
            }

            public static implicit operator EnumItem(KeyValuePair<string, string> pair)
            {
                return new EnumItem()
                {
                    Name = pair.Key,
                    OptionalValue = pair.Value
                };
            }

            public static implicit operator EnumItem(ValueTuple<string, string> pair)
            {
                return new EnumItem()
                {
                    Name = pair.Item1,
                    OptionalValue = pair.Item2
                };
            }
        }

        protected override bool NeedsInstantiation => false;

        public Enum()
        {
            Items = new List<EnumItem>(); 
        }

        protected override IEnumerable<string> GetValues()
        {
            if (Items == null)
            {
                yield break;
            }

            foreach (EnumItem item in Items)
            {
                string itemString = item.Name;
                if (!IsNullOrWhitespace(item.OptionalValue))
                {
                    itemString += " " + JoinWithSpaces("=", item.OptionalValue);
                }

                yield return itemString;
            }
        }
    }

    public class LiteralsCollection : Collection
    {
        public List<string> Values { get; set; }
        protected override bool NeedsInstantiation => true;

        public LiteralsCollection()
        {
            Values = new List<string>();
        }

        protected override IEnumerable<string> GetValues()
        {
            return Values;
        }
    }

    public abstract class Collection : Member
    {
        protected abstract bool NeedsInstantiation { get; }

        public override string Write(byte tabCount)
        {
            string collectionText = base.Write(tabCount);
            if (NeedsInstantiation)
            {
                collectionText += $"new {Type}()";
            }
            collectionText += GetNewLine();
            collectionText += WriteLine(tabCount, "{");
            tabCount++;

            List<string> values = GetValues().ToList();
            foreach (string value in values)
            {
                collectionText += PrependTabs(tabCount, value);
                if (value.IndexOf(value) != values.Count - 1)
                {
                    collectionText += "," + GetNewLine();
                }
            }

            tabCount--;
            collectionText += PrependTabs(tabCount, "}");

            if (NeedsInstantiation)
            {
                collectionText += GetSemiColon();
            }

            return collectionText;
        }

        protected abstract IEnumerable<string> GetValues();

    }

    public abstract class Member : TypedCodePart, IMember
    {

    }


    public class Parameter : TypedCodePart
    {
        public bool Extension { get; set; }

        public override AccessModifiers AccessModifier { get => AccessModifiers.None; set { } }
        public override ConstStaticReadonly ConstStatic { get => ConstStaticReadonly.None; set { } }

        public override string ConstStaticStringToUse => string.Empty;
        public override string AccessModifierStringToUse => Extension ? "this " : string.Empty;

        public override string Write(byte tabCount)
        {
            return base.Write(0);
        }
    }

    public class Method : Member, IMethod
    {
        public CodeType ReturnType { get => Type; set => Type = value; }
        public List<Parameter> Parameters { get; set; }
        public string MethodBody { get; set; }

        public Method()
        {
            Parameters= new List<Parameter>();
        }

        public override string Write(byte tabCount)
        {
            string methodText = base.Write(tabCount);

            string parameterText = Parameters != null && Parameters.Any()
                ? string.Join(", ", Parameters.Select(p => p.Write(0)))
                : string.Empty;
            methodText += $"({parameterText})" + GetNewLine();
            methodText += WriteLine(tabCount, "{");
            tabCount++;
            methodText += WriteLine(tabCount, MethodBody);
            tabCount--;
            methodText += WriteLine(tabCount, "}");
            return methodText;
        }
    }


    public class Field : Member, IField
    {
        public string Value { get; set; }

        public override string Write(byte tabCount)
        {
            string member = base.Write(tabCount);

            return JoinWithSpaces(member, GetFieldEnd());
        }

        protected virtual string GetFieldEnd()
        {
            string value = string.Empty;
            if (!IsNullOrWhitespace(Value))
            {
                value = JoinWithSpaces(value, "=", Value);
            }
            value += GetSemiColon();
            return value;
        }
    }

    public class Property : Field
    {
        public bool HasGetter { get; set; }
        public bool HasSetter { get; set; }

        public virtual string GetterText { get; set; }
        public virtual string SetterText { get; set; }

        protected override string GetFieldEnd()
        {
            if (!HasGetter && !HasSetter)
            {
                throw new Exception("Need a getter or setter!");
            }

            string getterText = HasGetter
                ? $"get{(IsNullOrWhitespace(GetterText) ? ";" : $" {GetterText}")}"
                : string.Empty;

            string setterText = HasSetter
               ? $"set{(IsNullOrWhitespace(SetterText) ? ";" : $" {SetterText}")}"
               : string.Empty;


            string propertyBlock = JoinWithSpaces("{", getterText, setterText, "}", base.GetFieldEnd());

            return propertyBlock;
        }
    }

    public enum AccessModifiers
    {
        None,
        Private,
        Protected,
        Public,
    }

    public enum ConstStaticReadonly
    {
        None,
        Const,
        Static,
        Readonly
    }

    public interface IMember : ITypedCodePart
    {

    }

    public interface IField : IMember
    {
        string Value { get; set; }
    }

    public interface IMethod : IMember
    {
        CodeType ReturnType { get; set; }
        List<Parameter> Parameters { get; set; }
        string MethodBody { get; set; }
    }

    public interface ITypedCodePart : ICodePart
    {
        AccessModifiers AccessModifier { get; set; }
        ConstStaticReadonly ConstStatic { get; set; }
        CodeType Type { get; set; }
    }

    public interface ICodePart
    {
        string Write(byte tabCount);
    }
}

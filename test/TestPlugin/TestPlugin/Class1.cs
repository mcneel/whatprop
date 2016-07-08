//using Rhino.Display;
//using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestPlugin
{
    public class Class1
    {
        public string m_publicField;
        private int m_privateField;
        
        Class1()
        {
            this.m_publicField = "Hello World!";
            this.m_privateField = 1;
//            var mesh = new Point3d(0.0, 0.0, 0.0);
//            Color4f.FromArgb(0.0f, 0.0f, 0.0f, 0.0f);
        }

        public void PublicMethod(int number)
        {
        }

        public string PublicMethodWithReturnType()
        {
            return this.m_publicField;
        }

        [Obsolete("Because, why not?")]
        public string ObsoleteMethod()
        {
            return this.m_publicField;
        }

        public static void GenericMethod<T>(ref T lhs, ref T rhs)
        {
            // swap
            T temp;
            temp = lhs;
            lhs = rhs;
            rhs = temp;
        }

        public void MethodWithGenericArg(EventHandler<Object> handler)
        {
        }

        public IEnumerable<int> MethodWithGenericReturnType()
        {
            yield return 1;
        }

        private int PrivateMethod()
        {
            return this.m_privateField;
        }

        public static void PublicStaticMethod(int number)
        {
        }

        public int PublicProperty { get; set; }

        public enum ExampleEnum
        {
            FIRST,
            SECOND
        }

        public class NestedClass
        {
            public NestedClass()
            {
            }

            public class DoubleNestedClass
            {
                public enum NestedEnum
                {
                    ONE,
                    TWO
                }
            }
        }
    }

    public interface Interface1
    {
        void DoSomething(int[] numbers);
    }

    public abstract class Abstract1 : Interface1
    {
        public void DoSomething(int[] numbers)
        {
        }
    }

    public enum AnotherExampleEnum
    {
        ERROR,
        WARNING,
        INFO,
        DEBUG
    }
}

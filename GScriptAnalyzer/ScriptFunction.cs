using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace GScript.Analyzer
{
    [Obsolete("This planed **Feature** is obsoleting.", true)]
    public class ScriptFunction
    {
        public static void Test()
        {
            var v = Expression.Variable(typeof(int), "i");

            Expression<Action> expression = Expression.Lambda<Action>(Expression.Block(
                Expression.IfThen(
                    Expression.GreaterThan(
                        v, Expression.Constant(-1)
                    ),
                    Expression.Block(
                        Expression.Call(
                            typeof(Console).GetMethod("WriteLine", new System.Type[] { typeof(string) }),
                            Expression.Constant("True.")
                        ),
                        Expression.Assign(v, Expression.Constant(1))
                    )
                ),
                Expression.Call(
                    typeof(Console).GetMethod("WriteLine",new System.Type[] {typeof(int)}),
                    v
                )
            ));
            var func = expression.Compile();
            func();
        }
    }
}

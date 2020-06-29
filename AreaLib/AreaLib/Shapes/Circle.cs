using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AreaLib
{
    public class Circle : Shape
    {
        public Circle()
        {
            List<ParameterTypes> params_ = new List<ParameterTypes>()
            {
                ParameterTypes.RADIUS
            };

            Formulas.Add(new Formula
                (
                    (parameters) =>
                    {
                        return Circle.PI * Math.Pow(parameters[0].Value,2);
                    }
                    , params_
                ));
        }

        public static readonly double PI = 3.14;
    }
}

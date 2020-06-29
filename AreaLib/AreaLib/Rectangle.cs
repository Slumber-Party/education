using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AreaLib
{
    public class Rectangle : Shape
    {
        public Rectangle()
        {
            List<ParameterTypes> params_ = new List<ParameterTypes>
            {
                ParameterTypes.SIDE_A,
                ParameterTypes.SIDE_B
            };

            Formulas.Add(new Formula
                (
                   (parameters) =>
                   {
                       double a = 0, b = 0;

                       foreach (FormulaParameter fp in parameters)
                       {
                           switch (fp.Type)
                           {
                               case ParameterTypes.SIDE_A:
                                   a = fp.Value;
                                   break;
                               case ParameterTypes.SIDE_B:
                                   b = fp.Value;
                                   break;
                           }
                       }

                       return a * b;
                   }
                   ,params_
                ));
        }
    }
}

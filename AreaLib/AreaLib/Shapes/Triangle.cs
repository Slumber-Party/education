using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AreaLib
{
    public class Triangle : Shape
    {
        public Triangle()
        {
            //создаем список объектов формул. Сначала задаем необходимые параметры, 
            //а потом - саму функцию, которая представляет собой формулу

            List<ParameterTypes> params_ = new List<ParameterTypes>()
            {
                ParameterTypes.SIDE_A,
                ParameterTypes.SIDE_B,
                ParameterTypes.SIDE_C
            };

            Formulas.Add(new Formula
                (   (parameters) => 
                    {
                        double a=0, b=0, c=0;

                        foreach(FormulaParameter fp in parameters)
                        {
                            switch (fp.Type)
                            {
                                case ParameterTypes.SIDE_A:
                                    a = fp.Value;
                                    break;
                                case ParameterTypes.SIDE_B:
                                    b = fp.Value;
                                    break;
                                case ParameterTypes.SIDE_C:
                                    c = fp.Value;
                                    break;
                            }
                        }

                        if (CheckIsRight(a, b, c) || CheckIsRight(a, c, b) || CheckIsRight(b, c, a))
                            IsRight = true;

                        double p = (a + b + c) / 2;

                        return Math.Sqrt(p * (p - a) * (p - b) * (p - c));
                    }
                    ,params_
                ));
        }

        public static bool CheckIsRight(double a, double b, double c)
        {
            return Math.Pow(a, 2) + Math.Pow(b, 2) == Math.Pow(c, 2);
        }

        public bool IsRight { get; protected set; }
    }
}

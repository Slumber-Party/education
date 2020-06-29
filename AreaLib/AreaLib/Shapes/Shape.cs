using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AreaLib
{
    public abstract class Shape
    {
        public Shape()
        {
            Formulas = new List<Formula>();
        }

        public double GetArea(List<FormulaParameter> parameters)
        {
            foreach (Formula formula in Formulas)
            {
                if (formula.CheckParameters(parameters))
                {
                    return formula.Calculate(parameters);
                }
            }

            throw new InvalidOperationException("Не удается подобрать формулу для переданных параметров");
        }

        public List<Formula> Formulas { get; protected set; }
    }
}

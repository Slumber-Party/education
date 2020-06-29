using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AreaLib
{
    public struct FormulaParameter
    {
        public FormulaParameter(double value, ParameterTypes type)
        {
            Value = value;
            Type = type;
        }

        public double Value { get; private set; }
        public ParameterTypes Type { get; private set; }
    }

    public class Formula
    {
        public delegate double CalculateDelegate(List<FormulaParameter> list);

        public Formula(CalculateDelegate calc_func, List<ParameterTypes> parameters)
        {
            Calculate = calc_func;
            Params_ = parameters;
        }

        public bool CheckParameters(List<FormulaParameter> params_) //проверка на то, были ли переданы все необходимые параметры в формулу
        {
            if (params_.Count != Params_.Count)
                return false;

            foreach(FormulaParameter p in params_)
            {
                bool isFound = false;
                foreach(ParameterTypes p_inner in Params_)
                {
                    if (p.Type == p_inner)
                        isFound = true;
                }

                if (!isFound)
                    return false;
            }

            return true;
        }

        public CalculateDelegate Calculate { get; private set; } //здесь будет храниться сама функция, выполняющая роль формулы
        public List<ParameterTypes> Params_ { get; private set; } //список всех типов параметров, необходимых для подстановки в формулу
    }
}

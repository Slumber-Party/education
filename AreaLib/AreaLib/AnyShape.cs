using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AreaLib
{
    public class AnyShape : Shape
    {
        public AnyShape()
        {
            List<Shape> shapes = new List<Shape>() { new Triangle(), new Circle() };

            foreach (Shape shape in shapes)
            {
                Formulas.AddRange(shape.Formulas);
            }
        }

        public AnyShape(List<Shape> additionalShapes)
        {
            List<Shape> shapes = new List<Shape>() { new Triangle(), new Circle() };
            shapes.AddRange(additionalShapes);

            foreach (Shape shape in shapes)
            {
                Formulas.AddRange(shape.Formulas);
            }
        }
    }
}

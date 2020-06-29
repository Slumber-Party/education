using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System;
using AreaLib;

namespace AreaLibTest
{
    [TestClass]
    public class AreaLibTests
    {
        [TestMethod]
        public void Triangle_AreaCalculate_WithIncorrectArgs()
        {
            Triangle tr = new Triangle();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(10,ParameterTypes.RADIUS)
                ,new FormulaParameter(12,ParameterTypes.SIDE_A)
            };
            
            Assert.ThrowsException<InvalidOperationException>(() => tr.GetArea(params_));
        }

        [TestMethod]
        public void TriangeAreaCalculate_WithCorrectArgs()
        {
            Triangle tr = new Triangle();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(3,ParameterTypes.SIDE_A)
                ,new FormulaParameter(4,ParameterTypes.SIDE_B)
                ,new FormulaParameter(6,ParameterTypes.SIDE_C)
            };

            Assert.AreEqual(Math.Round(tr.GetArea(params_),2),5.33);
        }

        [TestMethod]
        public void CircleAreaCalculate_WithCorrectArgs()
        {
            Circle c = new Circle();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(5,ParameterTypes.RADIUS)
            };

            Assert.AreEqual(c.GetArea(params_), 78.5);
        }

        [TestMethod]
        public void CircleAreaCalculate_WithMoreArgs()
        {
            Circle c = new Circle();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(5,ParameterTypes.RADIUS)
                ,new FormulaParameter(10,ParameterTypes.SIDE_A)
            };

            Assert.ThrowsException<InvalidOperationException>(() => c.GetArea(params_));
        }

        [TestMethod]
        public void UnknownShapeCalculate_WithCorrectArgs_circle()
        {
            AnyShape sh = new AnyShape();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(5,ParameterTypes.RADIUS)
            };

            Assert.AreEqual(sh.GetArea(params_), 78.5);
        }

        [TestMethod]
        public void UnknownShapeCalculate_WithCorrectArgs_triangle()
        {
            AnyShape sh = new AnyShape();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(3,ParameterTypes.SIDE_A)
                ,new FormulaParameter(4,ParameterTypes.SIDE_B)
                ,new FormulaParameter(6,ParameterTypes.SIDE_C)
            };

            Assert.AreEqual(Math.Round(sh.GetArea(params_), 2), 5.33);
        }

        [TestMethod]
        public void UnknownShapeCalculate_WithIncorrectArgs()
        {
            AnyShape sh = new AnyShape();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(3,ParameterTypes.SIDE_A)
                ,new FormulaParameter(4,ParameterTypes.RADIUS)
            };

            Assert.ThrowsException<InvalidOperationException>(() => sh.GetArea(params_));
        }

        [TestMethod]
        public void TriangleIsRightCheck()
        {
            Triangle tr = new Triangle();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(3,ParameterTypes.SIDE_A)
                ,new FormulaParameter(4,ParameterTypes.SIDE_B)
                ,new FormulaParameter(5,ParameterTypes.SIDE_C)
            };

            Assert.AreEqual(tr.GetArea(params_), 6);
            Assert.IsTrue(tr.IsRight);
        }

        [TestMethod]
        public void RectangleCalculate_WithCorrectArgs()
        {
            Rectangle rect = new Rectangle();

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(3,ParameterTypes.SIDE_A)
                ,new FormulaParameter(4,ParameterTypes.SIDE_B)
            };

            Assert.AreEqual(rect.GetArea(params_), 12);
        }

        [TestMethod]
        public void UnknownShapeCalculate_WithCorrectArgs_rectangle()
        {
            AnyShape sh = new AnyShape(new List<Shape>() { new Rectangle() });

            List<FormulaParameter> params_ = new List<FormulaParameter>()
            {
                new FormulaParameter(3,ParameterTypes.SIDE_A)
                ,new FormulaParameter(4,ParameterTypes.SIDE_B)
            };

            double s = sh.GetArea(params_);

            Assert.AreEqual(sh.GetArea(params_), 12);
        }
    }
}

﻿/*
    Copyright 2016 Travel Modelling Group, Department of Civil Engineering, University of Toronto

    This file is part of XTMF.

    XTMF is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    XTMF is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with XTMF.  If not, see <http://www.gnu.org/licenses/>.
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
using TMG;
using TMG.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datastructure;
using TMG.Frameworks.Data.Processing.AST;

namespace XTMF.Testing.TMG.Data
{
    [TestClass]
    public class TestCompiler
    {
        /// <summary>
        /// Create a new simple matrix for testing.
        /// </summary>
        /// <param name="m11"></param>
        /// <param name="m12"></param>
        /// <param name="m21"></param>
        /// <param name="m22"></param>
        /// <returns></returns>
        private IDataSource<SparseTwinIndex<float>> CreateData(string name, float m11, float m12, float m21, float m22)
        {
            SparseIndexing indexes = new SparseIndexing();
            indexes.Indexes = new SparseSet[]
            {
                new SparseSet()
                {
                    Start = 1,
                    Stop = 2,
                    SubIndex = new SparseIndexing()
                    {
                        Indexes = new SparseSet[] { new SparseSet() { Start = 1, Stop = 2  } }
                    }
                }
            };

            float[][] data = new float[][] { new float[] { m11, m12 }, new float[] { m21, m22 } };
            return new MatrixSource(new SparseTwinIndex<float>(indexes, data)) { Name = name };
        }

        [TestMethod]
        public void TestMatrixAdd()
        {
            var data = new IDataSource[] 
            {
                CreateData("A", 1, 2, 3, 4),
                CreateData("B", 2, 4, 6, 8)
            };
            string error = null;
            Expression ex;
            Assert.IsTrue(Compiler.Compile("A + B", out ex, ref error), "Unable to compile 'A + B'");
            var result = ex.Evaluate(data);
            Assert.IsTrue(result.IsODResult);
            var flat = result.ODData.GetFlatData();
            Assert.AreEqual(3.0f, flat[0][0], 0.00001f);
            Assert.AreEqual(6.0f, flat[0][1], 0.00001f);
            Assert.AreEqual(9.0f, flat[1][0], 0.00001f);
            Assert.AreEqual(12.0f, flat[1][1], 0.00001f);
        }

        [TestMethod]
        public void TestMatrixAddWithBrackets()
        {
            var data = new IDataSource[]
            {
                CreateData("A", 1, 2, 3, 4),
                CreateData("B", 2, 4, 6, 8)
            };
            string error = null;
            Expression ex;
            Assert.IsTrue(Compiler.Compile("(A) + (B)", out ex, ref error), "Unable to compile '(A) + (B)'");
            var result = ex.Evaluate(data);
            Assert.IsTrue(result.IsODResult);
            var flat = result.ODData.GetFlatData();
            Assert.AreEqual(3.0f, flat[0][0], 0.00001f);
            Assert.AreEqual(6.0f, flat[0][1], 0.00001f);
            Assert.AreEqual(9.0f, flat[1][0], 0.00001f);
            Assert.AreEqual(12.0f, flat[1][1], 0.00001f);
        }

        [TestMethod]
        public void TestMatrixAddWithDoubleBrackets()
        {
            var data = new IDataSource[]
            {
                CreateData("A", 1, 2, 3, 4),
                CreateData("B", 2, 4, 6, 8)
            };
            string error = null;
            Expression ex;
            Assert.IsTrue(Compiler.Compile("((A)) + ((B))", out ex, ref error));
            var result = ex.Evaluate(data);
            Assert.IsTrue(result.IsODResult);
            var flat = result.ODData.GetFlatData();
            Assert.AreEqual(3.0f, flat[0][0], 0.00001f);
            Assert.AreEqual(6.0f, flat[0][1], 0.00001f);
            Assert.AreEqual(9.0f, flat[1][0], 0.00001f);
            Assert.AreEqual(12.0f, flat[1][1], 0.00001f);
        }

        [TestMethod]
        public void TestMatrixAddWithTripleBrackets()
        {
            var data = new IDataSource[]
            {
                CreateData("A", 1, 2, 3, 4),
                CreateData("B", 2, 4, 6, 8)
            };
            string error = null;
            Expression ex;
            Assert.IsTrue(Compiler.Compile("(((A)) + ((B)))", out ex, ref error));
            var result = ex.Evaluate(data);
            Assert.IsTrue(result.IsODResult);
            var flat = result.ODData.GetFlatData();
            Assert.AreEqual(3.0f, flat[0][0], 0.00001f);
            Assert.AreEqual(6.0f, flat[0][1], 0.00001f);
            Assert.AreEqual(9.0f, flat[1][0], 0.00001f);
            Assert.AreEqual(12.0f, flat[1][1], 0.00001f);
        }

        [TestMethod]
        public void TestMatrixSumRows()
        {
            var data = new IDataSource[]
            {
                CreateData("A", 1, 2, 3, 4),
                CreateData("B", 2, 4, 6, 8)
            };
            string error = null;
            Expression ex;
            Assert.IsTrue(Compiler.Compile("SumRows(A + B)", out ex, ref error));
            var result = ex.Evaluate(data);
            Assert.IsTrue(result.IsVectorResult);
            Assert.IsTrue(result.Direction == ComputationResult.VectorDirection.Vertical);
            var flat = result.VectorData.GetFlatData();
            Assert.AreEqual(9.0f, flat[0], 0.00001f);
            Assert.AreEqual(21.0f, flat[1], 0.00001f);
        }

        [TestMethod]
        public void TestMatrixSumColumns()
        {
            var data = new IDataSource[]
            {
                CreateData("A", 1, 2, 3, 4),
                CreateData("B", 2, 4, 6, 8)
            };
            string error = null;
            Expression ex;
            Assert.IsTrue(Compiler.Compile("SumColumns(A + B)", out ex, ref error));
            var result = ex.Evaluate(data);
            Assert.IsTrue(result.IsVectorResult);
            Assert.IsTrue(result.Direction == ComputationResult.VectorDirection.Horizontal);
            var flat = result.VectorData.GetFlatData();
            Assert.AreEqual(12.0f, flat[0], 0.00001f);
            Assert.AreEqual(18.0f, flat[1], 0.00001f);
        }

        [TestMethod]
        public void TestMatrixAsHorizontal()
        {
            var data = new IDataSource[]
            {
                CreateData("A", 1, 2, 3, 4),
                CreateData("B", 2, 4, 6, 8)
            };
            string error = null;
            Expression ex;
            Assert.IsTrue(Compiler.Compile("AsHorizontal(SumRows(A + B))", out ex, ref error));
            var result = ex.Evaluate(data);
            Assert.IsTrue(result.IsVectorResult);
            Assert.IsTrue(result.Direction == ComputationResult.VectorDirection.Horizontal);
            var flat = result.VectorData.GetFlatData();
            Assert.AreEqual(9.0f, flat[0], 0.00001f);
            Assert.AreEqual(21.0f, flat[1], 0.00001f);
        }

        class MatrixSource : IDataSource<SparseTwinIndex<float>>
        {
            public bool Loaded { get; set; }


            public string Name { get; set; }


            public float Progress { get; set; }

            SparseTwinIndex<float> Data;

            public MatrixSource(SparseTwinIndex<float> matrix)
            {
                Data = matrix;
                Loaded = true;
            }


            public Tuple<byte, byte, byte> ProgressColour
            {
                get
                {
                    return null;
                }
            }

            public SparseTwinIndex<float> GiveData()
            {
                return Data;
            }

            public void LoadData()
            {

            }

            public bool RuntimeValidation(ref string error)
            {
                return true;
            }

            public void UnloadData()
            {

            }
        }
    }
}

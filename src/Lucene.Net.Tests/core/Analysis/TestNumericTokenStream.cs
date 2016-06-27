using Lucene.Net.Analysis.Tokenattributes;
using System;
using Xunit;

namespace Lucene.Net.Analysis
{
    /*
         * Licensed to the Apache Software Foundation (ASF) under one or more
         * contributor license agreements.  See the NOTICE file distributed with
         * this work for additional information regarding copyright ownership.
         * The ASF licenses this file to You under the Apache License, Version 2.0
         * (the "License"); you may not use this file except in compliance with
         * the License.  You may obtain a copy of the License at
         *
         *     http://www.apache.org/licenses/LICENSE-2.0
         *
         * Unless required by applicable law or agreed to in writing, software
         * distributed under the License is distributed on an "AS IS" BASIS,
         * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
         * See the License for the specific language governing permissions and
         * limitations under the License.
         */

    using BytesRef = Lucene.Net.Util.BytesRef;
    using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
    using ICharTermAttribute = Lucene.Net.Analysis.Tokenattributes.ICharTermAttribute;
    using NumericUtils = Lucene.Net.Util.NumericUtils;

    public class TestNumericTokenStream : BaseTokenStreamTestCase
    {
        internal const long Lvalue = 4573245871874382L;
        internal const int Ivalue = 123456;

        [Fact]
        public virtual void TestLongStream()
        {
            using (NumericTokenStream stream = (new NumericTokenStream()).SetLongValue(Lvalue)) {
                // use getAttribute to test if attributes really exist, if not an IAE will be throwed
                ITermToBytesRefAttribute bytesAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
                ITypeAttribute typeAtt = stream.GetAttribute<ITypeAttribute>();
                NumericTokenStream.INumericTermAttribute numericAtt = stream.GetAttribute<NumericTokenStream.INumericTermAttribute>();
                BytesRef bytes = bytesAtt.BytesRef;
                stream.Reset();
                Assert.Equal(64, numericAtt.ValueSize);
                for (int shift = 0; shift < 64; shift += NumericUtils.PRECISION_STEP_DEFAULT)
                {
                    Assert.True(stream.IncrementToken(), "New token is available");
                    Assert.Equal(shift, numericAtt.Shift); //, "Shift value wrong");
                    bytesAtt.FillBytesRef();
                    Assert.Equal(Lvalue & ~((1L << shift) - 1L), NumericUtils.PrefixCodedToLong(bytes)); //, "Term is incorrectly encoded");
                    Assert.Equal(Lvalue & ~((1L << shift) - 1L), numericAtt.RawValue); //, "Term raw value is incorrectly encoded");
                    Assert.Equal((shift == 0) ? NumericTokenStream.TOKEN_TYPE_FULL_PREC : NumericTokenStream.TOKEN_TYPE_LOWER_PREC, typeAtt.Type); //, "Type incorrect");
                }
                Assert.False(stream.IncrementToken(), "More tokens available");
                stream.End();
            }
        }

        [Fact]
        public virtual void TestIntStream()
        {
            NumericTokenStream stream = (new NumericTokenStream()).SetIntValue(Ivalue);
            // use getAttribute to test if attributes really exist, if not an IAE will be throwed
            ITermToBytesRefAttribute bytesAtt = stream.GetAttribute<ITermToBytesRefAttribute>();
            ITypeAttribute typeAtt = stream.GetAttribute<ITypeAttribute>();
            NumericTokenStream.INumericTermAttribute numericAtt = stream.GetAttribute<NumericTokenStream.INumericTermAttribute>();
            BytesRef bytes = bytesAtt.BytesRef;
            stream.Reset();
            Assert.Equal(32, numericAtt.ValueSize);
            for (int shift = 0; shift < 32; shift += NumericUtils.PRECISION_STEP_DEFAULT)
            {
                Assert.True(stream.IncrementToken(), "New token is available");
                Assert.Equal(shift, numericAtt.Shift); //, "Shift value wrong");
                bytesAtt.FillBytesRef();
                Assert.Equal(Ivalue & ~((1 << shift) - 1), NumericUtils.PrefixCodedToInt(bytes)); //, "Term is incorrectly encoded");
                Assert.Equal(((long)Ivalue) & ~((1L << shift) - 1L), numericAtt.RawValue); //, "Term raw value is incorrectly encoded");
                Assert.Equal((shift == 0) ? NumericTokenStream.TOKEN_TYPE_FULL_PREC : NumericTokenStream.TOKEN_TYPE_LOWER_PREC, typeAtt.Type); //, "Type incorrect");
            }
            Assert.False(stream.IncrementToken(), "More tokens available");
            stream.End();
            stream.Dispose();
        }

        [Fact]
        public virtual void TestNotInitialized()
        {
            NumericTokenStream stream = new NumericTokenStream();

            try
            {
                stream.Reset();
                Assert.True(false, "reset() should not succeed.");
            }
            catch (Exception)
            {
                // pass
            }

            try
            {
                stream.IncrementToken();
                Assert.True(false, "IncrementToken() should not succeed.");
            }
            catch (Exception)
            {
                // pass
            }
        }

        public interface ITestAttribute : ICharTermAttribute
        {
        }

        public class TestAttribute : CharTermAttribute, ITestAttribute
        {
        }

        [Fact]
        public virtual void TestCTA()
        {
            NumericTokenStream stream = new NumericTokenStream();
            try
            {
                stream.AddAttribute<ICharTermAttribute>();
                Assert.True(false, "Succeeded to add CharTermAttribute.");
            }
            catch (System.ArgumentException iae)
            {
                Assert.True(iae.Message.StartsWith("NumericTokenStream does not support"));
            }
            try
            {
                stream.AddAttribute<ITestAttribute>();
                Assert.True(false, "Succeeded to add TestAttribute.");
            }
            catch (System.ArgumentException iae)
            {
                Assert.True(iae.Message.StartsWith("NumericTokenStream does not support"));
            }
        }
    }
}
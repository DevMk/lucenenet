using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Lucene.Net.Util
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

    public class TestIOUtils : LuceneTestCase
    {
        internal sealed class BrokenIDisposable : IDisposable
        {
            internal readonly int i;

            public BrokenIDisposable(int i)
            {
                this.i = i;
            }

            public void Dispose()
            {
                throw new IOException("TEST-IO-EXCEPTION-" + i);
            }
        }

        internal sealed class TestException : Exception
        {
            public TestException()
                : base("BASE-EXCEPTION")
            {
            }
        }

        [Fact]
        public virtual void TestSuppressedExceptions()
        {
            // test with prior exception
            try
            {
                TestException t = new TestException();
                IOUtils.CloseWhileHandlingException(t, new BrokenIDisposable(1), new BrokenIDisposable(2));
            }
            catch (TestException e1)
            {
                Assert.True(e1.Data.Contains("SuppressedExceptions"));
                Assert.True(((List<Exception>) e1.Data["SuppressedExceptions"]).Count == 2);
            }
            catch (Exception e2)
            {
                Assert.True(false, "Exception should not be thrown here");
            }

            // test without prior exception
            try
            {
                IOUtils.CloseWhileHandlingException((TestException)null, new BrokenIDisposable(1), new BrokenIDisposable(2));
            }
            catch (IOException e1)
            {
                Assert.True(e1.Data.Contains("SuppressedExceptions"));
                Assert.True(((List<Exception>)e1.Data["SuppressedExceptions"]).Count == 1);
            }
            catch (Exception e2)
            {
                Assert.True(false, "Exception should not be thrown here");
            }
        }
    }
}
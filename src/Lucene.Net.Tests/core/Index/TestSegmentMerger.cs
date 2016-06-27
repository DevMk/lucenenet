using Lucene.Net.Support;
using Xunit;

namespace Lucene.Net.Index
{
    using BytesRef = Lucene.Net.Util.BytesRef;

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

    using Codec = Lucene.Net.Codecs.Codec;
    using Constants = Lucene.Net.Util.Constants;
    using Directory = Lucene.Net.Store.Directory;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using Document = Documents.Document;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using TestUtil = Lucene.Net.Util.TestUtil;

    public class TestSegmentMerger : LuceneTestCase
    {
        //The variables for the new merged segment
        private Directory MergedDir;

        private string MergedSegment = "test";

        //First segment to be merged
        private Directory Merge1Dir;

        private Document Doc1;
        private SegmentReader Reader1;

        //Second Segment to be merged
        private Directory Merge2Dir;

        private Document Doc2;
        private SegmentReader Reader2;

        public TestSegmentMerger() : base()
        {
            this.Doc1 = new Document();
            this.Doc2 = new Document();
            MergedDir = NewDirectory();
            Merge1Dir = NewDirectory();
            Merge2Dir = NewDirectory();
            DocHelper.SetupDoc(Doc1);
            SegmentCommitInfo info1 = DocHelper.WriteDoc(Random(), Merge1Dir, Doc1);
            DocHelper.SetupDoc(Doc2);
            SegmentCommitInfo info2 = DocHelper.WriteDoc(Random(), Merge2Dir, Doc2);
            Reader1 = new SegmentReader(info1, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));
            Reader2 = new SegmentReader(info2, DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));
        }

        public override void Dispose()
        {
            Reader1.Dispose();
            Reader2.Dispose();
            MergedDir.Dispose();
            Merge1Dir.Dispose();
            Merge2Dir.Dispose();
            base.Dispose();
        }

        [Fact]
        public virtual void Test()
        {
            Assert.True(MergedDir != null);
            Assert.True(Merge1Dir != null);
            Assert.True(Merge2Dir != null);
            Assert.True(Reader1 != null);
            Assert.True(Reader2 != null);
        }

        [Fact]
        public virtual void TestMerge()
        {
            Codec codec = Codec.Default;
            SegmentInfo si = new SegmentInfo(MergedDir, Constants.LUCENE_MAIN_VERSION, MergedSegment, -1, false, codec, null);

            SegmentMerger merger = new SegmentMerger(Arrays.AsList<AtomicReader>(Reader1, Reader2), si, InfoStream.Default, MergedDir, IndexWriterConfig.DEFAULT_TERM_INDEX_INTERVAL, MergeState.CheckAbort.NONE, new FieldInfos.FieldNumbers(), NewIOContext(Random()), true);
            MergeState mergeState = merger.Merge();
            int docsMerged = mergeState.SegmentInfo.DocCount;
            Assert.True(docsMerged == 2);
            //Should be able to open a new SegmentReader against the new directory
            SegmentReader mergedReader = new SegmentReader(new SegmentCommitInfo(new SegmentInfo(MergedDir, Constants.LUCENE_MAIN_VERSION, MergedSegment, docsMerged, false, codec, null), 0, -1L, -1L), DirectoryReader.DEFAULT_TERMS_INDEX_DIVISOR, NewIOContext(Random()));
            Assert.True(mergedReader != null);
            Assert.True(mergedReader.NumDocs == 2);
            Document newDoc1 = mergedReader.Document(0);
            Assert.True(newDoc1 != null);
            //There are 2 unstored fields on the document
            Assert.True(DocHelper.NumFields(newDoc1) == DocHelper.NumFields(Doc1) - DocHelper.Unstored.Count);
            Document newDoc2 = mergedReader.Document(1);
            Assert.True(newDoc2 != null);
            Assert.True(DocHelper.NumFields(newDoc2) == DocHelper.NumFields(Doc2) - DocHelper.Unstored.Count);

            DocsEnum termDocs = TestUtil.Docs(Random(), mergedReader, DocHelper.TEXT_FIELD_2_KEY, new BytesRef("field"), MultiFields.GetLiveDocs(mergedReader), null, 0);
            Assert.True(termDocs != null);
            Assert.True(termDocs.NextDoc() != DocIdSetIterator.NO_MORE_DOCS);

            int tvCount = 0;
            foreach (FieldInfo fieldInfo in mergedReader.FieldInfos)
            {
                if (fieldInfo.HasVectors())
                {
                    tvCount++;
                }
            }

            //System.out.println("stored size: " + stored.Size());
            Assert.Equal(3, tvCount); //, "We do not have 3 fields that were indexed with term vector");

            Terms vector = mergedReader.GetTermVectors(0).Terms(DocHelper.TEXT_FIELD_2_KEY);
            Assert.NotNull(vector);
            Assert.Equal(3, vector.Size());
            TermsEnum termsEnum = vector.Iterator(null);

            int i = 0;
            while (termsEnum.Next() != null)
            {
                string term = termsEnum.Term().Utf8ToString();
                int freq = (int)termsEnum.TotalTermFreq();
                //System.out.println("Term: " + term + " Freq: " + freq);
                Assert.True(DocHelper.FIELD_2_TEXT.IndexOf(term) != -1);
                Assert.True(DocHelper.FIELD_2_FREQS[i] == freq);
                i++;
            }

            TestSegmentReader.CheckNorms(mergedReader);
            mergedReader.Dispose();
        }

        private static bool Equals(MergeState.DocMap map1, MergeState.DocMap map2)
        {
            if (map1.MaxDoc != map2.MaxDoc)
            {
                return false;
            }
            for (int i = 0; i < map1.MaxDoc; ++i)
            {
                if (map1.Get(i) != map2.Get(i))
                {
                    return false;
                }
            }
            return true;
        }

        [Fact]
        public virtual void TestBuildDocMap()
        {
            int maxDoc = TestUtil.NextInt(Random(), 1, 128);
            int numDocs = TestUtil.NextInt(Random(), 0, maxDoc);
            int numDeletedDocs = maxDoc - numDocs;
            FixedBitSet liveDocs = new FixedBitSet(maxDoc);
            for (int i = 0; i < numDocs; ++i)
            {
                while (true)
                {
                    int docID = Random().Next(maxDoc);
                    if (!liveDocs.Get(docID))
                    {
                        liveDocs.Set(docID);
                        break;
                    }
                }
            }

            MergeState.DocMap docMap = MergeState.DocMap.Build(maxDoc, liveDocs);

            Assert.Equal(maxDoc, docMap.MaxDoc);
            Assert.Equal(numDocs, docMap.NumDocs);
            Assert.Equal(numDeletedDocs, docMap.NumDeletedDocs);
            // assert the mapping is compact
            for (int i = 0, del = 0; i < maxDoc; ++i)
            {
                if (!liveDocs.Get(i))
                {
                    Assert.Equal(-1, docMap.Get(i));
                    ++del;
                }
                else
                {
                    Assert.Equal(i - del, docMap.Get(i));
                }
            }
        }
    }
}
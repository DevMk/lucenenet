﻿using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Util;

namespace Lucene.Net.Search.Spans
{
    /// <summary>
    /// Base class for filtering a SpanQuery based on the position of a match.
    /// </summary>
    public abstract class SpanPositionCheckQuery : SpanQuery, ICloneable
    {
        public virtual SpanQuery Match { get; protected set; }

        protected SpanPositionCheckQuery(SpanQuery match)
        {
            Match = match;
        }

        public override string Field { get { return Match.Field; } }

        public override void ExtractTerms(ISet<Term> terms)
        {
            Match.ExtractTerms(terms);
        }
        
        protected enum AcceptStatus
        {
            /// <summary>
            /// Indicates the match should be accepted.
            /// </summary>
            YES,

            /// <summary>
            /// Indicates the match should be rejected.
            /// </summary>
            NO,

            /// <summary>
            /// Indicates the match should be rejected, and the enumeration should advance
            /// to the next document.
            /// </summary>
            NO_AND_ADVANCE
        }

        protected abstract AcceptStatus AcceptPosition(Spans spans);

        public override Spans GetSpans(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts)
        {
            return new PositionCheckSpan(context, acceptDocs, termContexts);
        }

        public override Query Rewrite(IndexReader reader)
        {
            SpanPositionCheckQuery clone = null;

            var rewritten = (SpanQuery) Match.Rewrite(reader);
            if (rewritten != Match)
            {
                clone = (SpanPositionCheckQuery) this.Clone();
                clone.Match = rewritten;
            }

            if (clone != null)
            {
                return clone;
            }
            else
            {
                return this;
            }
        }

        protected class PositionCheckSpan : Spans
        {
            private Spans spans;

            private SpanPositionCheckQuery parent;

            public PositionCheckSpan(AtomicReaderContext context, Bits acceptDocs, IDictionary<Term, TermContext> termContexts, SpanPositionCheckQuery parent)
            {
                this.parent = parent;
                spans = parent.Match.GetSpans(context, acceptDocs, termContexts);
            }

            public override bool Next()
            {
                return spans.Next() && DoNext();
            }

            public override bool SkipTo(int target)
            {
                return spans.SkipTo(target) && DoNext();
            }

            public bool DoNext()
            {
                for (;;)
                {
                    switch (parent.AcceptPosition(this))
                    {
                        case AcceptStatus.YES:
                            return true;
                        case AcceptStatus.NO:
                            if (!spans.Next())
                                return false;
                            break;
                        case AcceptStatus.NO_AND_ADVANCE:
                            if (!spans.SkipTo(spans.Doc() + 1))
                                return false;
                            break;
                    }
                }
            }

            public override int Doc()
            {
                return spans.Doc();
            }

            public override int Start()
            {
                return spans.Start();
            }

            public override int End()
            {
                return spans.End();
            }

            public override ICollection<sbyte[]> GetPayload()
            {
                List<sbyte[]> result = null;
                if (spans.IsPayloadAvailable())
                {
                    result = new List<sbyte[]>(spans.GetPayload());
                }
                return result;
            }

            public override bool IsPayloadAvailable()
            {
                return spans.IsPayloadAvailable();
            }

            public override long Cost()
            {
                return spans.Cost();
            }

            public override string ToString()
            {
                return "spans(" + parent.ToString() + ")";
            }
        }
    }
}
using Bonsai.Expressions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Bonsai.Lsl
{
    [WorkflowElementCategory(ElementCategory.Source)]
    public class ReceiverTest : SingleArgumentExpressionBuilder
    {
        static readonly Range<int> argumentRange = Range.Create(lowerBound: 0, upperBound: 0);
        public override Range<int> ArgumentRange
        {
            get { return argumentRange; }
        }

        public override Expression Build(IEnumerable<Expression> arguments)
        {
            throw new NotImplementedException();
        }
    }
}

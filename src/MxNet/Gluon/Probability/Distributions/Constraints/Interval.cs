﻿using System;
using System.Collections.Generic;
using System.Text;

namespace MxNet.Gluon.Probability.Distributions.Constraints
{
    public class Interval : Constraint
    {
        public Interval(float lower_bound, float upper_bound)
        {
            throw new NotImplementedException();
        }

        public override bool Check(NDArrayOrSymbol value)
        {
            throw new NotImplementedException();
        }
    }
}

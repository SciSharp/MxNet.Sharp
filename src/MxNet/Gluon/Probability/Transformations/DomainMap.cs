﻿using MxNet.Gluon.Probability.Distributions.Constraints;
using System;
using System.Collections.Generic;
using System.Text;

namespace MxNet.Gluon.Probability.Transformations
{
    public class DomainMap
    {
        public void Register(Constraint constraint, Func<Constraint, Transformation> factory = null)
        {
            throw new NotImplementedException();
        }

        public Transformation Call(Constraint constraint)
        {
            throw new NotImplementedException();
        }
    }
}

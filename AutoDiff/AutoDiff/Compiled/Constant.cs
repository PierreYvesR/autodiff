﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoDiff.Compiled
{
	class Constant : TapeElement
	{
        public Constant(double value)
        {
            Value = value;
            Derivative = 0;
        }
	}
}

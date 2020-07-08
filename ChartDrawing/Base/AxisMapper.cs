﻿using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Graphics.Base
{
    public class AxisMapper
    {
        public double InitialMin => manager.InitialMin;
        public double InitialMax => manager.InitialMax;

        private AxisManager manager;

        public AxisMapper(AxisManager manager_)
        {
            manager = manager_;
        }

        public double ValueToRenderPosition(object value)
        {
            return manager.ValueToRenderPosition(value);
        }
        public double RenderPositionToValue(double value)
        {
            return manager.RenderPositionToValue(value);
        }
    }
}
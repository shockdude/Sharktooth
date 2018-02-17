﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sharktooth
{
    public struct XMKTempo
    {
        public float Start; // In seconds
        public uint MicroPerQuarter; // Micro seconds per quarter note
        public uint Ticks; // ??
        public double BPM => 60000000.0d / MicroPerQuarter;

        public XMKTempo(float start, uint mpq, uint ticks)
        {
            Start = start;
            MicroPerQuarter = mpq;
            Ticks = ticks;
        }
    }
}
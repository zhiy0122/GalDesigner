﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalEngine
{
    public class GuiControlProvider : GameObject
    {
        public GuiControlProvider(string name) : base(name)
        {
            //add requirement component
            AddComponent<VisualGuiComponent>();
            AddComponent<LogicGuiComponent>();
            AddComponent<TransformGuiComponent>();
        }
    }
}
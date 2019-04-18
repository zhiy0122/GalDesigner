﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalEngine
{
    public class GuiControl : GameObject
    {
        public GuiControl()
        {
            //add requirement component
            AddComponent<LogicGuiComponent>();
            AddComponent<VisualGuiComponent>();
            AddComponent<TransformGuiComponent>();
        }

        public GuiControl(string name) : base(name)
        {
            //add requirement component
            AddComponent<LogicGuiComponent>();
            AddComponent<VisualGuiComponent>();
            AddComponent<TransformGuiComponent>();
        }
    }
}
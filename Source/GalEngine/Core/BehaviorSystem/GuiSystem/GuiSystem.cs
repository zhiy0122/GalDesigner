﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GalEngine.Runtime.Graphics;

namespace GalEngine
{
    public class GuiSystem : BehaviorSystem
    {
        private GraphicsRenderTarget mRenderTarget;
        private GraphicsTexture mRenderCanvas;

        private GuiRender mRender;

        public Rectangle<int> Area { get; set; }

        public GuiSystem(GraphicsDevice device, Rectangle<int> area) : base("GuiSystem")
        {
            RequireComponents.AddRequireComponentType<TransformGuiComponent>();
            RequireComponents.AddRequireComponentType<VisualGuiComponent>();
            RequireComponents.AddRequireComponentType<LogicGuiComponent>();

            mRender = new GuiRender(device);

            Area = area;

            mRenderCanvas = new GraphicsTexture(mRender.Device,
                Area.Right - Area.Left, Area.Bottom - Area.Top,
                PixelFormat.R8G8B8A8Unknown, GraphicsResourceBindType.RenderTarget);

            mRenderTarget = new GraphicsRenderTarget(mRender.Device, mRenderCanvas);
        }

        protected internal override void Update()
        {
            //update the render area, we need to update the canvas and render target
            //if the area's size is not equal the canvas's size
            if (mRenderCanvas.Width != Area.Right - Area.Left || 
                mRenderCanvas.Height != Area.Bottom - Area.Top)
            {
                //update canvas
                mRenderCanvas = new GraphicsTexture(mRender.Device,
                    Area.Right - Area.Left, Area.Bottom - Area.Top,
                    PixelFormat.R8G8B8A8Unknown, GraphicsResourceBindType.RenderTarget);

                //update render target
                mRenderTarget = new GraphicsRenderTarget(mRender.Device, mRenderCanvas);
            }
        }

        protected internal override void Excute(List<GameObject> passedGameObjectList)
        {
            mRender.BeginDraw(mRenderTarget);

            foreach (var gameObject in passedGameObjectList)
            {
                var transformComponent = gameObject.GetComponent<TransformGuiComponent>();

                //run rendering code
                switch (gameObject.GetComponent<VisualGuiComponent>())
                {
                    case FrameVisualGuiComponent frameComponent:
                        //draw rectangle
                        mRender.DrawRectangle(
                            new Rectangle<float>(
                                transformComponent.Position.X,
                                transformComponent.Position.Y,
                                transformComponent.Position.X + frameComponent.Size.Width,
                                transformComponent.Position.Y + frameComponent.Size.Height),
                            frameComponent.Color,
                            frameComponent.Padding);
                        break;
                }
            }

            mRender.EndDraw();
        }
    }
}
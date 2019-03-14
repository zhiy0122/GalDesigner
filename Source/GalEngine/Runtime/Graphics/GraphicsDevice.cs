﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GalEngine.Runtime.Graphics
{
    using Debug = System.Diagnostics.Debug;

    public class GraphicsDevice : IDisposable
    {
        private GraphicsVertexShader mVertexShader;
        private GraphicsPixelShader mPixelShader;
        private GraphicsInputLayout mInputLayout;

        private SharpDX.Direct3D11.Device mDevice;
        private SharpDX.Direct3D11.DeviceContext mImmediateContext;
        
        internal SharpDX.Direct3D11.Device Device { get => mDevice; }
        internal SharpDX.Direct3D11.DeviceContext ImmediateContext { get => mImmediateContext; }
        
        public GraphicsAdapter Adapter { get; }

        public GraphicsDevice(GraphicsAdapter adapter)
        {
            //set member to null
            mVertexShader = null;
            mPixelShader = null;
            mInputLayout = null;

            //set the adapter
            Adapter = adapter;

#if DEBUG
            //creation flag, we use debug flag
            var creationFlags = SharpDX.Direct3D11.DeviceCreationFlags.Debug;
#else
            var creationFlags = SharpDX.Direct3D11.DeviceCreationFlags.None;
#endif
            //fetuares level: 11_0, 12_0
            var fetuares = new SharpDX.Direct3D.FeatureLevel[2]
            {
                 SharpDX.Direct3D.FeatureLevel.Level_11_0,
                 SharpDX.Direct3D.FeatureLevel.Level_12_0
            };

            //create device with current adapter
            mDevice = new SharpDX.Direct3D11.Device(Adapter.Adapter, creationFlags, fetuares);
            mImmediateContext = Device.ImmediateContext;

            LogEmitter.Apply(LogLevel.Information, "[Initialize Graphics Device with {0}]", adapter.Description);
            LogEmitter.Apply(LogLevel.Information, "[Graphics Device Feature Level = {0}]", Device.FeatureLevel);
        }

        ~GraphicsDevice() => Dispose();

        public void Reset()
        {
            //clear all state and reset it

            mVertexShader = null;
            mPixelShader = null;
            mInputLayout = null;

            ImmediateContext.ClearState();
        }

        public void ClearRenderTarget(GraphicsRenderTarget renderTarget, Vector4<float> color)
        {
            //clear render target using color
            //x = red, y = green, z = blue, w = alpha
            ImmediateContext.ClearRenderTargetView(renderTarget.RenderTarget,
                new SharpDX.Mathematics.Interop.RawColor4(color.X, color.Y, color.Z, color.W));
        }

        public void SetViewPort(Rectangle<float> viewPort)
        {
            //set view port

            var viewPortF = new SharpDX.Mathematics.Interop.RawViewportF()
            {
                X = viewPort.Left,
                Y = viewPort.Top,
                Width = viewPort.Right - viewPort.Left,
                Height = viewPort.Bottom - viewPort.Top,
                MinDepth = 0.0f,
                MaxDepth = 1.0f
            };

            ImmediateContext.Rasterizer.SetViewport(viewPortF);
        }

        public void SetRenderTarget(GraphicsRenderTarget renderTarget)
        {
            //set render target
            ImmediateContext.OutputMerger.SetRenderTargets(renderTarget.RenderTarget);
        }

        public void SetInputLayout(GraphicsInputLayout inputLayout)
        {
            //set input layout
            mInputLayout = inputLayout;

            //set input layout Direct3D instance to pipeline
            ImmediateContext.InputAssembler.InputLayout = mInputLayout.InputLayout;
        }

        public void SetVertexShader(GraphicsVertexShader vertexShader)
        {
            //set vertex shader
            mVertexShader = vertexShader;

            //set vertex shader Direct3D instance to pipeline
            ImmediateContext.VertexShader.SetShader(mVertexShader.VertexShader, null, 0);
        }

        public void SetPixelShader(GraphicsPixelShader pixelShader)
        {
            //set pixel shader
            mPixelShader = pixelShader;

            //set pixel shader Direct3D instance to pipeline
            ImmediateContext.PixelShader.SetShader(mPixelShader.PixelShader, null, 0);
        }

        public void SetBuffer(GraphicsBuffer buffer, int register, ShaderType targetShader = ShaderType.VertexShaderAndPixelShader)
        {
            //set buffer Direct3D instance to pipeline's shader
            //we use target shader to flag which shader the buffer will set to

            //test if the buffer can be set
            Debug.Assert((buffer.BindType & GraphicsResourceBindType.ConstantBuffer) != GraphicsResourceBindType.None);

            //we can use "&" to make sure if we need set buffer to vertex shader
            if ((targetShader & ShaderType.VertexShader) != ShaderType.None)
                ImmediateContext.VertexShader.SetConstantBuffer(register, buffer.Resource as SharpDX.Direct3D11.Buffer);

            //we can use "&" to make sure if we need set buffer to pixel shader
            if ((targetShader & ShaderType.PixelShader) != ShaderType.None)
                ImmediateContext.PixelShader.SetConstantBuffer(register, buffer.Resource as SharpDX.Direct3D11.Buffer);
        }

        public void SetVertexBuffer(GraphicsBuffer buffer)
        {
            //set vertex buffer to input stage

            //test if the buffer can be set
            Debug.Assert((buffer.BindType & GraphicsResourceBindType.VertexBufferr) != GraphicsResourceBindType.None);

            //create buffer binding
            var bufferBinding = new SharpDX.Direct3D11.VertexBufferBinding(
                buffer.Resource as SharpDX.Direct3D11.Buffer,
                buffer.ElementSize, 0);

            //set vertex buffer
            ImmediateContext.InputAssembler.SetVertexBuffers(0, bufferBinding);
        }

        public void SetIndexBuffer(GraphicsBuffer buffer)
        {
            //set index buffer to input stage

            //test if the buffer can be set
            Debug.Assert((buffer.BindType & GraphicsResourceBindType.IndexBuffer) != GraphicsResourceBindType.None);

            //set index buffer
            ImmediateContext.InputAssembler.SetIndexBuffer(buffer.Resource as SharpDX.Direct3D11.Buffer,
                 SharpDX.DXGI.Format.R32_UInt, 0);
        }

        public void SetPrimitiveType(PrimitiveType primitiveType)
        {
            //set primitive type
            ImmediateContext.InputAssembler.PrimitiveTopology = GraphicsConvert.ToPrimitiveTopology(primitiveType);
        }

        public void SetBlendState(GraphicsBlendState blendState)
        {
            //set blend state
            //note: if you change the blend state, you need to reset the state
            ImmediateContext.OutputMerger.BlendState = blendState.BlendState;
        }

        public void SetRasterizerState(GraphicsRasterizerState rasterizerState)
        {
            //set rasterizerState
            //note: if you change the rasterizer state, you need to reset the state
            ImmediateContext.Rasterizer.State = rasterizerState.RasterizerState;
        }

        public void DrawIndexed(int indexCount, int startVertexLocation = 0, int baseVertexLocation = 0)
        {
            //draw indexed
            ImmediateContext.DrawIndexed(indexCount, startVertexLocation, baseVertexLocation);
        }

        public void Dispose()
        {
            //we can dispose it any times, because we only dispose resource really at the first time
            SharpDX.Utilities.Dispose(ref mImmediateContext);
            SharpDX.Utilities.Dispose(ref mDevice);
        }
    }
}

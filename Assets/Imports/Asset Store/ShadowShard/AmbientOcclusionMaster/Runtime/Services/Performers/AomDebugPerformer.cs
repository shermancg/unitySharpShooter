using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Enums;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime.Services.Performers
{
    internal class AomDebugPerformer
    {
        private readonly ScriptableRenderer _renderer;
        private readonly Material _material;

        public AomDebugPerformer(
            ScriptableRenderer renderer,
            Material material)
        {
            _renderer = renderer;
            _material = material;
        }

        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        internal void OnCameraSetup(Action<RTHandle> configureTarget) =>
            configureTarget.Invoke(_renderer.cameraColorTargetHandle);

        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        internal void Execute(ScriptableRenderContext context, ProfilingSampler profilingSampler)
        {
            if (_material == null)
            {
                Debug.LogErrorFormat(
                    "{0}.Execute(): Missing material. ShadowShard AmbientOcclusionMasterDebug pass will not execute. Check for missing reference in the renderer resources.",
                    GetType().Name);
                return;
            }

            CommandBuffer cmd = CommandBufferPool.Get();

            using (new ProfilingScope(cmd, profilingSampler))
            {
                ExecuteAoDebugPass(cmd, _renderer.cameraColorTargetHandle);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
        
        private void ExecuteAoDebugPass(CommandBuffer cmd, RTHandle cameraColor) =>
            Blitter.BlitCameraTexture(cmd, cameraColor, cameraColor, RenderBufferLoadAction.Load,
                RenderBufferStoreAction.Store, _material, (int)ShaderPasses.Debug);
    }
}
using System;
using ShadowShard.AmbientOcclusionMaster.Runtime.Data.Settings;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace ShadowShard.AmbientOcclusionMaster.Runtime
{
    [SupportedOnRenderer(typeof(UniversalRendererData))]
    [Serializable]
    [DisallowMultipleRendererFeature("ShadowShard AmbientOcclusion Master")]
    public class AmbientOcclusionMaster : ScriptableRendererFeature
    {
        [SerializeField] [HideInInspector] [Reload("Textures/BlueNoise256/LDR_LLL1_{0}.png", 0, 7)]
        internal Texture2D[] BlueNoise256Textures;

        [SerializeField] [HideInInspector] private Shader _aomShader;
        private Material _aomMaterial;

        private readonly AomSettings _defaultSettings = new();
        
        private AmbientOcclusionMasterPass _ambientOcclusionMasterPass;
        private AmbientOcclusionMasterDebugPass _ambientOcclusionMasterDebugPass;

        private const string AomShaderName = "Hidden/ShadowShard/AmbientOcclusionMaster";

        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif
            
            _ambientOcclusionMasterPass ??= new AmbientOcclusionMasterPass();
            _ambientOcclusionMasterDebugPass ??= new AmbientOcclusionMasterDebugPass();

            GetMaterial(AomShaderName);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (!GetMaterial(AomShaderName))
            {
                Debug.LogErrorFormat("{0}.AddRenderPasses(): Missing material. {1} render pass will not be added.",
                    GetType().Name, name);
                return;
            }

            bool shouldAddAom = _ambientOcclusionMasterPass.Setup(renderer, _defaultSettings, _aomMaterial, BlueNoise256Textures);
            
            if (shouldAddAom)
                renderer.EnqueuePass(_ambientOcclusionMasterPass);

            bool shouldAddDebug = _ambientOcclusionMasterDebugPass.Setup(renderer, _aomMaterial);
            if (shouldAddDebug)
                renderer.EnqueuePass(_ambientOcclusionMasterDebugPass);
        }

        protected override void Dispose(bool disposing)
        {
            _ambientOcclusionMasterPass?.Dispose();
            _ambientOcclusionMasterPass = null;
            _ambientOcclusionMasterDebugPass = null;
            CoreUtils.Destroy(_aomMaterial);
        }

        private bool GetMaterial(string shaderName)
        {
            if (_aomMaterial != null)
                return true;

            if (_aomShader == null)
            {
                _aomShader = Shader.Find(shaderName);
                if (_aomShader == null)
                    return false;
            }

            _aomMaterial = CoreUtils.CreateEngineMaterial(_aomShader);

            return _aomMaterial != null;
        }
    }
}
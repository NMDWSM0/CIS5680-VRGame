using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class ClearDepthFeature : ScriptableRendererFeature
{
    class ClearDepthPass : ScriptableRenderPass
    {
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Clear Depth Pass");
            cmd.ClearRenderTarget(true, false, Color.clear);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    ClearDepthPass m_ScriptablePass;

    public override void Create()
    {
        m_ScriptablePass = new ClearDepthPass();
        m_ScriptablePass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(m_ScriptablePass);
    }
}
#if UDON
using UdonSharp;
using UnityEngine;

namespace _3.UdonSharp
{
	[ExecuteInEditMode]
	public class StealLightmap : UdonSharpBehaviour
	{
		public MeshRenderer lightmappedObject;
		private MeshRenderer currentRenderer;

		private void Start()
		{
			currentRenderer = gameObject.GetComponent<MeshRenderer>();
			RendererInfoTransfer();
		}

		private void OnEnable()
		{
			Start();
		}

#if UNITY_EDITOR
		private void OnBecameVisible()
		{
			RendererInfoTransfer();
		}
#endif

		private void RendererInfoTransfer()
		{
			if (lightmappedObject == null || currentRenderer == null)
				return;

			currentRenderer.lightmapIndex = lightmappedObject.lightmapIndex;
			currentRenderer.lightmapScaleOffset = lightmappedObject.lightmapScaleOffset;
			currentRenderer.realtimeLightmapIndex = lightmappedObject.realtimeLightmapIndex;
			currentRenderer.realtimeLightmapScaleOffset = lightmappedObject.realtimeLightmapScaleOffset;
			currentRenderer.lightProbeUsage = lightmappedObject.lightProbeUsage;
		}
	}
}
#else
using UnityEngine;

namespace _3.Mono
{
    [ExecuteInEditMode]
    public class StealLightmap : MonoBehaviour
    {
        public MeshRenderer lightmappedObject;

        private MeshRenderer currentRenderer;

        private void Awake()
        {
            currentRenderer = gameObject.GetComponent<MeshRenderer>();
            RendererInfoTransfer();
        }

        private void OnEnable()
        {
            Awake();
        }

#if UNITY_EDITOR
        private void OnBecameVisible()
        {
            RendererInfoTransfer();
        }
#endif

        private void RendererInfoTransfer()
        {
            if (lightmappedObject == null || currentRenderer == null)
                return;

            currentRenderer.lightmapIndex = lightmappedObject.lightmapIndex;
            currentRenderer.lightmapScaleOffset = lightmappedObject.lightmapScaleOffset;
            currentRenderer.realtimeLightmapIndex = lightmappedObject.realtimeLightmapIndex;
            currentRenderer.realtimeLightmapScaleOffset = lightmappedObject.realtimeLightmapScaleOffset;
            currentRenderer.lightProbeUsage = lightmappedObject.lightProbeUsage;
        }
    }
}
#endif
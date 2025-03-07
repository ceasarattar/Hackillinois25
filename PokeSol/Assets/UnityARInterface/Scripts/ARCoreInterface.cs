using System;
using System.Collections.Generic;
using UnityEngine;
using GoogleARCore;
using System.Collections;
using System.Runtime.InteropServices;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation; // Use ARFoundation for background rendering

namespace UnityARInterface
{
    public class ARCoreInterface : ARInterface
    {
        #region ARCoreCameraAPI
        public const string ARCoreCameraUtilityAPI = "arcore_camera_utility";

        private const int k_ARCoreTextureWidth = 640;
        private const int k_ARCoreTextureHeight = 480;

        private const ImageFormatType k_ImageFormatType = ImageFormatType.ImageFormatColor;

        [DllImport(ARCoreCameraUtilityAPI)]
        public static extern void TextureReader_create(int format, int width, int height, bool keepAspectRatio);

        [DllImport(ARCoreCameraUtilityAPI)]
        public static extern void TextureReader_destroy();

        [DllImport(ARCoreCameraUtilityAPI)]
        public static extern IntPtr TextureReader_submitAndAcquire(
            int textureId, int textureWidth, int textureHeight, ref int bufferSize);

        private enum ImageFormatType
        {
            ImageFormatColor = 0,
            ImageFormatGrayscale = 1
        }

        private byte[] pixelBuffer;

        #endregion

        private List<TrackedPlane> m_TrackedPlaneBuffer = new List<TrackedPlane>();
        private ScreenOrientation m_CachedScreenOrientation;
        private Dictionary<TrackedPlane, BoundedPlane> m_TrackedPlanes = new Dictionary<TrackedPlane, BoundedPlane>();
        private ARCoreSession m_ARCoreSession;
        private ARCoreSessionConfig m_ARCoreSessionConfig;
        private ARCameraBackground m_BackgroundRenderer; // Replaced ARBackgroundRenderer with ARCameraBackground
        private Matrix4x4 m_DisplayTransform = Matrix4x4.identity;
        private List<Vector4> m_TempPointCloud = new List<Vector4>();
        private Dictionary<ARAnchor, Anchor> m_Anchors = new Dictionary<ARAnchor, Anchor>();
        private bool m_BackgroundRendering;

        public override bool IsSupported
        {
            get
            {
                return
                    Session.Status != SessionStatus.ErrorApkNotAvailable &&
                    Session.Status != SessionStatus.ErrorSessionConfigurationNotSupported;
            }
        }

        public override bool BackgroundRendering
        {
            get
            {
                return m_BackgroundRendering;
            }
            set
            {
                if (m_BackgroundRenderer == null)
                    return;

                m_BackgroundRendering = value;
                m_BackgroundRenderer.enabled = m_BackgroundRendering;
            }
        }

        public override IEnumerator StartService(Settings settings)
        {
            if (m_ARCoreSessionConfig == null)
                m_ARCoreSessionConfig = ScriptableObject.CreateInstance<ARCoreSessionConfig>();

            m_ARCoreSessionConfig.EnableLightEstimation = settings.enableLightEstimation;
            m_ARCoreSessionConfig.EnablePlaneFinding = settings.enablePlaneDetection;
            m_ARCoreSessionConfig.MatchCameraFramerate = true;

            if (m_ARCoreSession == null)
            {
                var go = new GameObject("ARCore Session");
                go.SetActive(false);
                m_ARCoreSession = go.AddComponent<ARCoreSession>();
                m_ARCoreSession.SessionConfig = m_ARCoreSessionConfig;
                go.SetActive(true);
            }

            m_ARCoreSession.SessionConfig = m_ARCoreSessionConfig;
            m_ARCoreSession.enabled = true;

            if (!IsSupported)
            {
                Debug.LogError("ARCore is not supported on this device.");
                yield break;
            }

            while (!Session.Status.IsValid())
            {
                IsRunning = false;

                if (Session.Status.IsError())
                {
                    Debug.LogError("ARCore encountered an error: " + Session.Status);
                    yield break;
                }

                yield return null;
            }

            IsRunning = true;
            TextureReader_create((int)k_ImageFormatType, k_ARCoreTextureWidth, k_ARCoreTextureHeight, true);
        }

        public override void StopService()
        {
            foreach (var anchor in m_Anchors.Keys)
            {
                DestroyAnchor(anchor);
            }

            m_ARCoreSession.enabled = false;
            TextureReader_destroy();
            BackgroundRendering = false;
            if (m_BackgroundRenderer != null)
            {
                m_BackgroundRenderer.enabled = false;
            }
            IsRunning = false;
        }

        public override bool TryGetUnscaledPose(ref Pose pose)
        {
            if (Session.Status != SessionStatus.Tracking)
                return false;

            pose.position = Frame.Pose.position;
            pose.rotation = Frame.Pose.rotation;
            return true;
        }

        public override bool TryGetCameraImage(ref CameraImage cameraImage)
        {
            if (Session.Status != SessionStatus.Tracking)
                return false;

            if (Frame.CameraImage.Texture == null)
                return false;

            int textureId = Frame.CameraImage.Texture.GetNativeTexturePtr().ToInt32();
            int bufferSize = 0;
            IntPtr bufferPtr = TextureReader_submitAndAcquire(textureId, k_ARCoreTextureWidth, k_ARCoreTextureHeight, ref bufferSize);

            GL.InvalidateState();

            if (bufferPtr == IntPtr.Zero || bufferSize == 0)
                return false;

            if (pixelBuffer == null || pixelBuffer.Length != bufferSize)
                pixelBuffer = new byte[bufferSize];

            Marshal.Copy(bufferPtr, pixelBuffer, 0, bufferSize);

            PixelBuffertoYUV2(pixelBuffer, k_ARCoreTextureWidth, k_ARCoreTextureHeight,
                              k_ImageFormatType, ref cameraImage.y, ref cameraImage.uv);

            cameraImage.width = k_ARCoreTextureWidth;
            cameraImage.height = k_ARCoreTextureHeight;

            return true;
        }

        public override Matrix4x4 GetDisplayTransform()
        {
            return m_DisplayTransform;
        }

        public override void SetupCamera(Camera camera)
        {
            m_BackgroundRenderer = camera.GetComponent<ARCameraBackground>();

            if (m_BackgroundRenderer == null)
            {
                m_BackgroundRenderer = camera.gameObject.AddComponent<ARCameraBackground>();
            }

            m_BackgroundRenderer.enabled = true;
        }

        public override void UpdateCamera(Camera camera)
        {
        }

        public override void Update()
        {
            if (m_ARCoreSession == null || Session.Status != SessionStatus.Tracking)
                return;

            AsyncTask.OnUpdate();
        }

        public override void ApplyAnchor(ARAnchor arAnchor)
        {
            if (!IsRunning)
                return;

            Anchor arCoreAnchor = Session.CreateAnchor(new Pose(arAnchor.transform.position, arAnchor.transform.rotation));
            arAnchor.anchorID = Guid.NewGuid().ToString();
            m_Anchors[arAnchor] = arCoreAnchor;
        }

        public override bool TryGetPointCloud(ref ARInterface.PointCloud pointCloud)
        {
            if (Session.Status != SessionStatus.Tracking)
                return false;

            // Fill in the data to draw the point cloud.
            m_TempPointCloud.Clear();
            Frame.PointCloud.CopyPoints(m_TempPointCloud);

            if (m_TempPointCloud.Count == 0)
                return false;

            if (pointCloud.points == null)
                pointCloud.points = new List<Vector3>();

            pointCloud.points.Clear();
            foreach (Vector3 point in m_TempPointCloud)
                pointCloud.points.Add(point);

            return true;
        }

        public override ARInterface.LightEstimate GetLightEstimate()
        {
            if (Session.Status.IsValid() && Frame.LightEstimate.State == LightEstimateState.Valid)
            {
                return new ARInterface.LightEstimate()
                {
                    capabilities = ARInterface.LightEstimateCapabilities.AmbientIntensity,
                    ambientIntensity = Frame.LightEstimate.PixelIntensity
                };
            }
            else
            {
                // Zero initialized means capabilities will be None
                return new ARInterface.LightEstimate();
            }
        }


        public override void DestroyAnchor(ARAnchor arAnchor)
        {
            if (!string.IsNullOrEmpty(arAnchor.anchorID))
            {
                if (m_Anchors.TryGetValue(arAnchor, out Anchor arCoreAnchor))
                {
                    UnityEngine.Object.Destroy(arCoreAnchor);
                    m_Anchors.Remove(arAnchor);
                }
                arAnchor.anchorID = null;
            }
        }
    }
}

using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using System;

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    public class PoseLandmarkerRunner : VisionTaskApiRunner<PoseLandmarker>
    {
        // -----------------------------
        // Singleton（跨場景保持）
        // -----------------------------
        public static PoseLandmarkerRunner Instance { get; private set; }

        public Vector3[] LatestWorldPoints { get; private set; }   // 最新 33 點
        public bool HasResult => LatestWorldPoints != null;

        public event Action<Vector3[]> OnLandmarkUpdated; // 事件式回傳

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // -----------------------------
        [SerializeField] 
        private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;

        private Experimental.TextureFramePool _textureFramePool;

        public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
        }

        // -----------------------------
        // MediaPipe 主流程
        // -----------------------------
        protected override IEnumerator Run()
        {
            Debug.Log("[PoseLandmarker] Start Running...");

            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            var options = config.GetPoseLandmarkerOptions(
                config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ?
                OnPoseLandmarkDetectionOutput : null
            );

            taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
            var imageSource = ImageSourceProvider.ImageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Logger.LogError(TAG, "ImageSource 啟動失敗");
                yield break;
            }

            _textureFramePool = new Experimental.TextureFramePool(
                imageSource.textureWidth,
                imageSource.textureHeight,
                TextureFormat.RGBA32,
                10);

            screen.Initialize(imageSource);

            SetupAnnotationController(_poseLandmarkerResultAnnotationController, imageSource);
            _poseLandmarkerResultAnnotationController.InitScreen(imageSource.textureWidth, imageSource.textureHeight);

            var transformation = imageSource.GetTransformationOptions();
            bool flipH = transformation.flipHorizontally;
            bool flipV = transformation.flipVertically;

            var imageOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

            AsyncGPUReadbackRequest req = default;
            var waitReq = new WaitUntil(() => req.done);

            var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);

            bool canUseGpu = options.baseOptions.delegateCase == Tasks.Core.BaseOptions.Delegate.GPU &&
                              SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 &&
                              GpuManager.GpuResources != null;

            using var glContext = canUseGpu ? GpuManager.GetGlContext() : null;

            while (true)
            {
                if (isPaused)
                    yield return new WaitWhile(() => isPaused);

                if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return new WaitForEndOfFrame();
                    continue;
                }

                Image image;

                if (canUseGpu)
                {
                    yield return new WaitForEndOfFrame();
                    textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipH, flipV);
                    image = textureFrame.BuildGpuImage(glContext);
                }
                else
                {
                    req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipH, flipV);
                    yield return waitReq;

                    if (req.hasError)
                    {
                        Debug.LogError("[PoseLandmarker] GPU Readback Error");
                        break;
                    }

                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release();
                }

                switch (taskApi.runningMode)
                {
                    case Tasks.Vision.Core.RunningMode.IMAGE:
                        if (taskApi.TryDetect(image, imageOptions, ref result))
                            _poseLandmarkerResultAnnotationController.DrawNow(result);
                        break;

                    case Tasks.Vision.Core.RunningMode.VIDEO:
                        if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageOptions, ref result))
                            _poseLandmarkerResultAnnotationController.DrawNow(result);
                        break;

                    case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageOptions);
                        break;
                }
            }
        }

        // -----------------------------
        // MediaPipe Callback
        // -----------------------------
        private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Image image, long timestamp)
        {
            _poseLandmarkerResultAnnotationController.DrawLater(result);

            if (result.poseWorldLandmarks == null || result.poseWorldLandmarks.Count == 0)
                return;

            var src = result.poseWorldLandmarks[0].landmarks;
            Vector3[] points = new Vector3[src.Count];

            for (int i = 0; i < src.Count; i++)
            {
                var lm = src[i];
                points[i] = new Vector3(lm.x, -lm.y, -lm.z);
            }

            LatestWorldPoints = points;                // 最新資料（給 Update 使用）
            OnLandmarkUpdated?.Invoke(points);         // 事件模式（給 Receiver）
        }
    }
}

using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using System;
using Mediapipe.Unity.Sample; // 引用父類別 VisionTaskApiRunner

namespace Mediapipe.Unity.Sample.PoseLandmarkDetection
{
    // 繼承 VisionTaskApiRunner 以自動處理 AssetLoader 和 Bootstrap
    public class PoseLandmarkerRunner_NoUI : VisionTaskApiRunner<PoseLandmarker>
    {
        // -----------------------------
        // Singleton
        // -----------------------------
        public static PoseLandmarkerRunner_NoUI Instance { get; private set; }

        public Vector3[] LatestWorldPoints { get; private set; }
        public bool HasResult => LatestWorldPoints != null;
        public event Action<Vector3[]> OnLandmarkUpdated;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // -----------------------------
        // 設定
        // -----------------------------
        private Experimental.TextureFramePool _textureFramePool;
        public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();

        public override void Stop()
        {
            base.Stop();
            _textureFramePool?.Dispose();
            _textureFramePool = null;
        }

        // -----------------------------
        // 核心運算 (重寫 Run 方法，移除畫圖邏輯)
        // -----------------------------
        protected override IEnumerator Run()
        {
            Debug.Log("[NoUI Runner] Start Running...");

            // 1. 載入模型 (繼承自父類別，自動處理 Resource)
            yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

            // ★ 修正點 1: 先設定 Config 的模式
            config.RunningMode = Mediapipe.Tasks.Vision.Core.RunningMode.LIVE_STREAM;

            // ★ 修正點 2: 這裡改為只傳入 1 個參數 (Callback)，解決 CS1501 錯誤
            var options = config.GetPoseLandmarkerOptions(OnPoseLandmarkDetectionOutput);

            // 3. 建立 API
            taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
            
            // 4. 啟動 ImageSource
            var imageSource = ImageSourceProvider.ImageSource;
            
            if (imageSource == null) yield return new WaitUntil(() => ImageSourceProvider.ImageSource != null);
            imageSource = ImageSourceProvider.ImageSource;

            yield return imageSource.Play();

            if (!imageSource.isPrepared)
            {
                Debug.LogError("ImageSource 啟動失敗");
                yield break;
            }

            // 5. 建立 TextureFramePool
            _textureFramePool = new Experimental.TextureFramePool(
                imageSource.textureWidth,
                imageSource.textureHeight,
                TextureFormat.RGBA32,
                10);

            // ★ 關鍵修改：移除了 screen.Initialize() 和 SetupAnnotationController()
            // 這樣就不會顯示畫面，也不會畫紅線

            var transformation = imageSource.GetTransformationOptions();
            bool flipH = transformation.flipHorizontally;
            bool flipV = transformation.flipVertically;

            var imageOptions = new Mediapipe.Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: 0);

            // 設定 GPU
            bool canUseGpu = options.baseOptions.delegateCase == Mediapipe.Tasks.Core.BaseOptions.Delegate.GPU &&
                             SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 &&
                             GpuManager.GpuResources != null;

            using var glContext = canUseGpu ? GpuManager.GetGlContext() : null;

            // 6. 偵測迴圈
            while (true)
            {
                if (isPaused) yield return new WaitWhile(() => isPaused);

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
                    var req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipH, flipV);
                    yield return new WaitUntil(() => req.done);
                    
                    if (req.hasError) 
                    { 
                        textureFrame.Release(); 
                        continue; 
                    }
                    
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release();
                }

                // 執行偵測 (只運算，不呼叫 DrawNow)
                taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageOptions);
            }
        }

        // -----------------------------
        // 回傳 Callback
        // -----------------------------
        private void OnPoseLandmarkDetectionOutput(PoseLandmarkerResult result, Image image, long timestamp)
        {
            if (result.poseWorldLandmarks == null || result.poseWorldLandmarks.Count == 0)
                return;

            var src = result.poseWorldLandmarks[0].landmarks;
            Vector3[] points = new Vector3[src.Count];

            for (int i = 0; i < src.Count; i++)
            {
                var lm = src[i];
                points[i] = new Vector3(lm.x, -lm.y, -lm.z);
            }

            LatestWorldPoints = points;
            OnLandmarkUpdated?.Invoke(points);
        }
    }
}
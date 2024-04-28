using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CodeBase.Decompression;
using CodeBase.Extensions;
using CodeBase.Models.BraydenModels.Breath;
using CodeBase.Models.BraydenModels.Breath.Exhalation;
using CodeBase.ReportModules.Models;
using CodeBase.ReportVisualizers;
using CodeBase.Settings;
using Cysharp.Threading.Tasks;
using Modules.Common;
using TMPro;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.UI;
using static CodeBase.BraydenVisualizerConstants;
using static CodeBase.BraydenVisualizerConstants.CompressionVisualizerConstants;
using static CodeBase.Settings.BraydenConfigKeys;

namespace CodeBase
{
    public class BraydenResultsViewController : MonoBehaviour
    {
        private const float MINIMAL_FREQUENCY_SCALE = 0f;
        private const float ABSOLUTE_MIDPOINT_FREQUENCY_SCALE = 0.5f; //it`s 50%

        [SerializeField] private BraydenCycleVisualizer cycleVisualizer;
        
        [Header("Colors")]
        [SerializeField] private Color inactiveElementColor;

        [Space(20)]
        [Header("Main description controls")]
        [SerializeField] private TMP_Text cyclesCountText;
        [SerializeField] private TMP_Text compressionsCountText;
        [SerializeField] private TMP_Text breatheCountText;
        [SerializeField] private List<TMP_Text> totalTimeTexts;

        [Space(20)]
        [Header("Compression controls lists")]
        [Space(5)] [SerializeField] private List<Image> compressionImages;
        [Space(5)] [SerializeField] private List<Image> frequencyImages;
        [Space(5)] [SerializeField] private List<Image> handsImages;

        [Space(20)]
        [Header("Compression graph controls")]
        [SerializeField] private RectTransform compressionsGraphImage;
        [SerializeField] private CompressionPointVisualizer compressionPointPrefab;
        [SerializeField] private DecompressionPointVisualizer decompressionPointPrefab;

        [Space(20)]
        [Header("Breathe controls")]
        [SerializeField] private List<Image> breatheImages;
        [SerializeField] private TMP_Text correctInhalationsText;
        [SerializeField] private TMP_Text incorrectInhalationsText;

        private List<CompressionPointVisualizer> _currentCompressionPoints = new();
        private List<DecompressionPointVisualizer> _currentDecompressionPoints = new();

        private readonly List<string> _timeTextsDefaultValues = new();
        private float _halfPointHeight;

        private BraydenGlobalSettingsManager _settings;
        #region Event functions

        private void Start()
        {
            InitTimeTextsDefaultsValues();
            _halfPointHeight = compressionPointPrefab.GetHeight() / 2f;
        }

        private void InitTimeTextsDefaultsValues()
        {
            foreach (var timeText in totalTimeTexts)
                _timeTextsDefaultValues.Add(timeText.text);
        }


        #endregion

        public void Init(BraydenGlobalSettingsManager settingsManager)
        {
            _settings = settingsManager;
        }
        
        public void VisualizeFinishedCompression(CompressionModel model)
        {
            if (model == null)
                return;

            VisualizeCompressionFrequency(model.FrequencyValue);
            VisualizeCompressionHandsPosition(model);
            VisualizeCompressionPointOnGraph(model);
            VisualizeCompressionDepth(model, true);
        }

        private void VisualizeCompressionPointOnGraph(CompressionModel model)
        {
            var graphSizeDelta = compressionsGraphImage.rect.size;
            var graphWidth = graphSizeDelta.x;
            var graphHeight = graphSizeDelta.y;
            var shiftHeight = graphHeight / compressionImages.Count;

            var widthShift = graphWidth / FREQUENCY_TYPES_COUNT;
            var frequencyPercent = CalculateFrequencyPointerPercentRatioPositionX(model.FrequencyValue);
            var startShift = model.FrequencyValue.SpeedType switch
            {
                FrequencyType.Slow => 0f,
                FrequencyType.Norm => widthShift,
                FrequencyType.None => widthShift,
                FrequencyType.Fast => widthShift * 2,
                _ => throw new ArgumentOutOfRangeException()
            };

            var frequencyPosition = widthShift * frequencyPercent + startShift;
            var differenceOfWidth = graphWidth - _halfPointHeight;

            if (differenceOfWidth <= 0)
            {
                Debug.Log("Error of difference calculation");
                return;
            }

            frequencyPosition = Math.Clamp(frequencyPosition, 0f, differenceOfWidth);

            var depth = Math.Clamp(model.ResultValue, DEPTH_GRAPH_MINIMUM, DEPTH_GRAPH_MAXIMUM);
            var depthPosition = 0f;
            
            const int reverseCoefficient = -1; //because coordinates from top to bottom

            var minDepthNorma = _settings.ConfigManager.GetParameterValue(MIN_DEPTH_NORMA_KEY);
            var maxDepthNorma = _settings.ConfigManager.GetParameterValue(MAX_DEPTH_NORMA_KEY);
            
            switch (model.Status)
            {
                case DepthStatus.Norm:
                {
                    var minHeight = shiftHeight * 2;

                    var currentDepthPercent = depth - minDepthNorma;

                    var shiftByDepth = shiftHeight * currentDepthPercent;
                    var shiftDepth = minHeight + shiftByDepth;

                    depthPosition = reverseCoefficient * shiftDepth + _halfPointHeight;
                    break;
                }
                case DepthStatus.Weak:
                {
                    var currentDepthPercent = depth / minDepthNorma;

                    var shiftDepth = 2 * shiftHeight * currentDepthPercent;
                    depthPosition = reverseCoefficient * shiftDepth + _halfPointHeight;
                    break;
                }
                case DepthStatus.Strong:
                {
                    var minHeight = shiftHeight * 3;

                    var currentDepthPercent = depth - maxDepthNorma;

                    var shiftByDepth = shiftHeight * currentDepthPercent;
                    var shiftDepth = minHeight + shiftByDepth;

                    depthPosition = reverseCoefficient * shiftDepth + _halfPointHeight;
                    break;
                }
            }

            var resultPosition = new Vector2(frequencyPosition, depthPosition);
            var point = Instantiate(compressionPointPrefab, compressionsGraphImage.transform);
            point.SetPosition(resultPosition);

            _currentCompressionPoints.Add(point);
        }

        private void VisualizeDecompressionPointOnGraph(DecompressionModel model)
        {
            if (model == null || model.ResultValue <= 0f)
                return;

            if (_lastVisualizedCompressionModel == null || _lastVisualizedCompressionModel.Gradations.IsNullOrEmpty())
                return;

            var depth = Math.Clamp(model.ResultValue, DEPTH_GRAPH_MINIMUM, DEPTH_GRAPH_MAXIMUM);
            if (depth <= 0.5f)
                return;

            var graphSizeDelta = compressionsGraphImage.rect.size;
            var graphWidth = graphSizeDelta.x;
            var graphHeight = graphSizeDelta.y;
            var shiftHeight = graphHeight / compressionImages.Count;

            var widthShift = graphWidth / FREQUENCY_TYPES_COUNT;
            var frequencyPercent = CalculateFrequencyPointerPercentRatioPositionX(_lastVisualizedCompressionModel.FrequencyValue);
            var startShift = _lastVisualizedCompressionModel.FrequencyValue.SpeedType switch
            {
                FrequencyType.Slow => 0f,
                FrequencyType.Norm => widthShift,
                FrequencyType.Fast => widthShift * 2,
                _ => throw new ArgumentOutOfRangeException()
            };

            var frequencyPosition = widthShift * frequencyPercent + startShift;
            frequencyPosition = Math.Clamp(frequencyPosition, 0f, graphWidth - _halfPointHeight);

            var reverseCoefficient = -1;

            var minDepthNorma = _settings.ConfigManager.GetParameterValue(MIN_DEPTH_NORMA_KEY);

            var currentDepthPercent = depth / minDepthNorma;

            var shiftDepth = 2 * shiftHeight * currentDepthPercent;
            var depthPosition = reverseCoefficient * shiftDepth + _halfPointHeight;

            var resultPosition = new Vector2(frequencyPosition, depthPosition);
            var point = Instantiate(decompressionPointPrefab, compressionsGraphImage.transform);
            point.SetPosition(resultPosition);

            _currentDecompressionPoints.Add(point);
        }

        public void VisualizeCompressionDepth(CompressionModel model, bool isFinished = false)
        {
            if (model == null || model.Gradations.IsNullOrEmpty())
            {
                DeactivateCompressionImages();
                return;
            }

            var gradations = model.Gradations;

            var depth = _settings.CompressionManager.GetCompressionDepth(gradations);

            if (depth <= 0.5f)
            {
                DeactivateCompressionImages();
                return;
            }

            var status = _settings.CompressionManager.GetCompressionStatus(depth);
            var minDepthNorma = _settings.ConfigManager.GetParameterValue(MIN_DEPTH_NORMA_KEY);

            var maxIndexOfDepth = 0;
            switch (status)
            {
                case DepthStatus.Norm:
                {
                    maxIndexOfDepth = compressionImages.Count - 1;
                    break;
                }

                case DepthStatus.Strong:
                {
                    maxIndexOfDepth = compressionImages.Count;
                    break;
                }

                case DepthStatus.Weak:
                {
                    var middlePoint = minDepthNorma / 2f;

                    maxIndexOfDepth = depth < middlePoint
                        ? 1
                        : 2;

                    break;
                }
            }

            var colors = new List<Color>();
            Color resultColor;

            if (!model.IsCompressionStartedCorrect())
                resultColor = OverNormaColor;
            else
            {
                resultColor = maxIndexOfDepth == compressionImages.Count
                    ? OverNormaColor
                    : maxIndexOfDepth == compressionImages.Count - 1
                        ? NormaColor
                        : UnderNormaColor;
            }

            for (var i = 0; i < maxIndexOfDepth; i++)
            {
                if (!model.IsCompressionStartedCorrect())
                {
                    colors.Add(OverNormaColor);
                    continue;
                }

                if (i == compressionImages.Count - 1)
                    colors.Add(OverNormaColor);
                else if (i == compressionImages.Count - 2)
                    colors.Add(NormaColor);
                else
                    colors.Add(UnderNormaColor);
            }

            for (var i = 0; i < maxIndexOfDepth; i++)
            {
                if (i == compressionImages.Count - 2)
                {
                    ActivateCompressionImage(compressionImages[0], NormaColor);
                    ActivateCompressionImage(compressionImages[1], NormaColor);
                    ActivateCompressionImage(compressionImages[2], NormaColor);
                    continue;
                }

                if (i == compressionImages.Count - 1)
                {
                    ActivateCompressionImage(compressionImages[0], OverNormaColor);
                    ActivateCompressionImage(compressionImages[1], OverNormaColor);
                    ActivateCompressionImage(compressionImages[2], OverNormaColor);
                    ActivateCompressionImage(compressionImages[3], OverNormaColor);
                    continue;
                }

                ActivateCompressionImage(compressionImages[i], colors[i]);
            }

            if (isFinished)
            {
                VisualizeCompressionFinalResult(resultColor, maxIndexOfDepth);
                _lastVisualizedCompressionModel = model;
            }
        }

        private void VisualizeCompressionFinalResult(Color clr, int index)
        {
            for (var i = 0; i < index; i++)
                ActivateCompressionImage(compressionImages[i], clr);
        }

        private void VisualizeCompressionHandsPosition(CompressionModel model)
        {
            var handsPos = model.PositionOfHands;
            DeactivateHandsPositionImages();

            if (handsPos is HandsPosition.None)
                return;

            var imgIndex = handsPos switch
            {
                HandsPosition.Center => 0,
                HandsPosition.Left => 1,
                HandsPosition.Right => 2,
                HandsPosition.Down => 3,
                HandsPosition.Top => 4,
                _ => throw new ArgumentOutOfRangeException()
            };

            var handsImage = handsImages[imgIndex];

            handsImage.color = handsPos.GetColor();
        }

        private void DeactivateHandsPositionImages()
        {
            foreach (var img in handsImages)
                img.color = inactiveElementColor;
        }

        private static void ActivateCompressionImage(Image img, Color clr)
        {
            img.color = clr;
        }

        private CompressionModel _lastVisualizedCompressionModel;

        public void VisualizeDecompressionDepth(DecompressionModel model, bool isFinish = false)
        {
            if (model == null || model.Gradations.IsNullOrEmpty())
            {
                DeactivateCompressionImages();
                return;
            }

            var depth = _settings.DecompressionManager.GetDecompressionDepth(model);
            var status = _settings.CompressionManager.GetCompressionStatus(depth);

            var index = 0;
            switch (status)
            {
                case DepthStatus.Norm:
                {
                    index = compressionImages.Count - 2;
                    break;
                }

                case DepthStatus.Strong:
                {
                    index = compressionImages.Count - 1;
                    break;
                }

                case DepthStatus.Weak:
                {
                    index = depth <= 0.5f
                        ? 0
                        : 1;
                    break;
                }
            }

            for (var i = compressionImages.Count - 1; i >= index; i--)
                DeactivateCompressionImage(compressionImages[i]);

            if (isFinish)
            {
                VisualizeDecompressionPointOnGraph(model);
            }
        }

        private void DeactivateCompressionImage(Image img)
        {
            img.color = inactiveElementColor;
        }

        private void DeactivateCompressionImages()
        {
            foreach (var img in compressionImages)
                img.color = inactiveElementColor;
        }

        #region Frequency visualization logic

        private void VisualizeCompressionFrequency(Frequency frequency)
        {
            DeactivateFrequencyImages();
            ActivateFrequencyImageByType(frequency.SpeedType);
        }

        private void DeactivateFrequencyImages()
        {
            foreach (var img in frequencyImages)
                img.color = inactiveElementColor;
        }

        private void ActivateFrequencyImageByType(FrequencyType frType)
        {
            var index = frType switch
            {
                FrequencyType.None or FrequencyType.Norm => 1,
                FrequencyType.Slow => 0,
                FrequencyType.Fast => 2,
                _ => throw new ArgumentOutOfRangeException(nameof(frType), frType, null)
            };

            var currentImg = frequencyImages[index];
            var highlightColor = frType.GetColor();
            currentImg.color = highlightColor;
        }

        private float CalculateFrequencyPointerPercentRatioPositionX(Frequency frequency)
        {
            var frequencyValue = frequency.ResultValue;

            float scaleLimitValue;
            float shiftedFrequency;

            var maxFrequencyNorma = _settings.ConfigManager.GetParameterValue(MAX_FREQUENCY_NORMA_KEY);
            var minFrequencyNorma = _settings.ConfigManager.GetParameterValue(MIN_FREQUENCY_NORMA_KEY);

            var frequencyScale = minFrequencyNorma + maxFrequencyNorma;

            switch (frequency.SpeedType)
            {
                case FrequencyType.None:
                    return ABSOLUTE_MIDPOINT_FREQUENCY_SCALE;

                case FrequencyType.Norm:
                    scaleLimitValue = maxFrequencyNorma - minFrequencyNorma;
                    shiftedFrequency = frequencyValue - minFrequencyNorma;
                    break;

                case FrequencyType.Slow:
                    scaleLimitValue = minFrequencyNorma - MINIMAL_FREQUENCY_SCALE;
                    shiftedFrequency = frequencyValue;
                    break;

                case FrequencyType.Fast:
                    scaleLimitValue = frequencyScale - maxFrequencyNorma;
                    shiftedFrequency = frequencyValue - maxFrequencyNorma;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            var result = shiftedFrequency / scaleLimitValue;
            result = Math.Clamp(result, 0f, 1f);
            return result;
        }

        #endregion

        public void VisualizeInhalation(InhalationModel model, bool isFinished = false)
        {
            var gradations = model.Gradations;
            if (gradations.IsNullOrEmpty())
            {
                DeactivateBreatheImages();
                return;
            }

            var capacity = _settings.BreatheManager.GetLungCapacity(gradations);
            if (capacity.Status is LungCapacityStatus.None)
            {
                DeactivateBreatheImages();
                return;
            }

            var maxNorma = _settings.ConfigManager.GetParameterValue(MAX_CAPACITY_NORMA_KEY);
            var minNorma = _settings.ConfigManager.GetParameterValue(MIN_CAPACITY_NORMA_KEY);
            var maximumCapacity = maxNorma + (maxNorma - minNorma);

            var capacityValue = Math.Clamp(capacity.CapacityValue, 0f, maximumCapacity) / 100f;

            var maxIndexOfInhalation = (int)Math.Round(capacityValue);

            var isTooMuchCapacity = false;
            switch (capacity.Status)
            {
                case LungCapacityStatus.NotEnough:
                    if (maxIndexOfInhalation == breatheImages.Count - 1)
                        maxIndexOfInhalation--;
                    break;

                case LungCapacityStatus.Normal:
                    maxIndexOfInhalation = breatheImages.Count;
                    break;

                case LungCapacityStatus.TooMuch:
                    maxIndexOfInhalation = breatheImages.Count;
                    isTooMuchCapacity = true;
                    break;
            }

            for (var i = 0; i < maxIndexOfInhalation; i++)
                ActivateBreatheImage(breatheImages[i]);

            if (isTooMuchCapacity)
                breatheImages.Last().color = OverNormaColor;
            else if (maxIndexOfInhalation == breatheImages.Count)
                breatheImages.Last().color = NormaColor;

            if (isFinished)
                ShowInhalationsCountByCorrectness(model);
        }

        private void ShowInhalationsCountByCorrectness(InhalationModel model)
        {
            var currentCorrectCount = int.Parse(correctInhalationsText.text);
            var currentIncorrectCount = int.Parse(incorrectInhalationsText.text);

            var status = _settings.BreatheManager.GetLungCapacity(model.Gradations).Status;
            switch (status)
            {
                case LungCapacityStatus.None:
                    return;
                case LungCapacityStatus.Normal:
                    currentCorrectCount++;
                    break;
                case LungCapacityStatus.NotEnough:
                case LungCapacityStatus.TooMuch:
                default:
                    currentIncorrectCount++;
                    break;
            }

            correctInhalationsText.text = currentCorrectCount.ToString();
            incorrectInhalationsText.text = currentIncorrectCount.ToString();
        }

        private void ActivateBreatheImage(Image img)
        {
            img.enabled = true;
        }

        public void VisualizeExhalation(ExhalationModel model)
        {
            var gradations = model.Gradations;
            if (gradations.IsNullOrEmpty())
            {
                DeactivateBreatheImages();
                return;
            }

            var minByte = gradations.Select(x => x.ByteValue).Min();
            var lungVolume = _settings.UnitConvertManager.GetMlFromAmounthOfBreathe(minByte);

            var maxNorma = _settings.ConfigManager.GetParameterValue(MAX_CAPACITY_NORMA_KEY);
            var minNorma = _settings.ConfigManager.GetParameterValue(MIN_CAPACITY_NORMA_KEY);
            
            var maximumCapacity = maxNorma + (maxNorma - minNorma);

            var capacityValue = Math.Clamp(lungVolume, 0f, maximumCapacity) / 100f;

            var index = (int)Math.Round(capacityValue);
            index = Math.Clamp(index, 0, breatheImages.Count);

            for (var i = breatheImages.Count - 1; i >= index; i--)
                DeactivateBreatheImage(breatheImages[i]);
        }

        private void DeactivateBreatheImages()
        {
            foreach (var img in breatheImages)
                img.enabled = false;
        }

        private static void DeactivateBreatheImage(Image img)
        {
            img.enabled = false;
        }

        #region Count visualize methods

        public void UpdateCycleText(BraydenCycle cycle)
        {
            cyclesCountText.text = $"{cycle.Number.ToString()}";
        }

        public void UpdateCompressionsText(string compressionsDescription)
        {
            compressionsCountText.text = compressionsDescription;
        }

        public void UpdateBreatheText(string inhalationDescription)
        {
            breatheCountText.text = inhalationDescription;
        }

        #endregion

        public void ResetUI()
        {
            ResetDescriptionTexts();
            ResetCompressionPoints();
            ResetDecompressionPoints();

            DeactivateCompressionImages();
            DeactivateHandsPositionImages();
            DeactivateFrequencyImages();

            DeactivateBreatheImages();
        }

        private void ResetDescriptionTexts()
        {
            compressionsCountText.text = _settings.CompressionManager.GetCompressionDescription();
            breatheCountText.text = _settings.BreatheManager.GetBreatheDescription();

            correctInhalationsText.text = "0";
            incorrectInhalationsText.text = "0";
        }

        public void ResetTimeTexts()
        {
            for (var i = 0; i < totalTimeTexts.Count; i++)
                totalTimeTexts[i].text = _timeTextsDefaultValues[i];
        }

        public async UniTask CaptureCycleScreens(List<BraydenCycle> cycles)
        {
            if (cycles.IsNullOrEmpty())
                return;
            
            for (var i = 0; i < cycles!.Count; i++)
            {
                captureCamera.gameObject.SetActive(true);
                captureCanvas.gameObject.SetActive(true);
                
                var currentCycle = cycles[i];
                var currentResults = new List<CycleResultModel>();
                var cycleManager = _settings.CycleManager;
                foreach (CycleResultType cycleResultType in Enum.GetValues(typeof(CycleResultType)))
                {
                    var cycleResultModel = cycleManager.CalculateCycleResults(cycleResultType, currentCycle);
                    currentResults.Add(cycleResultModel);
                }
                
                var visualizerCopy = Instantiate(cycleVisualizer, captureCanvas.transform, false);
                visualizerCopy.gameObject.SetActive(false);
             
                var visualizerRectTransform = visualizerCopy.GetComponent<RectTransform>();
              
                visualizerRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                visualizerRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                visualizerRectTransform.pivot = new Vector2(0.5f, 0.5f);
                visualizerRectTransform.anchoredPosition = Vector2.zero;

                var zoom = PlayerPrefs.GetFloat("ZOOM");
                
                var currentSize = captureCanvas.GetComponent<RectTransform>().sizeDelta;
                var height = currentSize.y + visualizerRectTransform.rect.size.y;
                var widthOffset = 100f;
                
                currentSize = new Vector2(currentSize.x - widthOffset, height) * zoom;
                visualizerRectTransform.sizeDelta = currentSize;
                
                visualizerCopy.Setup(i, currentResults, false);
                visualizerCopy.gameObject.SetActive(true);
                
                await visualizerCopy.AwaitParameters();
                
                currentSize = visualizerRectTransform.rect.size * zoom;
                var sizeX = (int)Math.Round(currentSize.x);
                var sizeY = (int)Math.Round(currentSize.y);

                Debug.Log($"Size: {sizeX} Size: {sizeY}");
               
                var rt = new RenderTexture(sizeX, sizeY, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D32_SFloat_S8_UInt);
                captureCamera.targetTexture = rt;

                await UniTask.Yield();

                var screenshot = new Texture2D(sizeX, sizeY, TextureFormat.ARGB32, false, false);
                var tmp = RenderTexture.active;
                RenderTexture.active = rt;

                screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                
                var pixels = screenshot.GetPixels();
            
                for (var j = 0; j < pixels.Length; j++)
                    pixels[j] = pixels[j].gamma;
            
                screenshot.SetPixels(pixels);
                screenshot.Apply();

                RenderTexture.active = tmp;

                var bytes = screenshot.EncodeToPNG();
                var path = DirectoryPath.GetBraydenCycleVisualizerPathByIndex(i);

                if (File.Exists(path))
                    File.Delete(path);

                await MightyFileHandler.WriteBytesFileAsync(path, bytes);

                captureCamera.targetTexture = null;

                Destroy(screenshot);
                Destroy(tmp);
                Destroy(rt);
                Destroy(visualizerCopy.gameObject);
                
                await UniTask.Yield(PlayerLoopTiming.LastPostLateUpdate);
                captureCamera.gameObject.SetActive(false);
                captureCanvas.gameObject.SetActive(false);
            } 
           
        }

        private void ResetCompressionPoints()
        {
            foreach (var point in _currentCompressionPoints)
                Destroy(point.gameObject);

            _currentCompressionPoints.Clear();
            _currentCompressionPoints = new List<CompressionPointVisualizer>();
        }

        private void ResetDecompressionPoints()
        {
            foreach (var point in _currentDecompressionPoints)
                Destroy(point.gameObject);

            _currentDecompressionPoints.Clear();
            _currentDecompressionPoints = new List<DecompressionPointVisualizer>();
        }

        public void ShowTime(TimeSpan timeSpan)
        {
            var mainTimeValue = $"{(int)timeSpan.TotalMinutes:D1}'{timeSpan.Seconds:D2}\"";
            var millisecondsTimeValue = $"{timeSpan.Milliseconds:D2}";

            var formattedTime = string.Concat(mainTimeValue, millisecondsTimeValue);

            for (var i = 0; i < totalTimeTexts.Count; i++)
            {
                totalTimeTexts[i].text = formattedTime[i].ToString();
            }
        }

        // Random rnd = new Random();
        //
        // private void Update()
        // {
        //     if (Input.GetKey(KeyCode.G))
        //     {
        //
        //         var randomDouble = (float)rnd.NextDouble();
        //         var randomInt = rnd.Next(0, 6);
        //         var resultDepth = randomInt + randomDouble;
        //         var depthStatus = GetCompressionStatus(resultDepth);
        //
        //         var testModel = new CompressionModel(0, resultDepth, depthStatus, HandsPosition.Center, null);
        //
        //         var randomFr = rnd.Next(0, 150);
        //         var frStatus = GetFrequencyType(randomFr);
        //         var frequency = new Frequency(randomFr, frStatus);
        //         testModel.UpdateFrequency(frequency);
        //
        //         VisualizeCompressionPointOnGraph(testModel);
        //     }
        //
        //     if (Input.GetKeyUp(KeyCode.C))
        //     {
        //         ResetUI();
        //     }
        //
        //     if (Input.GetKeyUp(KeyCode.S))
        //     {
        //         CaptureCycleScreenshot(indexx).Forget();
        //         indexx++;
        //     }
        // }
        //
        // private int indexx = 0;

        [SerializeField] private Camera captureCamera;
        [SerializeField] private Transform captureCanvas;

        public async UniTask CaptureCycleScreenshot(int index)
        {
            if (CompressionPointVisualizer.PrevPoint != null)
                CompressionPointVisualizer.PrevPoint.DeactivatePointAnimation();

            //var zoom = PlayerPrefs.GetFloat("ZOOM");
            //var currentSize = compressionsGraphImage.rect.size * zoom;
            
            var currentSize = compressionsGraphImage.rect.size;
            
            captureCamera.gameObject.SetActive(true);
            captureCanvas.gameObject.SetActive(true);

            var graphCopy = Instantiate(compressionsGraphImage, captureCanvas.transform, false);

            graphCopy.anchorMin = new Vector2(0.5f, 0.5f);
            graphCopy.anchorMax = new Vector2(0.5f, 0.5f);
            graphCopy.pivot = new Vector2(0.5f, 0.5f);

            graphCopy.anchoredPosition = Vector2.zero;
            graphCopy.GetComponent<Image>().color = new Color(1, 1, 1, 1);
            graphCopy.sizeDelta = currentSize;

            await UniTask.Yield();

            var sizeX = (int)Math.Round(currentSize.x);
            var sizeY = (int)Math.Round(currentSize.y);

            GraphSizeX = sizeX;
            GraphSizeY = sizeY;

            var rt = new RenderTexture(sizeX, sizeY, GraphicsFormat.R8G8B8A8_UNorm, GraphicsFormat.D32_SFloat_S8_UInt);

            captureCamera.targetTexture = rt;

            await UniTask.Yield();

            var screenshot = new Texture2D(sizeX, sizeY, TextureFormat.ARGB32, false, false);
            var tmp = RenderTexture.active;
            RenderTexture.active = rt;

            screenshot.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            var pixels = screenshot.GetPixels();
            
            for (var j = 0; j < pixels.Length; j++)
                pixels[j] = pixels[j].gamma;
            
            screenshot.SetPixels(pixels);
            screenshot.Apply();

            RenderTexture.active = tmp;

            var bytes = screenshot.EncodeToPNG();
            var path = DirectoryPath.GetBraydenScreenPathByIndex(index);

            if (File.Exists(path))
                File.Delete(path);

            await MightyFileHandler.WriteBytesFileAsync(path, bytes);

            captureCamera.targetTexture = null;

            Destroy(screenshot);
            Destroy(tmp);
            Destroy(rt);
            Destroy(graphCopy.gameObject);

            captureCamera.gameObject.SetActive(false);
            captureCanvas.gameObject.SetActive(false);
        }

    }
}
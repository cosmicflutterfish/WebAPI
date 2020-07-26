﻿using ClassTranscribeDatabase;
using ClassTranscribeDatabase.Models;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Translation;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClassTranscribeDatabase.CommonUtils;
using CTCommons.Grpc;

namespace CTCommons.MSTranscription
{
    public class MSTranscriptionService
    {
        private readonly ILogger _logger;
        private readonly SlackLogger _slackLogger;
        private readonly RpcClient _rpcClient;

        public class MSTResult
        {
            public Dictionary<string, List<Caption>> Captions { get; set; }
            public string ErrorCode { get; set; }
            public TimeSpan LastSuccessTime { get; set; }
        }

        public MSTranscriptionService(ILogger<MSTranscriptionService> logger, SlackLogger slackLogger, RpcClient rpcClient)
        {
            _logger = logger;
            _slackLogger = slackLogger;
            _rpcClient = rpcClient;
        }

        public async Task<MSTResult> RecognitionWithVideoStreamAsync(FileRecord videoFile, Key key, Dictionary<string, List<Caption>> captions, TimeSpan offset)
        {
            return await RecognitionWithVideoStreamAsync(videoFile.VMPath, key, captions, offset);
        }

        public async Task<MSTResult> RecognitionWithVideoStreamAsync(string filePath, Key key, Dictionary<string, List<Caption>> captions, TimeSpan offset)
        {   
            _logger.LogInformation($"Trimming video file with offset {offset.TotalSeconds} seconds");
            var trimmedAudioFile = await _rpcClient.PythonServerClient.ConvertVideoToWavRPCWithOffsetAsync(new CTGrpc.FileForConversion
            {
                File = new CTGrpc.File { FilePath = filePath },
                Offset = (float)offset.TotalSeconds
            });

            var filerecord = new FileRecord { Path = trimmedAudioFile.FilePath };
            string file = filerecord.Path;

            AppSettings _appSettings = Globals.appSettings;

            SpeechTranslationConfig _speechConfig = SpeechTranslationConfig.FromSubscription(key.ApiKey, key.Region);
            _speechConfig.RequestWordLevelTimestamps();
            // Sets source and target languages.
            _speechConfig.SpeechRecognitionLanguage = Languages.ENGLISH;
            _speechConfig.AddTargetLanguage(Languages.SIMPLIFIED_CHINESE);
            _speechConfig.AddTargetLanguage(Languages.KOREAN);
            _speechConfig.AddTargetLanguage(Languages.SPANISH);
            _speechConfig.AddTargetLanguage(Languages.FRENCH);
            _speechConfig.OutputFormat = OutputFormat.Detailed;

            TimeSpan lastSuccessfulTime = TimeSpan.Zero;
            string errorCode = "";
            Console.OutputEncoding = Encoding.Unicode;
            
            var stopRecognition = new TaskCompletionSource<int>();
            // Create an audio stream from a wav file.
            // Replace with your own audio file name.
            using (var audioInput = WavHelper.OpenWavFile(file))
            {
                // Creates a speech recognizer using audio stream input.
                using (var recognizer = new TranslationRecognizer(_speechConfig, audioInput))
                {
                    recognizer.Recognized += (s, e) =>
                    {
                        if (e.Result.Reason == ResultReason.TranslatedSpeech)
                        {
                            JObject jObject = JObject.Parse(e.Result.Properties.GetProperty(PropertyId.SpeechServiceResponse_JsonResult));
                            var wordLevelCaptions = jObject["Words"]
                            .ToObject<List<MSTWord>>()
                            .OrderBy(w => w.Offset)
                            .ToList();

                            if (e.Result.Text == "" && wordLevelCaptions.Count == 0)
                            {
                                TimeSpan _offset = new TimeSpan(e.Result.OffsetInTicks);
                                _logger.LogInformation($"Begin={_offset.Minutes}:{_offset.Seconds},{_offset.Milliseconds}", _offset);
                                _logger.LogInformation("Empty String");
                                TimeSpan _end = e.Result.Duration.Add(_offset);
                                _logger.LogInformation($"End={_end.Minutes}:{_end.Seconds},{_end.Milliseconds}");
                                return;
                            }
                            
                            if (wordLevelCaptions.Any())
                            {
                                var offsetDifference = e.Result.OffsetInTicks - wordLevelCaptions.FirstOrDefault().Offset;
                                wordLevelCaptions.ForEach(w => w.Offset += offsetDifference);
                            }

                            var sentenceLevelCaptions = MSTWord.WordLevelTimingsToSentenceLevelTimings(e.Result.Text, wordLevelCaptions);

                            TimeSpan offset = new TimeSpan(e.Result.OffsetInTicks);
                            _logger.LogInformation($"Begin={offset.Minutes}:{offset.Seconds},{offset.Milliseconds}", offset);
                            TimeSpan end = e.Result.Duration.Add(offset);
                            _logger.LogInformation($"End={end.Minutes}:{end.Seconds},{end.Milliseconds}");
                            var newCaptions = MSTWord.AppendCaptions(captions[Languages.ENGLISH].Count, sentenceLevelCaptions);
                            // Add offset here.
                            captions[Languages.ENGLISH].AddRange(newCaptions);

                            foreach (var element in e.Result.Translations)
                            {
                                newCaptions = Caption.AppendCaptions(captions[element.Key].Count, offset, end, element.Value);
                                captions[element.Key].AddRange(newCaptions);
                            }
                        }
                        else if (e.Result.Reason == ResultReason.NoMatch)
                        {
                            _logger.LogInformation($"NOMATCH: Speech could not be recognized.");
                        }
                    };

                    recognizer.Canceled += (s, e) =>
                    {
                        errorCode = e.ErrorCode.ToString();
                        _logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode} Reason={e.Reason}");

                        if (e.Reason == CancellationReason.Error)
                        {
                            _logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");

                            if (e.ErrorCode == CancellationErrorCode.ServiceTimeout
                            || e.ErrorCode == CancellationErrorCode.ServiceUnavailable
                            || e.ErrorCode == CancellationErrorCode.ConnectionFailure)
                            {
                                TimeSpan lastTime = TimeSpan.Zero;
                                if (captions.Count != 0)
                                {
                                    var lastCaption = captions[Languages.ENGLISH].OrderBy(c => c.End).TakeLast(1).ToList().First();
                                    lastTime = lastCaption.End;
                                }

                                _logger.LogInformation($"Retrying, LastSuccessTime={lastTime.ToString()}");
                                lastSuccessfulTime = lastTime;
                            } else if (e.ErrorCode != CancellationErrorCode.NoError)
                            {
                                _logger.LogInformation($"CANCELED: ErrorCode={e.ErrorCode.ToString()} Reason={e.Reason}");
                                _slackLogger.PostErrorAsync(new Exception($"Transcription Failure"),
                                    $"Transcription Failure").GetAwaiter().GetResult();
                            }
                        }

                        stopRecognition.TrySetResult(0);
                    };

                    recognizer.SessionStarted += (s, e) =>
                    {
                        _logger.LogInformation("\nSession started event.");
                    };

                    recognizer.SessionStopped += (s, e) =>
                    {
                        _logger.LogInformation("\nSession stopped event.");
                        _logger.LogInformation("\nStop recognition.");
                        stopRecognition.TrySetResult(0);
                    };

                    // Starts continuous recognition. Uses StopContinuousRecognitionAsync() to stop recognition.
                    await recognizer.StartContinuousRecognitionAsync().ConfigureAwait(false);

                    // Waits for completion.
                    // Use Task.WaitAny to keep the task rooted.
                    Task.WaitAny(new[] { stopRecognition.Task });

                    // Stops recognition.
                    await recognizer.StopContinuousRecognitionAsync().ConfigureAwait(false);

                    return new MSTResult {
                        Captions = captions,
                        ErrorCode = errorCode,
                        LastSuccessTime = lastSuccessfulTime
                    };
                }
            }
            // </recognitionAudioStream>
        }
    }
}

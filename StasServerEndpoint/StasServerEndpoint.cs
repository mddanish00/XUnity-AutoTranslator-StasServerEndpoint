using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using StasServer.SimpleJSON;
using XUnity.AutoTranslator.Plugin.Core.Endpoints;
using XUnity.AutoTranslator.Plugin.Core.Endpoints.Http;
using XUnity.AutoTranslator.Plugin.Core.Web;
using XUnity.Common.Logging;

namespace StasServer
{
    public class StasServerEndpoint : HttpEndpoint, ITranslateEndpoint, IDisposable
    {
        public override string Id => "StasServer";

        public override string FriendlyName => "stas-server";

        // stas-server is using asynchronous web server (bottle.py with tornado)
        // So, no actual limit. I just use 50 because that what most of Endpoints use if not 1.
        public override int MaxConcurrency => 50;

        int MaxTranslationsPerRequestInSetting { get; set; } = 10;

        public override int MaxTranslationsPerRequest => MaxTranslationsPerRequestInSetting;

        private Process process;
        private bool isDisposing = false;
        private bool isStarted = false;
        private bool isReady = false;

        private string ServerPort { get; set; }

        private bool EnableCuda { get; set; }

        private bool EnableShortDelay { get; set; }

        private bool DisableSpamChecks { get; set; }

        private bool LogServerMessages { get; set; }

        private string ModelsFolderPath { get; set; }

        private string StasServerExePath { get; set; }

        private bool DisableCache { get; set; }

        private bool EnablePreventRetranslation { get; set; }

        private string PlayerJPName { get; set; }

        private string PlayerTranslatedName { get; set; }

        public override void Initialize(IInitializationContext context)
        {
            if (context.SourceLanguage != "ja") throw new Exception("Only ja is supported as source language");
            if (context.DestinationLanguage != "en") throw new Exception("Only en is supported as destination language");

            // Exclusive StasServerEndpoint settings
            this.StasServerExePath = context.GetOrCreateSetting("StasServer", "StasServerExePath", "");
            this.ModelsFolderPath = context.GetOrCreateSetting("StasServer", "ModelsFolderPath", "");
            this.DisableCache = context.GetOrCreateSetting("StasServer", "DisableCache", false);
            this.EnablePreventRetranslation = context.GetOrCreateSetting("StasServer", "EnablePreventRetranslation", false);
            this.PlayerJPName = context.GetOrCreateSetting("StasServer", "PlayerJPName", "プレーヤー");
            this.PlayerTranslatedName = context.GetOrCreateSetting("StasServer", "PlayerTranslatedName", "Player");
            // Inherit from SugoiOfflineTranslator
            this.ServerPort = context.GetOrCreateSetting("StasServer", "ServerPort", "14367");
            this.EnableCuda = context.GetOrCreateSetting("StasServer", "EnableCuda", false);
            this.MaxTranslationsPerRequestInSetting = context.GetOrCreateSetting("StasServer", "MaxBatchSize", 10);
            this.EnableShortDelay = context.GetOrCreateSetting("StasServer", "EnableShortDelay", false);
            this.DisableSpamChecks = context.GetOrCreateSetting("StasServer", "DisableSpamChecks", true);
            this.LogServerMessages = context.GetOrCreateSetting("StasServer", "LogServerMessages", false);

            if (this.EnableShortDelay)
            {
                context.SetTranslationDelay(0.1f);
            }

            if (this.DisableSpamChecks)
            {
                context.DisableSpamChecks();
            }

            if (!string.IsNullOrEmpty(this.StasServerExePath) && !string.IsNullOrEmpty(this.ModelsFolderPath))
            {
                this.SetupServer(context);
            }
            else
            {
                XuaLogger.AutoTranslator.Info($"Either StasServerExePath or CT2FolderPath or both are not specified. Please refer to README for proper instruction to set up this Endpoint.");
            }
        }

        private void SetupServer(IInitializationContext context)
        {
            if (!File.Exists(this.StasServerExePath))
            {
                throw new Exception("stas-server.exe not found on the specified path.");
            }

            if (!Directory.Exists(this.ModelsFolderPath))
            {
                throw new Exception("models folder not found on the specified path.");
            }

            var configuredEndpoint = context.GetOrCreateSetting<string>("Service", "Endpoint");
            if (configuredEndpoint == this.Id)
            {
                this.StartProcess();
            }

        }

        public void Dispose()
        {
            this.isDisposing = true;
            if (this.process != null)
            {
                this.process.Kill();
                this.process.Dispose();
                this.process = null;
            }
        }

        private void StartProcess()
        {
            if (this.process == null || this.process.HasExited)
            {

                XuaLogger.AutoTranslator.Info($"Running stas-server:\n\tExecPath: {this.StasServerExePath}");

                List<string> argumentsList = new List<string>()
                {
                    this.ServerPort,
                    "--models_dir",
                    $"\"{this.ModelsFolderPath}\""
                };
                if (this.EnableCuda)
                {
                    argumentsList.Add("--cuda");
                }
                if (this.DisableCache)
                {
                    argumentsList.Add("--no-cache");
                }

                this.process = new Process();
                this.process.StartInfo = new ProcessStartInfo()
                {
                    FileName = this.StasServerExePath,
                    Arguments = string.Join(" ", argumentsList.ToArray()),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                };

                this.process.OutputDataReceived += this.ServerDataReceivedEventHandler;
                this.process.ErrorDataReceived += this.ServerDataReceivedEventHandler;

                this.process.Start();
                this.process.BeginErrorReadLine();
                this.process.BeginOutputReadLine();
                this.isStarted = true;


            }
        }

        void ServerDataReceivedEventHandler(object sender, DataReceivedEventArgs args)
        {
            if (this.LogServerMessages)
            {
                XuaLogger.AutoTranslator.Info(args.Data);
            }

            if (!this.isReady && args.Data.Contains("| INFO | Server | Listening on"))
            {
                this.isReady = true;
                XuaLogger.AutoTranslator.Info("stas-server is successfully launched and ready to use!");
            }
        }

        IEnumerator ITranslateEndpoint.Translate(ITranslationContext context)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var iterator = base.Translate(context);

            while (iterator.MoveNext()) yield return iterator.Current;

            var elapsed = stopwatch.Elapsed.TotalSeconds;

            if (this.LogServerMessages)
            {
                XuaLogger.AutoTranslator.Info($"Translate complete {elapsed}s");
            }
        }

        public override IEnumerator OnBeforeTranslate(IHttpTranslationContext context)
        {
            if (this.isStarted && this.process.HasExited)
            {
                this.isStarted = false;
                this.isReady = false;

                XuaLogger.AutoTranslator.Warn($"Translator server process exited unexpectedly [status {process.ExitCode}]");
            }

            if (!this.isStarted && !this.isDisposing)
            {
                XuaLogger.AutoTranslator.Warn($"Translator server process not running. Starting...");
                this.StartProcess();
            }

            while (!this.isReady) yield return null;
        }

        public string GetUrlEndpoint()
        {
            return $"http://127.0.0.1:{this.ServerPort}/";
        }

        private readonly Regex innerJpnRegex = new Regex(@"([\p{IsCJKUnifiedIdeographs}\p{IsCJKSymbolsandPunctuation}\p{IsHiragana}\p{IsKatakana}]+)");

        public string CheckIfAlreadyTranslated(string rawText)
        {
            string substitutedText = rawText.Replace(this.PlayerJPName, this.PlayerTranslatedName);
            if (innerJpnRegex.IsMatch(substitutedText))
            {
                return rawText;
            }
            else
            {
                return substitutedText;
            }
        }

        public override void OnCreateRequest(IHttpRequestCreationContext context)
        {
            var json = new JSONObject();

            if (this.MaxTranslationsPerRequestInSetting != 1)
            {
                if (this.EnablePreventRetranslation)
                {
                    json["batch"] = context.UntranslatedTexts.Select(txt => this.CheckIfAlreadyTranslated(txt)).ToArray();
                }
                else
                {
                    json["batch"] = context.UntranslatedTexts;
                }
                json["message"] = "translate batch";
            }
            else
            {
                if (this.EnablePreventRetranslation)
                {
                    json["content"] = this.CheckIfAlreadyTranslated(context.UntranslatedText);
                }
                else
                {
                    json["content"] = context.UntranslatedText;
                }
                json["message"] = "translate sentences";
            }

            var data = json.ToString();

            var request = new XUnityWebRequest("POST", this.GetUrlEndpoint(), data);
            request.Headers["Content-Type"] = "application/json";
            request.Headers["Accept"] = "*/*";

            context.Complete(request);
        }

        public override void OnExtractTranslation(IHttpTranslationExtractionContext context)
        {
            var data = context.Response.Data;
            var result = JSON.Parse(data);

            if (this.MaxTranslationsPerRequestInSetting != 1)
            {
                context.Complete(result.AsStringList.ToArray());
            }
            else
            {
                if (result.IsString)
                {
                    context.Complete(result.Value);
                }
                else
                {
                    context.Fail($"Unexpected return from server: {data}");
                }
            }
        }
    }
}

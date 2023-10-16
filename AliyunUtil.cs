using Aliyun.Acs.alimt.Model.V20181012;
using Aliyun.Acs.Core;
using Aliyun.Acs.Core.Profile;
using Amazon.Runtime.Internal.Util;
using Hungsum.Sys.Utility;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Hungsum.MyAliyun
{
    public class AliyunUtil
    {
        public readonly string AccessKey;

        public readonly string Secret;

        public readonly string AppKey;

        public AliyunUtil(string accessKey, string secret , string appKey)
        {
            this.AccessKey = accessKey;
            this.Secret = secret;
            this.AppKey = appKey;
        }

        #region Token

        private string _token;

        private string token
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this._token))
                {
                    this._token = this._getAliyunToken();
                }

                return this._token;
            }
        }

        #endregion

        private static HttpClient httpClient = new HttpClient();


        private string _getAliyunToken(string regionId = "cn-shanghai")
        {
            DefaultProfile defaultProfile = DefaultProfile.GetProfile(regionId, this.AccessKey, this.Secret);

            DefaultAcsClient client = new DefaultAcsClient(defaultProfile);

            CommonRequest request = new CommonRequest();

            request.Domain = "nls-meta.cn-shanghai.aliyuncs.com";
            request.RegionId = "cn-shanghai";
            request.Version = "2019-02-28";

            request.Action = "CreateToken";

            CommonResponse resp = client.GetCommonResponse(request);

            XElement xResp = XElement.Parse(JsonConvert.DeserializeXmlNode(resp.Data, "Data").OuterXml);

            string errMsg = xResp.Element("ErrMsg")?.Value;

            if (errMsg.IsNullOrWhiteSpace())
            {
                return xResp.Element("Token")?.Element("Id")?.Value;
            }
            else
            {
                throw new HsException(errMsg);
            }
        }


        #region 语音合成

        public static class Voice
        {
            public static List<string> SPEAKERS = new List<string>() { "Xiaoyun", "Xiaogang", "Ruoxi", "Siqi", "Sijia", "Sicheng", "Aiqi", "Aijia", "Aicheng", "Aida", "Ninger", "Ruilin", "Siyue", "Aiya", "Aixia", "Aimei", "Aiyu", "Aiyue", "Aijing", "Xiaomei", "Aina", "Yina", "Sijing", "Sitong", "Xiaobei", "Aitong", "Aiwei", "Aibao", "Harry", "Abby", "Andy", "Eric", "Emily", "Luna", "Luca", "Wendy", "William", "Olivia", "Shanshan", "Xiaoyue", "Lydia", "Aishuo" ,"donna" , "luca" , "zhiyuan" , "zhida"};

            public static string Default
            {
                get => SPEAKERS[0];
            }

            public static string Parse(string speaker)
            {
                return SPEAKERS.Contains(speaker) ? speaker : Default;

            }

        }

        public static class Format
        {
            public const string Pcm = "pcm";

            public const string Wav = "wav";

            public const string Mp3 = "mp3";
        }

        /// <summary>
        /// 语音合成数据请求
        /// </summary>
        public class YyhcReqest
        {
            public readonly string Text;

            public readonly string Format;

            public readonly string Voice;

            public int SampleRate;

            public int Volumn;

            public int SpeechRate;

            public int PitchRate;

            public YyhcReqest(string text, string voice = "", string format = AliyunUtil.Format.Mp3, int sampleRate = 16000, int volumn = 50, int speechRate = 0, int pitchRate = 0)
            {
                this.Text = text;
                this.Voice = voice;
                this.Format = format;
                this.SampleRate = sampleRate;
                this.Volumn = volumn;
                this.SpeechRate = speechRate;
                this.PitchRate = pitchRate;
            }

            public override string ToString()
            {
                return HsMD5.EncryptionMD5($"{Text}_{Voice}_{Format}_{SampleRate}_{Volumn}_{SpeechRate}_{PitchRate}");
            }

        }

        public async Task<byte[]> GetYyhcResultAsync(YyhcReqest req)
        {
            JObject obj = new JObject();
            obj["appkey"] = AppKey;
            obj["token"] = token;
            obj["text"] = req.Text;
            obj["format"] = req.Format;
            obj["voice"] = req.Voice;
            obj["sample_rate"] = req.SampleRate;
            obj["volumn"] = req.Volumn;
            obj["speech_rate"] = req.SpeechRate;
            obj["pitch_rate"] = req.PitchRate;

            StringContent content = new StringContent(obj.ToString(), Encoding.UTF8, "application/json");

            HttpResponseMessage httpResponseMessage = await httpClient.PostAsync("https://nls-gateway.cn-shanghai.aliyuncs.com/stream/v1/tts", content);

            string contentType = null;

            if (httpResponseMessage.IsSuccessStatusCode)
            {
                string[] typesArray = httpResponseMessage.Content.Headers.GetValues("Content-Type").ToArray();

                if (typesArray.Length > 0)
                {
                    contentType = typesArray.First();
                }
            }

            if ("audio/mpeg".Equals(contentType))
            {
                byte[] bs = await httpResponseMessage.Content.ReadAsByteArrayAsync();

                return bs;

            }
            else
            {
                string text = await httpResponseMessage.Content.ReadAsStringAsync();

                throw new HsException($"{httpResponseMessage.StatusCode} {httpResponseMessage.ReasonPhrase} {text}");

            }


        }

        #endregion

        #region 机器翻译

        public string GetTranslate(string text, string region = "cn-hangzhou",string sourceLanguage = "en" , string targetLanguage = "zh")
        {
            IClientProfile profile = DefaultProfile.GetProfile(region, this.AccessKey, this.Secret);

            DefaultAcsClient client = new DefaultAcsClient(profile);

            var request = new TranslateGeneralRequest()
            {
                ActionName = "TranslateGeneral",
                FormatType = "text",
                Scene = "general",
                SourceLanguage = sourceLanguage,
                SourceText = text,
                TargetLanguage = targetLanguage,
            };

            TranslateGeneralResponse response = client.GetAcsResponse(request);

            if (response.Code.HasValue)
            {
                if (response.Code.Value != 200)
                {
                    throw new HsException($"翻译发生错误 {response.Message}");
                }
                else
                {
                    return response.Data.Translated;
                }
            }
            else
            {
                throw new HsException($"翻译发生错误 {response.Message}");
            }

        }

        #endregion


    }

}

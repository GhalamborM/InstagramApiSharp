﻿/*
 * Developer: Ramtin Jokar [ Ramtinak@live.com ] [ RamtinJokar@outlook.com ]
 * 
 * Github source: https://github.com/ramtinak/InstagramApiSharp
 * Nuget package: https://www.nuget.org/packages/InstagramApiSharp
 * 
 * IRANIAN DEVELOPERS
 */

using System;
using System.Collections.Generic;
using System.Linq;
using InstagramApiSharp.Classes.Android.DeviceInfo;
using InstagramApiSharp.API;
using InstagramApiSharp.Classes.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using InstagramApiSharp.Enums;
using InstagramApiSharp.API.Versions;
using InstagramApiSharp.Helpers;
using System.Text;
using InstagramApiSharp.Classes;
using System.Net.Http;

#if NETSTANDARD || WINDOWS_UWP || NET461_OR_GREATER
using Org.BouncyCastle.Cms;
using Org.BouncyCastle.Security;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
#elif NET || NETCOREAPP3_1
using System.Security.Cryptography;
#endif

namespace InstagramApiSharp
{
    internal static class ExtensionHelper
    {
        public static void SetCsrfTokenIfAvailable(this UserSessionData data, HttpResponseMessage response,
            IHttpRequestProcessor _httpRequestProcessor, bool dontCheck = false)
        {
            if (response.IsSuccessStatusCode)
            {
                var cookies =
                    _httpRequestProcessor.HttpHandler.CookieContainer.GetCookies(_httpRequestProcessor.Client
                    .BaseAddress);

                var csrfToken = cookies[InstaApiConstants.CSRFTOKEN]?.Value ?? string.Empty;
                if (dontCheck && !string.IsNullOrEmpty(csrfToken))
                    data.CsrfToken = csrfToken;
                else if (!string.IsNullOrEmpty(csrfToken) && string.IsNullOrEmpty(data.CsrfToken))
                    data.CsrfToken = csrfToken;
            }
        }
        public static string GenerateUserAgent(this AndroidDevice deviceInfo, InstaApiVersion apiVersion)
        {
            if (deviceInfo == null)
                return InstaApiConstants.USER_AGENT_DEFAULT;
            if (deviceInfo.AndroidVer == null)
                deviceInfo.AndroidVer = AndroidVersion.GetRandomAndriodVersion();

            return string.Format(InstaApiConstants.USER_AGENT, deviceInfo.Dpi, deviceInfo.Resolution, deviceInfo.HardwareManufacturer,
                deviceInfo.DeviceModelIdentifier, deviceInfo.FirmwareBrand, deviceInfo.HardwareModel,
                apiVersion.AppVersion, deviceInfo.AndroidVer.APILevel,
                deviceInfo.AndroidVer.VersionNumber, apiVersion.AppApiVersionCode);
        }
        public static string GenerateFacebookUserAgent()
        {
            var deviceInfo = AndroidDeviceGenerator.GetRandomAndroidDevice();
            //Mozilla/5.0 (Linux; Android 7.0; PRA-LA1 Build/HONORPRA-LA1; wv) AppleWebKit/537.36 (KHTML, like Gecko) Version/4.0 Chrome/69.0.3497.100 Mobile Safari/537.36

            return string.Format(InstaApiConstants.FACEBOOK_USER_AGENT,
              deviceInfo.AndroidVer.VersionNumber,deviceInfo.DeviceModelIdentifier,
              $"{deviceInfo.AndroidBoardName}{deviceInfo.DeviceModel}");
        }
        public static bool IsEmpty(this string content)
        {
            return string.IsNullOrEmpty(content);
        }
        public static bool IsNotEmpty(this string content)
        {
            return !string.IsNullOrEmpty(content);
        }
        public static string EncodeList(this long[] listOfValues, bool appendQuotation = true)
        {
            return EncodeList(listOfValues.ToList(), appendQuotation);
        }
        public static string EncodeList(this string[] listOfValues, bool appendQuotation = true)
        {
            return EncodeList(listOfValues.ToList(), appendQuotation);
        }
        public static string EncodeList(this List<long> listOfValues, bool appendQuotation = true)
        {
            if (!appendQuotation)
                return string.Join(",", listOfValues);
            var list = new List<string>();
            foreach (var item in listOfValues)
                list.Add(item.Encode());
            return string.Join(",", list);
        }
        public static string EncodeList(this List<string> listOfValues, bool appendQuotation = true)
        {
            if (!appendQuotation)
                return string.Join(",", listOfValues);
            var list = new List<string>();
            foreach (var item in listOfValues)
                list.Add(item.Encode());
            return string.Join(",", list);
        }
        public static string Encode(this long content)
        {
            return content.ToString().Encode();
        }
        public static string Encode(this string content)
        {
            return "\"" + content + "\"";
        }

        public static string EncodeRecipients(this long[] recipients)
        {
            return EncodeRecipients(recipients.ToList());
        }
        public static string EncodeRecipients(this List<long> recipients)
        {
            var list = new List<string>();
            foreach (var item in recipients)
                list.Add($"[{item}]");
            return string.Join(",", list);
        }

        public static string EncodeUri(this string data)
        {
            return System.Net.WebUtility.UrlEncode(data);
        }

        public static string GetStoryToken()
        {
            var str = "";
            str += Rnd.Next(3, 4);
            str += Rnd.Next(1, 9);
            str += Rnd.Next(0, 9);
            str += Rnd.Next(0, 9);
            str += Rnd.Next(0, 9);
            str += Rnd.Next(0, 9);
            str += Rnd.Next(11111, 99999);
            str += Rnd.Next(2222, 6789);
            return str;
        }
        public static string GetThreadToken()
        {
            var str = "";
            str += Rnd.Next(11, 99);
            str += Rnd.Next(0, 9);
            str += Rnd.Next(2, 9);
            str += Rnd.Next(1000, 9999);
            str += Rnd.Next(1111, 8898);

            str += Rnd.Next(2222, 6789);
            str += Rnd.Next(5000, 8000);

            return $"{str}";
        }
        public static string GenerateJazoest(string guid)
        {
            int ix = 0;
            var chars = guid.ToCharArray();
            foreach (var ch in chars)
                ix += (int)ch;
            return "2" + ix;
        }

#if NETSTANDARD || WINDOWS_UWP || NET461_OR_GREATER
        static private readonly SecureRandom secureRandom = new SecureRandom();

        public static string GetEncryptedPassword(this IInstaApi api, string password, long? providedTime = null, uint version = 4) 
        {
            var pubKey = api.GetLoggedUser().PublicKey;
            var pubKeyId = api.GetLoggedUser().PublicKeyId;
            byte[] randKey = new byte[32];
            byte[] iv = new byte[12];
            secureRandom.NextBytes(randKey, 0, randKey.Length);
            secureRandom.NextBytes(iv, 0, iv.Length);
            long time = providedTime ?? DateTime.UtcNow.ToUnixTime();
            byte[] associatedData = Encoding.UTF8.GetBytes(time.ToString());
            var pubKEY = Encoding.UTF8.GetString(Convert.FromBase64String(pubKey));
            byte[] encryptedKey;
            using (var rdr = PemKeyUtils.GetRSAProviderFromPemString(pubKEY.Trim()))
                encryptedKey = rdr.Encrypt(randKey, false);

            byte[] plaintext = Encoding.UTF8.GetBytes(password);

            var cipher = new GcmBlockCipher(new AesEngine());
            var parameters = new AeadParameters(new KeyParameter(randKey), 128, iv, associatedData);
            cipher.Init(true, parameters);

            var ciphertext = new byte[cipher.GetOutputSize(plaintext.Length)];
            var len = cipher.ProcessBytes(plaintext, 0, plaintext.Length, ciphertext, 0);
            cipher.DoFinal(ciphertext, len);

            var con = new byte[plaintext.Length];
            for (int i = 0; i < plaintext.Length; i++)
                con[i] = ciphertext[i];
            ciphertext = con;
            var tag = cipher.GetMac();

            byte[] buffersSize = BitConverter.GetBytes(Convert.ToInt16(encryptedKey.Length));
            byte[] encKeyIdBytes = BitConverter.GetBytes(Convert.ToUInt16(pubKeyId));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(encKeyIdBytes);
            encKeyIdBytes[0] = 1; 
            var payload = Convert
                .ToBase64String(encKeyIdBytes
                .Concat(iv)
                .Concat(buffersSize)
                .Concat(encryptedKey)
                .Concat(tag)
                .Concat(ciphertext)
                .ToArray());

            return $"#PWD_INSTAGRAM:{version}:{time}:{payload}";
        }
#elif NET || NETCOREAPP3_1
        private readonly static RandomNumberGenerator RnGenerator = RandomNumberGenerator.Create();
        private readonly static RSACryptoServiceProvider RsaProvider = new ();

        public static string GetEncryptedPassword(this IInstaApi api, string password, long? providedTime = null, int version = 4)
        {
            var pubKey = api.GetLoggedUser().PublicKey;
            var pubKeyId = api.GetLoggedUser().PublicKeyId;
            byte[] randKey = new byte[32];
            byte[] iv = new byte[12];
            RnGenerator.GetBytes(randKey);
            RnGenerator.GetBytes(iv);

            long time = providedTime ?? DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            byte[] pubKEY = Convert.FromBase64String(pubKey);
            string decodedPubKey = Encoding.UTF8.GetString(pubKEY, 0, pubKEY.Length);
            decodedPubKey = decodedPubKey.Replace("-----BEGIN PUBLIC KEY-----", "").Replace("-----END PUBLIC KEY-----", "");
            byte[] publicKeyBytes = Convert.FromBase64String(decodedPubKey.Trim());
            RsaProvider.ImportSubjectPublicKeyInfo(publicKeyBytes, out _);
            byte[] encryptedKey = RsaProvider.Encrypt(randKey, false);

            var plaintext = Encoding.UTF8.GetBytes(password);

            byte[] ciphertext = new byte[plaintext.Length];
            byte[] tag = new byte[16];
            byte[] associatedData = Encoding.UTF8.GetBytes(time.ToString());
            AesGcm aesGsm = new (randKey);
            aesGsm.Encrypt(iv, plaintext, ciphertext, tag, associatedData);

            byte[] encKeyIdBytes = BitConverter.GetBytes(Convert.ToUInt16(pubKeyId));
            if (BitConverter.IsLittleEndian)
                Array.Reverse(encKeyIdBytes);
            encKeyIdBytes[0] = 1;
            byte[] buffersSize = BitConverter.GetBytes(Convert.ToInt16(encryptedKey.Length));
            var payload = Convert
                .ToBase64String(encKeyIdBytes
                .Concat(iv)
                .Concat(buffersSize)
                .Concat(encryptedKey)
                .Concat(tag)
                .Concat(ciphertext)
                .ToArray());

            return $"#PWD_INSTAGRAM:{version}:{time}:{payload}";
        }

#endif
        public static string GetJson(this InstaLocationShort location)
        {
            if (location == null)
                return null;

            return new JObject
                            {
                                {"name", location.Address ?? string.Empty},
                                {"address", location.ExternalId ?? string.Empty},
                                {"lat", location.Lat},
                                {"lng", location.Lng},
                                {"external_source", location.ExternalSource ?? "facebook_places"},
                                {"facebook_places_id", location.ExternalId},
                            }.ToString(Formatting.None);
        }

        public static InstaTVChannelType GetChannelType(this string type)
        {
            if(string.IsNullOrEmpty(type))
                return InstaTVChannelType.User;
            switch (type.ToLower())
            {
                case "chrono_following":
                    return InstaTVChannelType.ChronoFollowing;
                case "continue_watching":
                    return InstaTVChannelType.ContinueWatching;
                case "for_you":
                    return InstaTVChannelType.ForYou;
                case "popular":
                    return InstaTVChannelType.Popular;
                default:
                case "user":
                    return InstaTVChannelType.User;
            }
        }
        public static string GetRealChannelType(this InstaTVChannelType type)
        {
            switch(type)
            {
                case InstaTVChannelType.ChronoFollowing:
                    return "chrono_following";
                case InstaTVChannelType.ContinueWatching:
                    return "continue_watching";
                case InstaTVChannelType.Popular:
                    return "popular";
                case InstaTVChannelType.User:
                    return "user";
                case InstaTVChannelType.ForYou:
                default:
                    return "for_you";

            }
        }
        internal static readonly Random Rnd = new Random();
        public static string GenerateRandomString(this int length)
        {
            const string pool = "abcdefghijklmnopqrstuvwxyz0123456789";
            var chars = Enumerable.Range(0, length)
                .Select(x => pool[Rnd.Next(0, pool.Length)]);
            return new string(chars.ToArray());
        }

        public static string GenerateSnNonce(string emailOrPhoneNumber)
        {
            byte[] b = new byte[24];
            Rnd.NextBytes(b);
            var str = $"{emailOrPhoneNumber}|{DateTimeHelper.ToUnixTime(DateTime.UtcNow)}|{Encoding.UTF8.GetString(b)}";
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(str));
        }

        public static DateTime GenerateRandomBirthday()
        {
            int day = Rnd.Next(1, 29);
            int month = Rnd.Next(1, 12);
            int year = Rnd.Next(1979, 2000);
            return new DateTime(year, month, day);
        }
        public static void PrintInDebug(this object obj)
        {
            System.Diagnostics.Debug.WriteLine(Convert.ToString(obj));
        }

        public static string GetStoryLink(this InstaStoryLinkType linkType)
        {
            switch (linkType)
            {
                case InstaStoryLinkType.Link:
                    return "link";
                case InstaStoryLinkType.SwipeUp:
                    return "swipe up";
                case InstaStoryLinkType.Shopping:
                    return "shopping";
                case InstaStoryLinkType.IgTV:
                    return "igtv";
                default:
                case InstaStoryLinkType.Web:
                    return "web";
            }
        }
        public static InstaImageUpload ConvertToImageUpload(this InstaImage instaImage, InstaUserTagUpload[] userTags = null)
        {
            return new InstaImageUpload
            {
                Height = instaImage.Height,
                ImageBytes = instaImage.ImageBytes,
                Uri = instaImage.Uri,
                Width = instaImage.Width,
                UserTags = userTags?.ToList()
            };
        }

        public static JObject ConvertToJson(this InstaStoryPollUpload poll)
        {
            var jArray = new JArray
            {
                new JObject
                {
                    {"text", poll.Answer1},
                    {"count", 0},
                    {"font_size", poll.Answer1FontSize}
                },
                new JObject
                {
                    {"text", poll.Answer2},
                    {"count", 0},
                    {"font_size", poll.Answer2FontSize}
                },
            };

            return new JObject
            {
                {"x", poll.X},
                {"y", poll.Y},
                {"z", poll.Z},
                {"width", poll.Width},
                {"height", poll.Height},
                {"rotation", poll.Rotation},
                {"question", poll.Question},
                {"viewer_vote", 0},
                {"viewer_can_vote", true},
                {"tallies", jArray},
                {"is_shared_result", false},
                {"finished", false},
                {"is_sticker", poll.IsSticker},
            };
        }

        public static JObject ConvertToJson(this InstaStoryLocationUpload location)
        {
            return new JObject
            {
                {"x", location.X},
                {"y", location.Y},
                {"z", location.Z},
                {"width", location.Width},
                {"height", location.Height},
                {"rotation", location.Rotation},
                {"location_id", location.LocationId},
                {"is_sticker", location.IsSticker},
            };
        }

        public static JObject ConvertToJson(this InstaStoryHashtagUpload hashtag)
        {
            return new JObject
            {
                {"x", hashtag.X},
                {"y", hashtag.Y},
                {"z", hashtag.Z},
                {"width", hashtag.Width},
                {"height", hashtag.Height},
                {"rotation", hashtag.Rotation},
                {"tag_name", hashtag.TagName},
                {"is_sticker", hashtag.IsSticker},
            };
        }

        public static JObject ConvertToJson(this InstaStorySliderUpload slider)
        {
            return new JObject
            {
                {"x", slider.X},
                {"y", slider.Y},
                {"z", slider.Z},
                {"width", slider.Width},
                {"height", slider.Height},
                {"rotation", slider.Rotation},
                {"question", slider.Question},
                {"viewer_can_vote", true},
                {"viewer_vote", -1.0},
                {"slider_vote_average", 0.0},
                {"background_color", slider.BackgroundColor},
                {"emoji", $"{slider.Emoji}"},
                {"text_color", slider.TextColor},
                {"is_sticker", slider.IsSticker},
            };
        }

        public static JObject ConvertToJson(this InstaMediaStoryUpload mediaStory)
        {
            return new JObject
            {
                {"x", mediaStory.X},
                {"y", mediaStory.Y},
                {"width", mediaStory.Width},
                {"height", mediaStory.Height},
                {"rotation", mediaStory.Rotation},
                {"media_id", mediaStory.MediaPk},
                {"is_sticker", mediaStory.IsSticker},
            };
        }

        public static JObject ConvertToJson(this InstaStoryMentionUpload storyMention, bool isNew = false)
        {
            var jObj = new JObject
            {
                {"x", storyMention.X},
                {"y", storyMention.Y},
                {"z", storyMention.Z},
                {"width", storyMention.Width},
                {"height", storyMention.Height},
                {"rotation", storyMention.Rotation},
                {"user_id", storyMention.Pk}
            };
            if (isNew)
            {
                jObj.Add("type", "mention");
                jObj.Add("is_sticker", true);
                jObj.Add("display_type", "mention_username");
                jObj.Add("tap_state", 0);
                jObj.Add("tap_state_str_id", "mention_sticker_gradient");
            }
            return jObj;
        }

        public static JObject ConvertToJson(this InstaStoryQuestionUpload question)
        {
            return new JObject
            {
                {"x", question.X},
                {"y", question.Y},
                {"z", question.Z},
                {"width", question.Width},
                {"height", question.Height},
                {"rotation", question.Rotation},
                {"question", question.Question},
                {"viewer_can_interact", question.ViewerCanInteract},
                {"profile_pic_url", question.ProfilePicture},
                {"question_type", question.QuestionType},
                {"background_color", question.BackgroundColor},
                {"text_color", question.TextColor},
                {"is_sticker", question.IsSticker},
            };
        }

        public static JObject ConvertToJson(this InstaStoryCountdownUpload countdown)
        {
            return new JObject
            {
                {"x", countdown.X},
                {"y", countdown.Y},
                {"z", countdown.Z},
                {"width", countdown.Width},
                {"height", countdown.Height},
                {"rotation", countdown.Rotation},
                {"text", countdown.Text},
                {"start_background_color", countdown.StartBackgroundColor},
                {"end_background_color", countdown.EndBackgroundColor},
                {"digit_color", countdown.DigitColor},
                {"digit_card_color", countdown.DigitCardColor},
                {"end_ts", countdown.EndTime.ToUnixTime()},
                {"text_color", countdown.TextColor},
                {"following_enabled", countdown.FollowingEnabled},
                {"is_sticker", countdown.IsSticker}
            };
        }

        public static JObject ConvertToJson(this InstaStoryLinkStickerUpload storyLink)
        {
            var sticker = new JObject
            {
                {"x", storyLink.X},
                {"y", storyLink.Y},
                {"z", storyLink.Z},
                {"width", storyLink.Width},
                {"height", storyLink.Height},
                {"rotation", storyLink.Rotation},
                {"type", "story_link"},
                {"link_type", storyLink.LinkType.GetStoryLink()},
                {"url", storyLink.Url},
                {"selected_index", storyLink.SelectedIndex},
                {"is_sticker", Convert.ToInt32(storyLink.IsSticker)},
                {"tap_state", storyLink.TapState},
                {"tap_state_str_id", storyLink.TapStateStrId},
            };
            if (storyLink.CustomStickerText.IsNotEmpty())
            {
                sticker.Add("custom_cta", storyLink.CustomStickerText);
            }
            return sticker;
        }

    }
}

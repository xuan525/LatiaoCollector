﻿using JsonUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bili
{
    /// <summary>
    /// Class <c>BiliApi</c> packaged a number of Apis for Bilibili.
    /// Author: Xuan525
    /// Date: 24/04/2019
    /// </summary>
    class BiliApi
    {
        // Api infos
        public const string APP_KEY = "iVGUTjsxvpLeuDCf";
        public const string SECRET_KEY = "aHRmhWMLkdeMuILqORnYZocwMBpMEOdt";
        public const string BUILD = "8430";

        public static bool IsLoggedIn { get { return LoginCookies != null; } }

        // Cookies for identification
        public static CookieCollection LoginCookies;

        /// <summary>
        /// Convert the parameters in the dictionary to a string.
        /// </summary>
        /// <param name="dic">The dictionary storing parameters</param>
        /// <param name="addVerification">Add verification sign for parameters</param>
        /// <returns>Parameter string</returns>
        public static string DicToParams(Dictionary<string, string> dic, bool addVerification)
        {
            if (dic != null)
            {
                if (addVerification)
                    dic = AddVerification(dic);
                StringBuilder stringBuilder = new StringBuilder();
                foreach (KeyValuePair<string, string> item in dic)
                {
                    stringBuilder.Append("&");
                    stringBuilder.Append(item.Key);
                    stringBuilder.Append("=");
                    stringBuilder.Append(item.Value.Replace(" ", "%20").Replace("&", "%26").Replace("=", "%3D"));
                }
                return stringBuilder.ToString().Substring(1);
            }
            return string.Empty;
        }

        /// <summary>
        /// Add sign for parameters.
        /// </summary>
        /// <param name="dic">Original parameter dictionary</param>
        /// <returns>Parameter dictionary with verification sign</returns>
        public static Dictionary<string, string> AddVerification(Dictionary<string, string> dic)
        {
            dic.Add("appkey", APP_KEY);
            dic.Add("build", BUILD);
            string baseParams = DicToParams(dic.OrderBy(i => i.Key).ToDictionary(i => i.Key, i => i.Value), false);
            string sign = CreateMD5Hash(baseParams + SECRET_KEY);
            dic.Add("sign", sign);
            return dic;
        }

        /// <summary>
        /// Create a MD5 hash for a string.
        /// </summary>
        /// <param name="input"></param>
        /// <returns>MD5 hash</returns>
        private static string CreateMD5Hash(string input)
        {
            MD5 md5 = MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.Append(hashBytes[i].ToString("x2"));
            }
            return sb.ToString();
        }

        /// <summary>
        /// Get the result of the request in the form of a string.
        /// </summary>
        /// <param name="url">Request url</param>
        /// <param name="paramsDic">Parameter dictionary</param>
        /// <param name="addVerification">Add verification sign for parameters</param>
        /// <returns>Text result</returns>
        public static string GetTextResult(string url, Dictionary<string, string> paramsDic, bool addVerification)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(string.Format("{0}?{1}", url, DicToParams(paramsDic, addVerification)));
            request.Referer = System.Text.RegularExpressions.Regex.Match(url, "https?://[^/]+").Value;
            if (LoginCookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(LoginCookies);
            }
            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using(Stream stream = response.GetResponseStream())
                    using(StreamReader reader = new StreamReader(stream))
                        return reader.ReadToEnd();
        }

        /// <summary>
        /// Get the result of the request in the form of a string asynchronously.
        /// </summary>
        /// <param name="url">Request url</param>
        /// <param name="paramsDic">Parameter dictionary</param>
        /// <param name="addVerification">Add verification sign for parameters</param>
        /// <returns>Text result</returns>
        public static Task<string> GetTextResultAsync(string url, Dictionary<string, string> paramsDic, bool addVerification)
        {
            Task<string> task = new Task<string>(() =>
            {
                return GetTextResult(url, paramsDic, addVerification);
            });
            task.Start();
            return task;
        }

        /// <summary>
        /// Get the result of the request in the form of IJson.
        /// </summary>
        /// <param name="url">Request url</param>
        /// <param name="paramsDic">Parameter dictionary</param>
        /// <param name="addVerification">Add verification sign for parameters</param>
        /// <returns>IJson result</returns>
        public static Json.Value GetJsonResult(string url, Dictionary<string, string> paramsDic, bool addVerification)
        {
            string result = GetTextResult(url, paramsDic, addVerification);
            Json.Value json = Json.Parser.Parse(result);
            return json;
        }

        /// <summary>
        /// Get the result of the request in the form of IJson asynchronously.
        /// </summary>
        /// <param name="url">Request url</param>
        /// <param name="paramsDic">Parameter dictionary</param>
        /// <param name="addVerification">Add verification sign for parameters</param>
        /// <returns>IJson result</returns>
        public static Task<Json.Value> GetJsonResultAsync(string url, Dictionary<string, string> paramsDic, bool addVerification)
        {
            Task<Json.Value> task = new Task<Json.Value>(() =>
            {
                return GetJsonResult(url, paramsDic, addVerification);
            });
            task.Start();
            return task;
        }

        /// <summary>
        /// Formatting numbers.
        /// </summary>
        /// <param name="number">Original number</param>
        /// <param name="decimalPlaces">Reserved decimal places</param>
        /// <returns>Formatted numeric string</returns>
        public static string FormatNum(long number, int decimalPlaces)
        {
            if (number < 10000)
            {
                return number.ToString();
            }
            else if (number < 100000000)
            {
                if (decimalPlaces == -1)
                    return ((double)number / 10000) + "万";
                else if (decimalPlaces == 0)
                    return (number / 10000) + "万";
                else
                    return ((double)(number / (10000 / (long)Math.Pow(10, decimalPlaces)))) / (long)Math.Pow(10, decimalPlaces) + "万";
            }
            else
            {
                if (decimalPlaces == -1)
                    return ((double)number / 100000000) + "亿";
                else if (decimalPlaces == 0)
                    return (number / 100000000) + "亿";
                else
                    return (double)(number / (100000000 / (long)Math.Pow(10, decimalPlaces))) / (long)Math.Pow(10, decimalPlaces) + "亿";
            }
        }
    }
}

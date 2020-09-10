using UnityEngine;
using Debug = UnityEngine.Debug;

using System;
using System.Collections.Generic;
using System.Text;

namespace UnityUseful.Misc
{
    public static class Utils
    {
        static StringBuilder m_s_builder = new StringBuilder();

        public enum WebCamRotation { Degrees90, Degrees270 }

        public static string ToShortForm(this TimeSpan t)
        {
            var shortForm = "";

            if (t.Days > 0)
            {
                shortForm += $"{t.Days}d ";
            }
            if (t.Hours > 0)
            {
                shortForm += $"{t.Hours}h ";
            }
            if (t.Minutes > 0)
            {
                shortForm += $"{t.Minutes}m";
            }
            return shortForm;
        }
        public static void DestroyChildren(this Transform parent)
        {
            if (parent)
            {

                foreach (Transform child in parent)
                {
                    UnityEngine.Object.Destroy(child.gameObject);
                }
            }
            else
            {
                Debug.Log("<b>DestroyChildren</b> <color=red>parent is null!</color>");
            }
        }
        public static string ToStyle(this string origin, string style_name)
        {
            return $"<style={style_name}>{origin}</style>";
        }
        public static string WrapWithTag(this string origin, string tag_name = "color", string tag_value = null)
        {
            return $"<{tag_name}{(tag_value != null ? "=" + tag_value : "")}>{origin}</{tag_name}>";
        }
        public static string ToHTMLColor(this Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }
        public static string ToShortForm(this string origin, int limit = 10, string end = "...")
        {
            if (origin.Length > limit)
            {
                return origin.Substring(0, limit - end.Length) + end;
            }
            else
            {
                return origin;
            }
        }

        public static string GetBytesReadable(long i)
        {
            // Get absolute value
            long absolute_i = (i < 0 ? -i : i);
            // Determine the suffix and readable value
            string suffix;
            double readable;
            if (absolute_i >= 0x1000000000000000) // Exabyte
            {
                suffix = "EB";
                readable = (i >> 50);
            }
            else if (absolute_i >= 0x4000000000000) // Petabyte
            {
                suffix = "PB";
                readable = (i >> 40);
            }
            else if (absolute_i >= 0x10000000000) // Terabyte
            {
                suffix = "TB";
                readable = (i >> 30);
            }
            else if (absolute_i >= 0x40000000) // Gigabyte
            {
                suffix = "GB";
                readable = (i >> 20);
            }
            else if (absolute_i >= 0x100000) // Megabyte
            {
                suffix = "MB";
                readable = (i >> 10);
            }
            else if (absolute_i >= 0x400) // Kilobyte
            {
                suffix = "KB";
                readable = i;
            }
            else
            {
                return i.ToString("0 B"); // Byte
            }
            // Divide by 1024 to get fractional value
            readable = (readable / 1024);
            // Return formatted number with suffix
            return readable.ToString("0.### ") + suffix;
        }

        public static Texture2D RotateCamTexture(Texture2D t, WebCamRotation rotation = WebCamRotation.Degrees90)
        {
            Texture2D newTexture = new Texture2D(t.height, t.width);
            Debug.Log($"Utils.RotateCamTexture: from {t.width}x{t.height} -> {newTexture.width}x{newTexture.height}");

            for (int i = 0; i < t.width; i++)
            {
                for (int j = 0; j < t.height; j++)
                {
                    switch (rotation)
                    {
                        case WebCamRotation.Degrees90:
                            newTexture.SetPixel(j, i, t.GetPixel(t.width - i, j)); //(t.width - i, j) -> +90,
                            break;
                        case WebCamRotation.Degrees270:
                            newTexture.SetPixel(t.height - j, i, t.GetPixel(t.width + i, j)); //(t.width + i, j) -> -90
                            break;
                        default:
                            throw new NotImplementedException();
                    }
                }
            }
            newTexture.Apply();
            return newTexture;
        }

        public static void AddOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dic, TKey key, TValue value)
        {
            if (dic == null)
            {
                Debug.Log("AddOrCreate dictionary is null!");
                return;
            }
            if (dic.ContainsKey(key))
            {
                dic[key] = value;
            }
            else
            {
                dic.Add(key, value);
            }
        }
        public static string ToStringKVP<TKey, TValue>(this Dictionary<TKey, TValue> dic, Func<TKey, TValue, string> each = null)
        {
            string result = "Dictionary.ToStringKVP.\n";
            if (each == null)
            {
                each = (key, value) => $"k:{key} v:{value}\n";
            }

            foreach (var item in dic)
            {
                result += each?.Invoke(item.Key, item.Value);
            }

            return result;
        }
        public static string GetTypeName(this Type type, string color = "#00FF00")
        {
            m_s_builder.Clear();

            m_s_builder.Append("<<color=");
            m_s_builder.Append(color);
            m_s_builder.Append(">");
            m_s_builder.Append(type.Name);
            if (type.IsConstructedGenericType)
            {
                m_s_builder.Append("<");
                m_s_builder.Append(string.Join(",", Array.ConvertAll(type.GenericTypeArguments, x => x.Name)));
                m_s_builder.Append(">");
            }

            m_s_builder.Append("</color>>");

            var str = m_s_builder.ToString();
            //$"<<color={color}>{type.Name}{(type.IsConstructedGenericType ? $"<{string.Join(",", type.GenericTypeArguments.ToList().ConvertAll(x => x.Name))}>" : string.Empty)}</color>>";
            return str;
        }

        public static Vector3 AngleToDirection(float degrees)
        {
            var a =  degrees * Mathf.PI / 180f;
            return new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a));
        }
    }
}

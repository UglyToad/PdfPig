namespace UglyToad.Pdf.ContentStream.TypedAccessors
{
    using System.Collections.Generic;
    using Cos;
    using Util;
    using Util.JetBrains.Annotations;

    public static class DictionaryValueAccessorExtensions
    {
        public static long GetLongOrDefault(this ContentStreamDictionary dictionary, CosName key)
        {
            return dictionary.GetLongOrDefault(key, -1L);
        }

        public static long GetLongOrDefault(this ContentStreamDictionary dictionary, IEnumerable<string> keys, long defaultValue)
        {
            foreach (var key in keys)
            {
                if (dictionary.TryGetValue(CosName.Create(key), out var value) && value is ICosNumber number)
                {
                    return number.AsLong();
                }
            }

            return defaultValue;
        }

        public static long GetLongOrDefault(this ContentStreamDictionary dictionary, string key, long defaultValue)
        {
            return dictionary.GetLongOrDefault(CosName.Create(key), defaultValue);
        }

        public static long GetLongOrDefault(this ContentStreamDictionary dictionary, CosName key, long defaultValue)
        {
            if (!dictionary.TryGetValue(key, out CosBase obj) || !(obj is ICosNumber number))
            {
                return defaultValue;
            }

            return number.AsLong();
        }

        public static int GetIntOrDefault(this ContentStreamDictionary dictionary, CosName key)
        {
            return dictionary.GetIntOrDefault(key, -1);
        }
        
        public static int GetIntOrDefault(this ContentStreamDictionary dictionary, IEnumerable<string> keyList, int defaultValue)
        {
            foreach (var key in keyList)
            {
                if (dictionary.TryGetValue(CosName.Create(key), out var obj) && obj is ICosNumber number)
                {
                    return number.AsInt();
                }
            }

            return defaultValue;
        }
        
        public static int GetIntOrDefault(this ContentStreamDictionary dictionary, string key, int defaultValue)
        {
            return dictionary.GetIntOrDefault(CosName.Create(key), defaultValue);
        }
        
        public static int GetIntOrDefault(this ContentStreamDictionary dictionary, CosName key, int defaultValue)
        {
            return dictionary.GetIntOrDefault(key, null, defaultValue);
        }
        
        public static int GetIntOrDefault(this ContentStreamDictionary dictionary, CosName firstKey, CosName secondKey)
        {
            return dictionary.GetIntOrDefault(firstKey, secondKey, -1);
        }
        
        public static int GetIntOrDefault(this ContentStreamDictionary dictionary, CosName firstKey, CosName secondKey, int defaultValue)
        {
            if (dictionary.TryGetValue(firstKey, out var obj) && obj is ICosNumber number)
            {
                return number.AsInt();
            }

            if (secondKey != null && dictionary.TryGetValue(secondKey, out obj) && obj is ICosNumber second)
            {
                return second.AsInt();
            }

            return defaultValue;
        }

        public static CosBase GetDictionaryObject(this ContentStreamDictionary dictionary, CosName firstKey, CosName secondKey)
        {
            CosBase result = dictionary.GetDictionaryObject(firstKey);

            if (result == null && secondKey != null)
            {
                result = dictionary.GetDictionaryObject(secondKey);
            }

            return result;
        }
       
        public static CosBase GetDictionaryObject(this ContentStreamDictionary dictionary, IEnumerable<string> keyList)
        {
            foreach (var key in keyList)
            {
                var obj = dictionary.GetDictionaryObject(CosName.Create(key));

                if (obj != null)
                {
                    return obj;
                }
            }

            return null;
        }
        
        [CanBeNull]
        public static CosBase GetDictionaryObject(this ContentStreamDictionary dictionary, CosName key)
        {
            dictionary.TryGetValue(key, out CosBase result);

            if (result is CosObject)
            {
                result = ((CosObject)result).GetObject();
            }
            if (result is CosNull)
            {
                result = null;
            }

            return result;
        }

        [CanBeNull]
        public static CosObjectKey GetObjectKey(this ContentStreamDictionary dictionary, CosName key)
        {
            if (!dictionary.TryGetValue(key, out var value) || !(value is CosObject obj))
            {
                return null;
            }

            return new CosObjectKey(obj);
        }

        [CanBeNull]
        public static ContentStreamDictionary GetDictionaryOrDefault(this ContentStreamDictionary dictionary,
            CosName key)
        {
            if (!dictionary.TryGetValue(key, out var value))
            {
                return null;
            }

            return value as ContentStreamDictionary;
        }
    }
}

namespace UglyToad.Pdf.Cos
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Linq;
    using Util;

    internal class CosDictionary : CosBase, ICosUpdateInfo
    {
        private static readonly string PATH_SEPARATOR = "/";
        private bool needToBeUpdated;

        /**
         * The name-value pairs of this dictionary. The pairs are kept in the order they were added to the dictionary.
         */
        protected Dictionary<CosName, CosBase> items = new Dictionary<CosName, CosBase>();

        /**
         * Constructor.
         */
        public CosDictionary()
        {
        }

        /**
         * Copy Constructor. This will make a shallow copy of this dictionary.
         *
         * @param dict The dictionary to copy.
         */
        public CosDictionary(CosDictionary dict)
        {
            foreach (var kvp in dict.items)
            {
                items.Add(kvp.Key, kvp.Value);
            }
        }

        /**
         * Only for memory debugging purposes (especially PDFBOX-3284): holds weak
         * references to all instances and prints after each 10,000th instance a
         * statistic across all instances showing how many instances we have per
         * dictionary size (item count).
         * This is to show that there can be a large number of CosDictionary instances
         * but each having only few items, thus using a {@link LinkedHashMap} is a
         * waste of memory resources.
         * 
         * <p>This method should be removed if further testing of CosDictionary uses
         * is not needed anymore.</p>
         */


        /**
         * @see java.util.Map#containsValue(java.lang.Object)
         *
         * @param value The value to find in the map.
         *
         * @return true if the map contains this value.
         */
        public bool containsValue(object value)
        {
            var contains = items.Values.Any(x => ReferenceEquals(x, value));
            if (!contains && value is CosObject)
            {
                contains = items.ContainsValue(((CosObject)value).GetObject());
            }
            return contains;
        }

        /**
         * Search in the map for the value that matches the parameter and return the first key that maps to that value.
         *
         * @param value The value to search for in the map.
         * @return The key for the value in the map or null if it does not exist.
         */
        public CosName getKeyForValue(Object value)
        {
            foreach (var item in items)
            {
                Object nextValue = item.Value;
                if (nextValue.Equals(value)
                    || nextValue is CosObject && ((CosObject)nextValue).GetObject()
                    .Equals(value))
                {
                    return item.Key;
                }
            }

            return null;
        }

        /**
         * This will return the number of elements in this dictionary.
         *
         * @return The number of elements in the dictionary.
         */
        public int size()
        {
            return items.Count;
        }

        /**
         * This will clear all items in the map.
         */
        public void clear()
        {
            items.Clear();
        }

        /**
         * This will get an object from this dictionary. If the object is a reference then it will dereference it and get it
         * from the document. If the object is COSNull then null will be returned.
         *
         * @param key The key to the object that we are getting.
         *
         * @return The object that matches the key.
         */
        public CosBase getDictionaryObject(String key)
        {
            return getDictionaryObject(CosName.Create(key));
        }

        /**
         * This is a special case of getDictionaryObject that takes multiple keys, it will handle the situation where
         * multiple keys could get the same value, ie if either CS or ColorSpace is used to get the colorspace. This will
         * get an object from this dictionary. If the object is a reference then it will dereference it and get it from the
         * document. If the object is COSNull then null will be returned.
         *
         * @param firstKey The first key to try.
         * @param secondKey The second key to try.
         *
         * @return The object that matches the key.
         */
        public CosBase getDictionaryObject(CosName firstKey, CosName secondKey)
        {
            CosBase retval = getDictionaryObject(firstKey);
            if (retval == null && secondKey != null)
            {
                retval = getDictionaryObject(secondKey);
            }
            return retval;
        }

        /**
         * This is a special case of getDictionaryObject that takes multiple keys, it will handle the situation where
         * multiple keys could get the same value, ie if either CS or ColorSpace is used to get the colorspace. This will
         * get an object from this dictionary. If the object is a reference then it will dereference it and get it from the
         * document. If the object is COSNull then null will be returned.
         *
         * @param keyList The list of keys to find a value.
         *
         * @return The object that matches the key.
         */
        public CosBase getDictionaryObject(String[] keyList)
        {
            CosBase retval = null;
            for (int i = 0; i < keyList.Length && retval == null; i++)
            {
                retval = getDictionaryObject(CosName.Create(keyList[i]));
            }
            return retval;
        }

        /**
         * This will get an object from this dictionary. If the object is a reference then it will dereference it and get it
         * from the document. If the object is COSNull then null will be returned.
         *
         * @param key The key to the object that we are getting.
         *
         * @return The object that matches the key.
         */
        public CosBase getDictionaryObject(CosName key)
        {
            items.TryGetValue(key, out CosBase result);

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

        /**
         * This will set an item in the dictionary. If value is null then the result will be the same as removeItem( key ).
         *
         * @param key The key to the dictionary object.
         * @param value The value to the dictionary object.
         */
        public void setItem(CosName key, CosBase value)
        {
            if (value == null)
            {
                removeItem(key);
            }
            else
            {
                items[key] = value;
            }
        }

        /**
         * This will set an item in the dictionary. If value is null then the result will be the same as removeItem( key ).
         *
         * @param key The key to the dictionary object.
         * @param value The value to the dictionary object.
         */
        public void setItem(CosName key, ICosObject value)
        {
            CosBase baseObject = null;
            if (value != null)
            {
                baseObject = value.GetCosObject();
            }
            setItem(key, baseObject);
        }

        /**
         * This will set an item in the dictionary. If value is null then the result will be the same as removeItem( key ).
         *
         * @param key The key to the dictionary object.
         * @param value The value to the dictionary object.
         */
        public void setItem(String key, ICosObject value)
        {
            setItem(CosName.Create(key), value);
        }

        /**
         * This will set an item in the dictionary.
         *
         * @param key The key to the dictionary object.
         * @param value The value to the dictionary object.
         */
        public void setbool(String key, bool value)
        {
            setItem(CosName.Create(key), (CosBoolean)value);
        }

        /**
         * This will set an item in the dictionary.
         *
         * @param key The key to the dictionary object.
         * @param value The value to the dictionary object.
         */
        public void setbool(CosName key, bool value)
        {
            setItem(key, (CosBoolean)value);
        }

        /**
         * This will set an item in the dictionary. If value is null then the result will be the same as removeItem( key ).
         *
         * @param key The key to the dictionary object.
         * @param value The value to the dictionary object.
         */
        public void setItem(String key, CosBase value)
        {
            setItem(CosName.Create(key), value);
        }

        /**
         * This is a convenience method that will convert the value to a CosName object. If it is null then the object will
         * be removed.
         *
         * @param key The key to the object,
         * @param value The string value for the name.
         */
        public void setName(String key, String value)
        {
            setName(CosName.Create(key), value);
        }

        /**
         * This is a convenience method that will convert the value to a CosName object. If it is null then the object will
         * be removed.
         *
         * @param key The key to the object,
         * @param value The string value for the name.
         */
        public void setName(CosName key, String value)
        {
            CosName name = null;
            if (value != null)
            {
                name = CosName.Create(value);
            }
            setItem(key, name);
        }

        /**
         * Set the value of a date entry in the dictionary.
         *
         * @param key The key to the date value.
         * @param date The date value.
         */
        public void setDate(String key, DateTime date)
        {
            setDate(CosName.Create(key), date);
        }

        /**
         * Set the date object.
         *
         * @param key The key to the date.
         * @param date The date to set.
         */
        public void setDate(CosName key, DateTime date)
        {
            setString(key, date.ToString());
        }

        /**
         * Set the value of a date entry in the dictionary.
         *
         * @param embedded The embedded dictionary.
         * @param key The key to the date value.
         * @param date The date value.
         */
        public void setEmbeddedDate(String embedded, string key, DateTime date)
        {
            setEmbeddedDate(embedded, CosName.Create(key), date);
        }

        /**
         * Set the date object.
         *
         * @param embedded The embedded dictionary.
         * @param key The key to the date.
         * @param date The date to set.
         */
        public void setEmbeddedDate(String embedded, CosName key, DateTime date)
        {
            CosDictionary dic = (CosDictionary)getDictionaryObject(embedded);
            if (dic == null)
            {
                dic = new CosDictionary();
                setItem(embedded, dic);
            }
            dic.setDate(key, date);
        }

        /**
         * This is a convenience method that will convert the value to a COSString object. If it is null then the object
         * will be removed.
         *
         * @param key The key to the object,
         * @param value The string value for the name.
         */
        public void setString(String key, String value)
        {
            setString(CosName.Create(key), value);
        }

        /**
         * This is a convenience method that will convert the value to a COSString object. If it is null then the object
         * will be removed.
         *
         * @param key The key to the object,
         * @param value The string value for the name.
         */
        public void setString(CosName key, String value)
        {
            CosString name = null;
            if (value != null)
            {
                name = new CosString(value);
            }
            setItem(key, name);
        }

        /**
         * This is a convenience method that will convert the value to a COSString object. If it is null then the object
         * will be removed.
         *
         * @param embedded The embedded dictionary to set the item in.
         * @param key The key to the object,
         * @param value The string value for the name.
         */
        public void setEmbeddedString(String embedded, String key, String value)
        {
            setEmbeddedString(embedded, CosName.Create(key), value);
        }

        /**
         * This is a convenience method that will convert the value to a COSString object. If it is null then the object
         * will be removed.
         *
         * @param embedded The embedded dictionary to set the item in.
         * @param key The key to the object,
         * @param value The string value for the name.
         */
        public void setEmbeddedString(String embedded, CosName key, String value)
        {
            CosDictionary dic = (CosDictionary)getDictionaryObject(embedded);
            if (dic == null && value != null)
            {
                dic = new CosDictionary();
                setItem(embedded, dic);
            }
            if (dic != null)
            {
                dic.setString(key, value);
            }
        }

        /**
         * This is a convenience method that will convert the value to a COSInteger object.
         *
         * @param key The key to the object,
         * @param value The int value for the name.
         */
        public void setInt(String key, int value)
        {
            setInt(CosName.Create(key), value);
        }

        /**
         * This is a convenience method that will convert the value to a COSInteger object.
         *
         * @param key The key to the object,
         * @param value The int value for the name.
         */
        public void setInt(CosName key, int value)
        {
            setItem(key, CosInt.Get(value));
        }

        /**
         * This is a convenience method that will convert the value to a COSInteger object.
         *
         * @param key The key to the object,
         * @param value The int value for the name.
         */
        public void setLong(String key, long value)
        {
            setLong(CosName.Create(key), value);
        }

        /**
         * This is a convenience method that will convert the value to a COSInteger object.
         *
         * @param key The key to the object,
         * @param value The int value for the name.
         */
        public void setLong(CosName key, long value)
        {
            var intVal = CosInt.Get(value);
            setItem(key, intVal);
        }

        /**
         * This is a convenience method that will convert the value to a COSInteger object.
         *
         * @param embeddedDictionary The embedded dictionary.
         * @param key The key to the object,
         * @param value The int value for the name.
         */
        public void setEmbeddedInt(String embeddedDictionary, String key, int value)
        {
            setEmbeddedInt(embeddedDictionary, CosName.Create(key), value);
        }

        /**
         * This is a convenience method that will convert the value to a COSInteger object.
         *
         * @param embeddedDictionary The embedded dictionary.
         * @param key The key to the object,
         * @param value The int value for the name.
         */
        public void setEmbeddedInt(String embeddedDictionary, CosName key, int value)
        {
            CosDictionary embedded = (CosDictionary)getDictionaryObject(embeddedDictionary);
            if (embedded == null)
            {
                embedded = new CosDictionary();
                setItem(embeddedDictionary, embedded);
            }
            embedded.setInt(key, value);
        }

        /**
         * This is a convenience method that will convert the value to a COSFloat object.
         *
         * @param key The key to the object,
         * @param value The int value for the name.
         */
        public void setFloat(String key, float value)
        {
            setFloat(CosName.Create(key), value);
        }

        /**
         * This is a convenience method that will convert the value to a COSFloat object.
         *
         * @param key The key to the object,
         * @param value The int value for the name.
         */
        public void setFloat(CosName key, float value)
        {
            var fltVal = new CosFloat(value);
            setItem(key, fltVal);
        }

        /**
         * Sets the given bool value at bitPos in the flags.
         *
         * @param field The CosName of the field to set the value into.
         * @param bitFlag the bit position to set the value in.
         * @param value the value the bit position should have.
         */
        public void setFlag(CosName field, int bitFlag, bool value)
        {
            int currentFlags = getInt(field, 0);
            if (value)
            {
                currentFlags = currentFlags | bitFlag;
            }
            else
            {
                currentFlags &= ~bitFlag;
            }
            setInt(field, currentFlags);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name. Null is returned
         * if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @return The COS name.
         */
        public CosName getCosName(CosName key)
        {
            CosBase name = getDictionaryObject(key);
            if (name is CosName)
            {
                return (CosName)name;
            }
            return null;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name. Default is
         * returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The COS name.
         */
        public CosName getCosName(CosName key, CosName defaultValue)
        {
            CosBase name = getDictionaryObject(key);
            if (name is CosName)
            {
                return (CosName)name;
            }
            return defaultValue;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @return The name converted to a string.
         */
        public String getNameAsString(String key)
        {
            return getNameAsString(CosName.Create(key));
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @return The name converted to a string.
         */
        public String getNameAsString(CosName key)
        {
            String retval = null;
            CosBase name = getDictionaryObject(key);
            if (name is CosName)
            {
                retval = ((CosName)name).Name;
            }
            else if (name is CosString)
            {
                retval = ((CosString)name).GetString();
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The name converted to a string.
         */
        public String getNameAsString(String key, String defaultValue)
        {
            return getNameAsString(CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The name converted to a string.
         */
        public String getNameAsString(CosName key, String defaultValue)
        {
            String retval = getNameAsString(key);
            if (retval == null)
            {
                retval = defaultValue;
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @return The name converted to a string.
         */
        public String getString(String key)
        {
            return getString(CosName.Create(key));
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @return The name converted to a string.
         */
        public String getString(CosName key)
        {
            String retval = null;
            CosBase value = getDictionaryObject(key);
            if (value is CosString)
            {
                retval = ((CosString)value).GetString();
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The default value to return.
         * @return The name converted to a string.
         */
        public String getString(String key, String defaultValue)
        {
            return getString(CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The default value to return.
         * @return The name converted to a string.
         */
        public String getString(CosName key, String defaultValue)
        {
            String retval = getString(key);
            if (retval == null)
            {
                retval = defaultValue;
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param embedded The embedded dictionary.
         * @param key The key to the item in the dictionary.
         * @return The name converted to a string.
         */
        public String getEmbeddedString(String embedded, String key)
        {
            return getEmbeddedString(embedded, CosName.Create(key), null);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param embedded The embedded dictionary.
         * @param key The key to the item in the dictionary.
         * @return The name converted to a string.
         */
        public String getEmbeddedString(String embedded, CosName key)
        {
            return getEmbeddedString(embedded, key, null);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param embedded The embedded dictionary.
         * @param key The key to the item in the dictionary.
         * @param defaultValue The default value to return.
         * @return The name converted to a string.
         */
        public String getEmbeddedString(String embedded, String key, String defaultValue)
        {
            return getEmbeddedString(embedded, CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param embedded The embedded dictionary.
         * @param key The key to the item in the dictionary.
         * @param defaultValue The default value to return.
         * @return The name converted to a string.
         */
        public String getEmbeddedString(String embedded, CosName key, String defaultValue)
        {
            String retval = defaultValue;
            CosDictionary dic = (CosDictionary)getDictionaryObject(embedded);
            if (dic != null)
            {
                retval = dic.getString(key, defaultValue);
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary or if the date was invalid.
         *
         * @param key The key to the item in the dictionary.
         * @return The name converted to a date.
         */
        public DateTime? getDate(String key)
        {
            return getDate(CosName.Create(key));
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary or if the date was invalid.
         *
         * @param key The key to the item in the dictionary.
         * @return The name converted to a date.
         */
        public DateTime? getDate(CosName key)
        {
            CosBase baseObj = getDictionaryObject(key);
            if (baseObj is CosString)
            {
                return DateConverter.toCalendar((CosString)baseObj);
            }

            return null;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a date. Null is returned
         * if the entry does not exist in the dictionary or if the date was invalid.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The default value to return.
         * @return The name converted to a date.
         */
        public DateTime? getDate(String key, DateTime defaultValue)
        {
            return getDate(CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a date. Null is returned
         * if the entry does not exist in the dictionary or if the date was invalid.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The default value to return.
         * @return The name converted to a date.
         */
        public DateTime? getDate(CosName key, DateTime? defaultValue)
        {
            var retval = getDate(key);
            if (retval == null)
            {
                retval = defaultValue;
            }

            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param embedded The embedded dictionary to get.
         * @param key The key to the item in the dictionary.
         * @return The name converted to a string.
         * @ If there is an error converting to a date.
         */
        public DateTime? getEmbeddedDate(String embedded, String key)
        {
            return getEmbeddedDate(embedded, CosName.Create(key), null);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a name and convert it to
         * a string. Null is returned if the entry does not exist in the dictionary.
         *
         * @param embedded The embedded dictionary to get.
         * @param key The key to the item in the dictionary.
         * @return The name converted to a string.
         *
         * @ If there is an error converting to a date.
         */
        public DateTime? getEmbeddedDate(String embedded, CosName key)
        {
            return getEmbeddedDate(embedded, key, null);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a date. Null is returned
         * if the entry does not exist in the dictionary.
         *
         * @param embedded The embedded dictionary to get.
         * @param key The key to the item in the dictionary.
         * @param defaultValue The default value to return.
         * @return The name converted to a string.
         * @ If there is an error converting to a date.
         */
        public DateTime? getEmbeddedDate(String embedded, String key, DateTime? defaultValue)

        {
            return getEmbeddedDate(embedded, CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a date. Null is returned
         * if the entry does not exist in the dictionary.
         *
         * @param embedded The embedded dictionary to get.
         * @param key The key to the item in the dictionary.
         * @param defaultValue The default value to return.
         * @return The name converted to a string.
         * @ If there is an error converting to a date.
         */
        public DateTime? getEmbeddedDate(String embedded, CosName key, DateTime? defaultValue)

        {
            var retval = defaultValue;
            CosDictionary eDic = (CosDictionary)getDictionaryObject(embedded);
            if (eDic != null)
            {
                retval = eDic.getDate(key, defaultValue);
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a cos bool and convert
         * it to a primitive bool.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value returned if the entry is null.
         *
         * @return The value converted to a bool.
         */
        public bool getbool(String key, bool defaultValue)
        {
            return getbool(CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a CosBool and convert
         * it to a primitive bool.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value returned if the entry is null.
         *
         * @return The entry converted to a bool.
         */
        public bool getbool(CosName key, bool defaultValue)
        {
            return getbool(key, null, defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a CosBool and convert
         * it to a primitive bool.
         *
         * @param firstKey The first key to the item in the dictionary.
         * @param secondKey The second key to the item in the dictionary.
         * @param defaultValue The value returned if the entry is null.
         *
         * @return The entry converted to a bool.
         */
        public bool getbool(CosName firstKey, CosName secondKey, bool defaultValue)
        {
            bool retval = defaultValue;
            CosBase boolValue = getDictionaryObject(firstKey, secondKey);
            if (boolValue is CosBoolean)
            {
                retval = ((CosBoolean)boolValue).Value;
            }

            return retval;
        }

        /**
         * GetLongOrDefault an integer from an embedded dictionary. Useful for 1-1 mappings. default:-1
         *
         * @param embeddedDictionary The name of the embedded dictionary.
         * @param key The key in the embedded dictionary.
         *
         * @return The value of the embedded integer.
         */
        public int getEmbeddedInt(String embeddedDictionary, String key)
        {
            return getEmbeddedInt(embeddedDictionary, CosName.Create(key));
        }

        /**
         * GetLongOrDefault an integer from an embedded dictionary. Useful for 1-1 mappings. default:-1
         *
         * @param embeddedDictionary The name of the embedded dictionary.
         * @param key The key in the embedded dictionary.
         *
         * @return The value of the embedded integer.
         */
        public int getEmbeddedInt(String embeddedDictionary, CosName key)
        {
            return getEmbeddedInt(embeddedDictionary, key, -1);
        }

        /**
         * GetLongOrDefault an integer from an embedded dictionary. Useful for 1-1 mappings.
         *
         * @param embeddedDictionary The name of the embedded dictionary.
         * @param key The key in the embedded dictionary.
         * @param defaultValue The value if there is no embedded dictionary or it does not contain the key.
         *
         * @return The value of the embedded integer.
         */
        public int getEmbeddedInt(String embeddedDictionary, String key, int defaultValue)
        {
            return getEmbeddedInt(embeddedDictionary, CosName.Create(key), defaultValue);
        }

        /**
         * GetLongOrDefault an integer from an embedded dictionary. Useful for 1-1 mappings.
         *
         * @param embeddedDictionary The name of the embedded dictionary.
         * @param key The key in the embedded dictionary.
         * @param defaultValue The value if there is no embedded dictionary or it does not contain the key.
         *
         * @return The value of the embedded integer.
         */
        public int getEmbeddedInt(String embeddedDictionary, CosName key, int defaultValue)
        {
            int retval = defaultValue;
            CosDictionary embedded = (CosDictionary)getDictionaryObject(embeddedDictionary);
            if (embedded != null)
            {
                retval = embedded.getInt(key, defaultValue);
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an int. -1 is returned if
         * there is no value.
         *
         * @param key The key to the item in the dictionary.
         * @return The integer value.
         */
        public int getInt(String key)
        {
            return getInt(CosName.Create(key), -1);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an int. -1 is returned if
         * there is no value.
         *
         * @param key The key to the item in the dictionary.
         * @return The integer value..
         */
        public int getInt(CosName key)
        {
            return getInt(key, -1);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an integer. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param keyList The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The integer value.
         */
        public int getInt(String[] keyList, int defaultValue)
        {
            int retval = defaultValue;
            CosBase obj = getDictionaryObject(keyList);
            if (obj is ICosNumber)
            {
                retval = ((ICosNumber)obj).AsInt();
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an integer. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The integer value.
         */
        public int getInt(String key, int defaultValue)
        {
            return getInt(CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an integer. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The integer value.
         */
        public int getInt(CosName key, int defaultValue)
        {
            return getInt(key, null, defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an integer. If the
         * dictionary value is null then the default Value -1 will be returned.
         *
         * @param firstKey The first key to the item in the dictionary.
         * @param secondKey The second key to the item in the dictionary.
         * @return The integer value.
         */
        public int getInt(CosName firstKey, CosName secondKey)
        {
            return getInt(firstKey, secondKey, -1);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an integer. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param firstKey The first key to the item in the dictionary.
         * @param secondKey The second key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The integer value.
         */
        public int getInt(CosName firstKey, CosName secondKey, int defaultValue)
        {
            int retval = defaultValue;
            CosBase obj = getDictionaryObject(firstKey, secondKey);
            if (obj is ICosNumber ICosNumber)
            {
                retval = ICosNumber.AsInt();
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an long. -1 is returned
         * if there is no value.
         *
         * @param key The key to the item in the dictionary.
         *
         * @return The long value.
         */
        public long getLong(String key)
        {
            return getLong(CosName.Create(key), -1L);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an long. -1 is returned
         * if there is no value.
         *
         * @param key The key to the item in the dictionary.
         * @return The long value.
         */
        public long getLong(CosName key)
        {
            return getLong(key, -1L);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an long. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param keyList The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The long value.
         */
        public long getLong(String[] keyList, long defaultValue)
        {
            long retval = defaultValue;
            CosBase obj = getDictionaryObject(keyList);
            if (obj is ICosNumber ICosNumber)
            {
                retval = ICosNumber.AsLong();
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an integer. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The integer value.
         */
        public long getLong(String key, long defaultValue)
        {
            return getLong(CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an integer. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The integer value.
         */
        public long getLong(CosName key, long defaultValue)
        {
            long retval = defaultValue;
            CosBase obj = getDictionaryObject(key);
            if (obj is ICosNumber)
            {
                retval = ((ICosNumber)obj).AsLong();
            }
            return retval;
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an float. -1 is returned
         * if there is no value.
         *
         * @param key The key to the item in the dictionary.
         * @return The float value.
         */
        public float getFloat(String key)
        {
            return getFloat(CosName.Create(key), -1);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an float. -1 is returned
         * if there is no value.
         *
         * @param key The key to the item in the dictionary.
         * @return The float value.
         */
        public float getFloat(CosName key)
        {
            return getFloat(key, -1);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be a float. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The float value.
         */
        public float getFloat(String key, float defaultValue)
        {
            return getFloat(CosName.Create(key), defaultValue);
        }

        /**
         * This is a convenience method that will get the dictionary object that is expected to be an float. If the
         * dictionary value is null then the default Value will be returned.
         *
         * @param key The key to the item in the dictionary.
         * @param defaultValue The value to return if the dictionary item is null.
         * @return The float value.
         */
        public float getFloat(CosName key, float defaultValue)
        {
            float retval = defaultValue;
            CosBase obj = getDictionaryObject(key);
            if (obj is ICosNumber)
            {
                retval = ((ICosNumber)obj).AsFloat();
            }
            return retval;
        }

        /**
         * Gets the bool value from the flags at the given bit position.
         *
         * @param field The CosName of the field to get the flag from.
         * @param bitFlag the bitPosition to get the value from.
         *
         * @return true if the number at bitPos is '1'
         */
        public bool getFlag(CosName field, int bitFlag)
        {
            int ff = getInt(field, 0);
            return (ff & bitFlag) == bitFlag;
        }

        /**
         * This will remove an item for the dictionary. This will do nothing of the object does not exist.
         *
         * @param key The key to the item to remove from the dictionary.
         */
        public void removeItem(CosName key)
        {
            items.Remove(key);
        }

        /**
         * This will do a lookup into the dictionary.
         *
         * @param key The key to the object.
         *
         * @return The item that matches the key.
         */
        public CosBase getItem(CosName key)
        {
            items.TryGetValue(key, out CosBase result);

            return result;
        }

        /**
         * This will do a lookup into the dictionary.
         * 
         * @param key The key to the object.
         *
         * @return The item that matches the key.
         */
        public CosBase getItem(String key)
        {
            return getItem(CosName.Create(key));
        }

        /**
         * Returns the names of the entries in this dictionary. The returned set is in the order the entries were added to
         * the dictionary.
         *
         * @since Apache PDFBox 1.1.0
         * @return names of the entries in this dictionary
         */
        public IReadOnlyList<CosName> keySet()
        {
            return new List<CosName>(items.Keys);
        }

        /**
         * Returns the name-value entries in this dictionary. The returned set is in the order the entries were added to the
         * dictionary.
         *
         * @since Apache PDFBox 1.1.0
         * @return name-value entries in this dictionary
         */
        public IReadOnlyList<KeyValuePair<CosName, CosBase>> entrySet()
        {
            return items.ToList();
        }

        /**
         * This will get all of the values for the dictionary.
         *
         * @return All the values for the dictionary.
         */
        public IReadOnlyList<CosBase> getValues()
        {
            return items.Values.ToList();
        }

        /**
         * visitor pattern double dispatch method.
         *
         * @param visitor The object to notify when visiting this object.
         * @return The object that the visitor returns.
         *
         * @ If there is an error visiting this object.
         */
        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromDictionary(this);
        }

        public bool isNeedToBeUpdated()
        {
            return needToBeUpdated;
        }

        public void setNeedToBeUpdated(bool flag)
        {
            needToBeUpdated = flag;
        }

        /**
         * This will add all of the dictionarys keys/values to this dictionary. Only called when adding keys to a trailer
         * that already exists.
         *
         * @param dic The dic to get the keys from.
         */
        public void addAll(CosDictionary dic)
        {
            foreach (var entry in dic.entrySet())
            {
                /*
                 * If we're at a second trailer, we have a linearized pdf file, meaning that the first Size entry represents
                 * all of the objects so we don't need to grab the second.
                 */
                if (!entry.Key.Name.Equals("Size")
                    || !items.ContainsKey(CosName.Create("Size")))
                {
                    setItem(entry.Key, entry.Value);
                }
            }
        }

        /**
         * @see java.util.Map#containsKey(Object)
         *
         * @param name The key to find in the map.
         * @return true if the map contains this key.
         */
        public bool containsKey(CosName name)
        {
            return this.items.ContainsKey(name);
        }

        /**
         * @see java.util.Map#containsKey(Object)
         *
         * @param name The key to find in the map.
         * @return true if the map contains this key.
         */
        public bool containsKey(String name)
        {
            return containsKey(CosName.Create(name));
        }

        /**
         * Nice method, gives you every object you want Arrays works properly too. Try "P/Annots/[k]/Rect" where k means the
         * index of the Annotsarray.
         *
         * @param objPath the relative path to the object.
         * @return the object
         */
        public CosBase getObjectFromPath(String objPath)
        {
            String[] path = objPath.Split(new string[] { PATH_SEPARATOR }, StringSplitOptions.RemoveEmptyEntries);
            CosBase retval = this;
            foreach (var pathString in path)
            {
                if (retval is COSArray)
                {
                    int idx = int.Parse(pathString.Replace("\\[", "").Replace("\\]", ""));
                    retval = ((COSArray)retval).getObject(idx);
                }
                else if (retval is CosDictionary)
                {
                    retval = ((CosDictionary)retval).getDictionaryObject(pathString);
                }
            }

            return retval;
        }

        /**
         * Returns an unmodifiable view of this dictionary.
         * 
         * @return an unmodifiable view of this dictionary
         */
        public CosDictionary asUnmodifiableDictionary()
        {
            throw new NotImplementedException();
            //  return new UnmodifiableCosDictionary(this);
        }

        /**
         * {@inheritDoc}
         */
        public override string ToString()
        {
            try
            {
                return getDictionaryString(this, new List<CosBase>());
            }
            catch (Exception e)
            {
                return $"CosDictionary{{{e.Message}}}";
            }
        }

        private static string getDictionaryString(CosBase baseObj, List<CosBase> objs)
        {
            if (baseObj == null)
            {
                return "null";
            }
            if (objs.Contains(baseObj))
            {
                // avoid endless recursion
                return baseObj.GetHashCode().ToString();
            }

            objs.Add(baseObj);

            if (baseObj is CosDictionary cosDictionary)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("CosDictionary{");
                foreach (var entry in cosDictionary.entrySet())
                {

                    sb.Append(entry.Key);
                    sb.Append(":");
                    sb.Append(getDictionaryString(entry.Value, objs));
                    sb.Append(";");
                }

                sb.Append("}");

                return sb.ToString();
            }

            if (baseObj is CosObject obj)
            {
                return "COSObject{" + getDictionaryString(obj.GetObject(), objs) + "}";
            }

            return baseObj.ToString();
        }

        public bool NeedsToBeUpdated { get; set; }
    }
}

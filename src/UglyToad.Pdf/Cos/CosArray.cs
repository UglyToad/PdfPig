/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/**
 * An array of PDFBase objects as part of the PDF document.
 *
 * @author Ben Litchfield
 */
namespace UglyToad.Pdf.Cos
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    public class COSArray : CosBase, IEnumerable<CosBase>, ICosUpdateInfo
    {
        private readonly List<CosBase> objects = new List<CosBase>();
        public bool NeedsToBeUpdated { get; set; }

        public int Count => objects.Count;

        /**
         * This will add an object to the array.
         *
         * @param object The object to add to the array.
         */
        public void add(CosBase obj)
        {
            objects.Add(obj);
        }

        /**
         * This will add an object to the array.
         *
         * @param object The object to add to the array.
         */
        public void add(ICosObject obj)
        {
            objects.Add(obj.GetCosObject());
        }

        /**
         * Add the specified object at the ith location and push the rest to the
         * right.
         *
         * @param i The index to add at.
         * @param object The object to add at that index.
         */
        public void add(int i, CosBase obj)
        {
            objects.Insert(i, obj);
        }

        /**
         * This will remove all of the objects in the collection.
         */
        public void clear()
        {
            objects.Clear();
        }

        /**
         * This will remove all of the objects in the collection.
         *
         * @param objectsList The list of objects to remove from the collection.
         */
        public void removeAll(IEnumerable<CosBase> objectsList)
        {
            if (objectsList == null)
            {
                return;
            }

            foreach (var cosBase in objectsList)
            {
                objects.Remove(cosBase);
            }
        }

        /**
         * This will retain all of the objects in the collection.
         *
         * @param objectsList The list of objects to retain from the collection.
         */
        public void retainAll(IEnumerable<CosBase> objectsList)
        {
            objects.Clear();
            objects.AddRange(new List<CosBase>(objectsList));
        }

        /**
         * This will add an object to the array.
         *
         * @param objectsList The object to add to the array.
         */
        public void addAll(IEnumerable<CosBase> objectsList)
        {
            objects.AddRange(objectsList);
        }

        /**
         * This will add all objects to this array.
         *
         * @param objectList The objects to add.
         */
        public void addAll(COSArray objectList)
        {
            if (objectList != null)
            {
                objects.AddRange(objectList.objects);
            }
        }

        /**
         * Add the specified object at the ith location and push the rest to the
         * right.
         *
         * @param i The index to add at.
         * @param objectList The object to add at that index.
         */
        public void addAll(int i, IEnumerable<CosBase> objectList)
        {
            if (objectList == null)
            {
                throw new ArgumentNullException(nameof(objectList));
            }

            objects.InsertRange(i, objectList);
        }

        /**
         * This will set an object at a specific index.
         *
         * @param index zero based index into array.
         * @param object The object to set.
         */
        public void set(int index, CosBase obj)
        {
            objects[index] = obj;
        }

        /**
         * This will set an object at a specific index.
         *
         * @param index zero based index into array.
         * @param intVal The object to set.
         */
        public void set(int index, int intVal)
        {
            set(index, CosInt.Get(intVal));
        }

        /**
         * This will set an object at a specific index.
         *
         * @param index zero based index into array.
         * @param object The object to set.
         */
        public void set(int index, ICosObject obj)
        {
            CosBase baseObj = null;
            if (obj != null)
            {
                baseObj = baseObj.GetCosObject();
            }

            set(index, baseObj);
        }

        /**
         * This will get an object from the array.  This will dereference the object.
         * If the object is COSNull then null will be returned.
         *
         * @param index The index into the array to get the object.
         *
         * @return The object at the requested index.
         */
        public CosBase getObject(int index)
        {
            var obj = objects[index];
            if (obj is CosObject cosObject)
            {
                obj = cosObject.GetObject();
            }
            if (obj is CosNull)
            {
                obj = null;
            }

            return obj;
        }

        /**
         * This will get an object from the array.  This will NOT dereference
         * the COS object.
         *
         * @param index The index into the array to get the object.
         *
         * @return The object at the requested index.
         */
        public CosBase get(int index)
        {
            return objects[index];
        }

        /**
         * GetLongOrDefault the value of the array as an integer.
         *
         * @param index The index into the list.
         *
         * @return The value at that index or -1 if it is null.
         */
        public int getInt(int index)
        {
            return getInt(index, -1);
        }

        /**
         * GetLongOrDefault the value of the array as an integer, return the default if it does
         * not exist.
         *
         * @param index The value of the array.
         * @param defaultValue The value to return if the value is null.
         * @return The value at the index or the defaultValue.
         */
        public int getInt(int index, int defaultValue)
        {
            int retval = defaultValue;
            if (index < size())
            {
                var obj = objects[index];
                if (obj is ICosNumber num)
                {
                    retval = num.AsInt();
                }
            }
            return retval;
        }

        /**
         * Set the value in the array as an integer.
         *
         * @param index The index into the array.
         * @param value The value to set.
         */
        public void setInt(int index, int value)
        {
            set(index, CosInt.Get(value));
        }

        /**
         * Set the value in the array as a name.
         * @param index The index into the array.
         * @param name The name to set in the array.
         */
        public void setName(int index, string name)
        {
            set(index, CosName.Create(name));
        }

        /**
         * GetLongOrDefault the value of the array as a string.
         *
         * @param index The index into the array.
         * @return The name converted to a string or null if it does not exist.
         */
        public string getName(int index)
        {
            return getName(index, null);
        }

        /**
         * GetLongOrDefault an entry in the array that is expected to be a COSName.
         * @param index The index into the array.
         * @param defaultValue The value to return if it is null.
         * @return The value at the index or defaultValue if none is found.
         */
        public string getName(int index, string defaultValue)
        {
            var retval = defaultValue;
            if (index < size())
            {
                var obj = objects[index];
                if (obj is CosName name)
                {
                    retval = name.Name;
                }
            }
            return retval;
        }

        /**
         * Set the value in the array as a string.
         * @param index The index into the array.
         * @param string The string to set in the array.
         */
        public void setString(int index, string str)
        {
            if (str != null)
            {
                set(index, new CosString(str));
            }
            else
            {
                set(index, null);
            }
        }

        /**
         * GetLongOrDefault the value of the array as a string.
         *
         * @param index The index into the array.
         * @return The string or null if it does not exist.
         */
        public string getString(int index)
        {
            return getString(index, null);
        }

        /**
         * GetLongOrDefault an entry in the array that is expected to be a COSName.
         * @param index The index into the array.
         * @param defaultValue The value to return if it is null.
         * @return The value at the index or defaultValue if none is found.
         */
        public string getString(int index, string defaultValue)
        {
            var retval = defaultValue;
            if (index < size())
            {
                var obj = objects[index];
                if (obj is CosString str)
                {
                    retval = str.GetString();
                }
            }
            return retval;
        }

        /**
         * This will get the size of this array.
         *
         * @return The number of elements in the array.
         */
        public int size()
        {
            return objects.Count;
        }

        /**
         * This will remove an element from the array.
         *
         * @param i The index of the object to remove.
         *
         * @return The object that was removed.
         */
        public CosBase remove(int i)
        {
            var item = objects[i];
            objects.RemoveAt(i);

            return item;
        }

        /**
         * This will remove an element from the array.
         *
         * @param o The object to remove.
         *
         * @return <code>true</code> if the object was removed, <code>false</code>
         *  otherwise
         */
        public bool remove(CosBase o)
        {
            return objects.Remove(o);
        }

        /**
         * This will remove an element from the array.
         * This method will also remove a reference to the object.
         *
         * @param o The object to remove.
         * @return <code>true</code> if the object was removed, <code>false</code>
         *  otherwise
         */
        public bool removeObject(CosBase o)
        {
            var removed = remove(o);
            if (!removed)
            {
                for (int i = 0; i < size(); i++)
                {
                    CosBase entry = get(i);
                    if (entry is CosObject obj)
                    {
                        if (obj.GetObject().Equals(o))
                        {
                            return remove(entry);
                        }
                    }
                }
            }
            return removed;
        }

        /**
         * {@inheritDoc}
         */

        public IEnumerator<CosBase> GetEnumerator()
        {
            return objects.GetEnumerator();
        }

        public override string ToString()
        {
            return "COSArray{" + objects + "}";
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /**
         * This will return the index of the entry or -1 if it is not found.
         *
         * @param object The object to search for.
         * @return The index of the object or -1.
         */
        public int indexOf(CosBase obj)
        {
            int retval = -1;
            for (int i = 0; retval < 0 && i < size(); i++)
            {
                if (get(i).Equals(obj))
                {
                    retval = i;
                }
            }
            return retval;
        }

        /**
         * This will return the index of the entry or -1 if it is not found.
         * This method will also find references to indirect objects.
         *
         * @param object The object to search for.
         * @return The index of the object or -1.
         */
        public int indexOfObject(CosBase obj)
        {
            int retval = -1;
            for (int i = 0; retval < 0 && i < this.size(); i++)
            {
                CosBase item = this.get(i);
                if (item.Equals(obj))
                {
                    retval = i;
                    break;
                }


                if (item is CosObject && ((CosObject)item).GetObject().Equals(obj))
                {
                    retval = i;
                    break;
                }
            }
            return retval;
        }

        /**
         * This will add null values until the size of the array is at least
         * as large as the parameter.  If the array is already larger than the
         * parameter then nothing is done.
         *
         * @param size The desired size of the array.
         */
        public void growToSize(int size)
        {
            growToSize(size, null);
        }

        /**
         * This will add the object until the size of the array is at least
         * as large as the parameter.  If the array is already larger than the
         * parameter then nothing is done.
         *
         * @param size The desired size of the array.
         * @param object The object to fill the array with.
         */
        public void growToSize(int targetSize, CosBase obj)
        {
            while (size() < targetSize)
            {
                add(obj);
            }
        }

        /**
         * visitor pattern double dispatch method.
         *
         * @param visitor The object to notify when visiting this object.
         * @return any object, depending on the visitor implementation, or null
         * @throws IOException If an error occurs while visiting this object.
         */
        public override object Accept(ICosVisitor visitor)
        {
            return visitor.VisitFromArray(this);
        }

        /**
         * This will take an COSArray of numbers and convert it to a float[].
         *
         * @return This COSArray as an array of float numbers.
         */
        public float[] toFloatArray()
        {
            float[] retval = new float[size()];
            for (int i = 0; i < size(); i++)
            {
                retval[i] = ((ICosNumber)getObject(i)).AsFloat();
            }
            return retval;
        }

        /**
         * Clear the current contents of the COSArray and set it with the float[].
         *
         * @param value The new value of the float array.
         */
        public void setFloatArray(float[] value)
        {
            this.clear();
            foreach (float aValue in value)
            {
                add(new CosFloat(aValue));
            }
        }

        /**
         *  Return contents of COSArray as a Java List.
         *
         *  @return the COSArray as List
         */
        public List<CosBase> toList()
        {
            return new List<CosBase>(objects);
        }
    }
}
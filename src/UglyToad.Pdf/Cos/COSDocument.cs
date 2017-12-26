namespace UglyToad.Pdf.Cos
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using IO;
    using Parser;
    using Parser.Parts;

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
     * This is the in-memory representation of the PDF document.  You need to call
     * close() on this object when you are done using it!!
     *
     * @author Ben Litchfield
     * 
     */
    public class COSDocument : CosBase, IDisposable
    {
        private float version = 1.4f;

        /**
         * Maps ObjectKeys to a COSObject. Note that references to these objects
         * are also stored in COSDictionary objects that map a name to a specific object.
         */
        private readonly Dictionary<CosObjectKey, CosObject> objectPool = new Dictionary<CosObjectKey, CosObject>();

        /**
         * Maps object and generation id to object byte offsets.
         */
        private readonly Dictionary<CosObjectKey, long> xrefTable = new Dictionary<CosObjectKey, long>();
        
        /**
         * Document trailer dictionary.
         */
        public CosDictionary trailer;

        private bool warnMissingClose = true;

        /** 
         * Signal that document is already decrypted. 
         */

        private bool closed = false;
        
        /**
         * This will get the first dictionary object by type.
         *
         * @param type The type of the object.
         *
         * @return This will return an object with the specified type.
         * @throws IOException If there is an error getting the object
         */
        public CosObject getObjectByType(CosName type)
        {
            foreach (CosObject obj in objectPool.Values)
            {
                var realObject = obj.GetObject();
                if (realObject is CosDictionary dic)
                {
                    try
                    {
                        var typeItem = dic.getItem(CosName.TYPE);
                        if (typeItem is CosName objectType)
                        {
                            if (objectType.Equals(type))
                            {
                                return obj;
                            }
                        }
                        else if (typeItem != null)
                        {
                            //LOG.debug("Expected a /Name object after /Type, got '" + typeItem + "' instead");
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        //LOG.warn(e, e);
                    }
                }
            }
            return null;
        }

        /**
         * This will get all dictionary objects by type.
         *
         * @param type The type of the object.
         *
         * @return This will return an object with the specified type.
         * @throws IOException If there is an error getting the object
         */
        public List<CosObject> getObjectsByType(String type)
        {
            return getObjectsByType(CosName.Create(type));
        }

        /**
         * This will get a dictionary object by type.
         *
         * @param type The type of the object.
         *
         * @return This will return an object with the specified type.
         * @throws IOException If there is an error getting the object
         */
        public List<CosObject> getObjectsByType(CosName type)
        {
            var retval = new List<CosObject>();
            foreach (var obj in objectPool.Values)
            {
                var realObject = obj.GetObject();
                if (realObject is CosDictionary dic)
                {
                    try
                    {
                        var typeItem = dic.getItem(CosName.TYPE);
                        if (typeItem is CosName objectType)
                        {
                            if (objectType.Equals(type))
                            {
                                retval.Add(obj);
                            }
                        }
                        else if (typeItem != null)
                        {
                            //LOG.debug("Expected a /Name object after /Type, got '" + typeItem + "' instead");
                        }
                    }
                    catch (InvalidCastException e)
                    {
                        //LOG.warn(e, e);
                    }
                }
            }
            return retval;
        }

        /**
         * Returns the CosObjectKey for a given COS object, or null if there is none.
         * This lookup iterates over all objects in a PDF, which may be slow for large files.
         * 
         * @param object COS object
         * @return key
         */
        public CosObjectKey getKey(CosBase obj)
        {
            foreach (var entry in objectPool)
            {
                if (entry.Value.GetObject() == obj)
                {
                    return entry.Key;
                }
            }

            return null;
        }

        /**
         * This will print contents to stdout.
         */
        public void print()
        {
            foreach (CosObject obj in objectPool.Values)
            {
                Console.WriteLine(obj);
            }
        }

        public decimal Version { get; set; }
        

        public bool IsDecrypted { get; set; }
        /**
         * This will tell if this is an encrypted document.
         *
         * @return true If this document is encrypted.
         */
        public bool isEncrypted()
        {
            var encrypted = false;
            if (trailer != null)
            {
                encrypted = trailer.getDictionaryObject(CosName.ENCRYPT) != null;
            }
            return encrypted;
        }

        /**
         * This will get the encryption dictionary if the document is encrypted or null
         * if the document is not encrypted.
         *
         * @return The encryption dictionary.
         */
        public CosDictionary getEncryptionDictionary()
        {
            return (CosDictionary)trailer.getDictionaryObject(CosName.ENCRYPT);
        }

        /**
         * This will set the encryption dictionary, this should only be called when
         * encrypting the document.
         *
         * @param encDictionary The encryption dictionary.
         */
        public void setEncryptionDictionary(CosDictionary encDictionary)
        {
            trailer.setItem(CosName.ENCRYPT, encDictionary);
        }

        /**
         * This will get the document ID.
         *
         * @return The document id.
         */
        public COSArray getDocumentID()
        {
            return (COSArray)getTrailer().getDictionaryObject(CosName.ID);
        }

        /**
         * This will set the document ID.
         *
         * @param id The document id.
         */
        public void setDocumentID(COSArray id)
        {
            getTrailer().setItem(CosName.ID, id);
        }

        /**
         * This will get the document catalog.
         *
         * @return @return The catalog is the root of the document; never null.
         *
         * @throws IOException If no catalog can be found.
         */
        public CosObject getCatalog()
        {
            CosObject catalog = getObjectByType(CosName.CATALOG);
            if (catalog == null)
            {
                throw new InvalidOperationException("Catalog cannot be found");
            }
            return catalog;
        }

        /**
         * This will get a list of all available objects.
         *
         * @return A list of all objects, never null.
         */
        public List<CosObject> getObjects()
        {
            return objectPool.Values.ToList();
        }

        /**
         * This will get the document trailer.
         *
         * @return the document trailer dict
         */
        public CosDictionary getTrailer()
        {
            return trailer;
        }

        /**
         * // MIT added, maybe this should not be supported as trailer is a persistence construct.
         * This will set the document trailer.
         *
         * @param newTrailer the document trailer dictionary
         */
        public void setTrailer(CosDictionary newTrailer)
        {
            trailer = newTrailer;
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
            return visitor.VisitFromDocument(this);
        }

        /**
         * This will close all storage and delete the tmp files.
         *
         *  @throws IOException If there is an error close resources.
         */
        public void Dispose()
        {
            if (!closed)
            {
                // close all open I/O streams
                foreach (CosObject obj in getObjects())
                {
                    CosBase cosObject = obj.GetObject();
                }
                closed = true;
            }
        }

        /**
         * Returns true if this document has been closed.
         */
        public bool isClosed()
        {
            return closed;
        }

        /**
         * Warn the user in the finalizer if he didn't close the PDF document. The method also
         * closes the document just in case, to avoid abandoned temporary files. It's still a good
         * idea for the user to close the PDF document at the earliest possible to conserve resources.
         * @throws IOException if an error occurs while closing the temporary files
         */
        ~COSDocument()
        {
            if (!closed)
            {
                if (warnMissingClose)
                {
                    //LOG.warn("Warning: You did not close a PDF Document");
                }
                Dispose();
            }
        }

        /**
         * Controls whether this instance shall issue a warning if the PDF document wasn't closed
         * properly through a call to the {@link #close()} method. If the PDF document is held in
         * a cache governed by soft references it is impossible to reliably close the document
         * before the warning is raised. By default, the warning is enabled.
         * @param warn true enables the warning, false disables it.
         */
        public void setWarnMissingClose(bool warn)
        {
            this.warnMissingClose = warn;
        }

        /**
         * This will get an object from the pool.
         *
         * @param key The object key.
         *
         * @return The object in the pool or a new one if it has not been parsed yet.
         *
         * @throws IOException If there is an error getting the proxy object.
         */
        public CosObject getObjectFromPool(CosObjectKey key)
        {
            if (key != null)
            {
                if (objectPool.TryGetValue(key, out var value))
                {
                    return value;
                }
            }

            // this was a forward reference, make "proxy" object
            var obj = new CosObject(null);
            if (key != null)
            {
                obj.SetObjectNumber(key.Number);
                obj.SetGenerationNumber((int)key.Generation);
                objectPool[key] = obj;
            }

            return obj;
        }

        /**
         * Removes an object from the object pool.
         * @param key the object key
         * @return the object that was removed or null if the object was not found
         */
        public CosObject removeObject(CosObjectKey key)
        {
            if (!objectPool.TryGetValue(key, out CosObject result))
            {
                return null;
            }

            objectPool.Remove(key);

            return result;
        }

        /**
         * Populate XRef HashMap with given values.
         * Each entry maps ObjectKeys to byte offsets in the file.
         * @param xrefTableValues  xref table entries to be added
         */
        public void addXRefTable(Dictionary<CosObjectKey, long> xrefTableValues)
        {
            foreach (var value in xrefTableValues)
            {
                xrefTable[value.Key] = value.Value;
            }
        }

        /**
         * Returns the xrefTable which is a mapping of ObjectKeys
         * to byte offsets in the file.
         * @return mapping of ObjectsKeys to byte offsets
         */
        public Dictionary<CosObjectKey, long> getXrefTable()
        {
            return xrefTable;
        }


        public long StartXRef { get; set; }

        public bool IsXRefStream { get; set; }

    }

}

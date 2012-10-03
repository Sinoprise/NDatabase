using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NDatabase.Btree;
using NDatabase.Btree.Impl;
using NDatabase.Odb.Core.BTree;
using NDatabase.Odb.Core.Layers.Layer2.Instance;
using NDatabase.Odb.Core.Layers.Layer2.Meta;
using NDatabase.Odb.Core.Oid;
using NDatabase.Tool.Wrappers;
using NDatabase.Tool.Wrappers.List;
using NDatabase.Tool.Wrappers.Map;

namespace NDatabase.Odb.Core.Layers.Layer1.Introspector
{
    /// <summary>
    ///   The ClassIntrospector is used to introspect classes.
    /// </summary>
    /// <remarks>
    ///   The ClassIntrospector is used to introspect classes. It uses Reflection to extract class information. It transforms a native Class into a ClassInfo (a meta representation of the class) that contains all informations about the class.
    /// </remarks>
    internal static class ClassIntrospector
    {
        private static readonly IDictionary<string, IOdbList<FieldInfo>> Fields =
            new OdbHashMap<string, IOdbList<FieldInfo>>();

        private static readonly IDictionary<string, Type> SystemClasses = new OdbHashMap<string, Type>();

        private static readonly object FieldsAccess = new object();

        static ClassIntrospector()
        {
            FillSystemClasses();
        }

        /// <summary>
        /// </summary>
        /// <param name="clazz"> The class to instrospect </param>
        /// <param name="recursive"> If true, goes does the hierarchy to try to analyse all classes </param>
        /// <returns> </returns>
        public static ClassInfoList Introspect(Type clazz, bool recursive)
        {
            return InternalIntrospect(clazz, recursive, null);
        }

        public static FieldInfo GetField(Type type, string fieldName)
        {
            return type.GetField(fieldName);
        }

        public static IOdbList<FieldInfo> GetAllFields(string fullClassName)
        {
            IOdbList<FieldInfo> result;
            lock (FieldsAccess)
            {
                Fields.TryGetValue(fullClassName, out result);

                if (result != null)
                    return result;
            }

            IDictionary attributesNames = new Hashtable();
            result = new OdbList<FieldInfo>(50);
            var classes = GetSuperClasses(fullClassName, true);

            foreach (var clazz1 in classes)
            {
                var superClassfields =
                    clazz1.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public |
                                     BindingFlags.DeclaredOnly | BindingFlags.Static);
                foreach (var fieldInfo in superClassfields)
                {
                    // Only adds the attribute if it does not exist one with same name
                    if (attributesNames[fieldInfo.Name] == null)
                    {
                        result.Add(fieldInfo);
                        attributesNames[fieldInfo.Name] = fieldInfo.Name;
                    }
                }
            }

            result = RemoveUnnecessaryFields(result);
            
            lock (FieldsAccess)
            {
                Fields[fullClassName] = result;
            }

            attributesNames.Clear();

            return result;
        }

        /// <summary>
        ///   introspect a list of classes This method return the current meta model based on the classes that currently exist in the execution classpath.
        /// </summary>
        /// <remarks>
        ///   introspect a list of classes This method return the current meta model based on the classes that currently exist in the execution classpath. The result will be used to check meta model compatiblity between the meta model that is currently persisted in the database and the meta model currently executing in JVM. This is used b the automatic meta model refactoring
        /// </remarks>
        /// <returns> </returns>
        /// <returns> A map where the key is the class name and the key is the ClassInfo: the class meta representation </returns>
        public static IDictionary<string, ClassInfo> Instrospect(IEnumerable<ClassInfo> classInfos)
        {
            IDictionary<string, ClassInfo> classInfoSet = new Dictionary<string, ClassInfo>();

            foreach (var persistedClassInfo in classInfos)
            {
                var currentClassInfo = GetClassInfo(persistedClassInfo.FullClassName, persistedClassInfo);

                classInfoSet.Add(currentClassInfo.FullClassName, currentClassInfo);
            }

            return classInfoSet;
        }

        public static ClassInfoList Introspect(String fullClassName, bool recursive)
        {
            return Introspect(OdbClassPool.GetClass(fullClassName), true);
        }

        /// <summary>
        ///   Builds a class info from a class and an existing class info <pre>The existing class info is used to make sure that fields with the same name will have
        ///                                                                 the same id</pre>
        /// </summary>
        /// <param name="fullClassName"> The name of the class to get info </param>
        /// <param name="existingClassInfo"> </param>
        /// <returns> A ClassInfo - a meta representation of the class </returns>
        private static ClassInfo GetClassInfo(String fullClassName, ClassInfo existingClassInfo)
        {
            var classInfo = new ClassInfo(fullClassName) {ClassCategory = GetClassCategory(fullClassName)};

            var fields = GetAllFields(fullClassName);
            IOdbList<ClassAttributeInfo> attributes = new OdbList<ClassAttributeInfo>(fields.Count);

            var maxAttributeId = existingClassInfo.MaxAttributeId;
            foreach (var fieldInfo in fields)
            {
                // Gets the attribute id from the existing class info
                var attributeId = existingClassInfo.GetAttributeId(fieldInfo.Name);
                if (attributeId == - 1)
                {
                    maxAttributeId++;
                    // The attibute with field.getName() does not exist in existing class info
                    //  create a new id
                    attributeId = maxAttributeId;
                }
                var fieldClassInfo = !OdbType.GetFromClass(fieldInfo.FieldType).IsNative()
                                         ? new ClassInfo(fieldInfo.FieldType)
                                         : null;

                attributes.Add(new ClassAttributeInfo(attributeId, fieldInfo.Name, fieldInfo.FieldType,
                                                      OdbClassUtil.GetFullName(fieldInfo.FieldType), fieldClassInfo));
            }

            classInfo.Attributes = attributes;
            classInfo.MaxAttributeId = maxAttributeId;

            return classInfo;
        }

        /// <summary>
        ///   Get The list of super classes
        /// </summary>
        /// <returns> The list of super classes </returns>
        private static IEnumerable<Type> GetSuperClasses(string fullClassName, bool includingThis)
        {
            IList<Type> result = new List<Type>();

            var clazz = OdbClassPool.GetClass(fullClassName);

            if (clazz == null)
                return result;

            if (includingThis)
                result.Add(clazz);

            var baseType = clazz.BaseType;

            while (baseType != null && baseType != typeof (Object))
            {
                result.Add(baseType);
                baseType = baseType.BaseType;
            }

            return result;
        }

        private static IOdbList<FieldInfo> RemoveUnnecessaryFields(IOdbList<FieldInfo> fields)
        {
            IOdbList<FieldInfo> fieldsToRemove = new OdbList<FieldInfo>(fields.Count);

            // Remove static fields
            foreach (var fieldInfo in fields)
            {
                // by osmadja
                if (fieldInfo.IsNotSerialized || fieldInfo.IsStatic)
                    fieldsToRemove.Add(fieldInfo);

                //by cristi
                if (fieldInfo.FieldType == typeof (IntPtr))
                    fieldsToRemove.Add(fieldInfo);

                var oattr = fieldInfo.GetCustomAttributes(true);
                var isNonPersistent = oattr.OfType<NonPersistentAttribute>().Any();

                if (isNonPersistent || fieldInfo.IsStatic)
                    fieldsToRemove.Add(fieldInfo);

                // Remove inner class fields
                if (fieldInfo.Name.StartsWith("this$"))
                    fieldsToRemove.Add(fieldInfo);
            }

            fields.RemoveAll(fieldsToRemove);
            return fields;
        }

        private static byte GetClassCategory(Type type)
        {
            return GetClassCategory(OdbClassUtil.GetFullName(type));
        }

        private static byte GetClassCategory(string fullClassName)
        {
            return SystemClasses.ContainsKey(fullClassName)
                       ? ClassInfo.CategorySystemClass
                       : ClassInfo.CategoryUserClass;
        }

        /// <param name="type"> The class to instrospect </param>
        /// <param name="recursive"> If true, goes does the hierarchy to try to analyse all classes </param>
        /// <param name="classInfoList"> map with classname that are being introspected, to avoid recursive calls </param>
        private static ClassInfoList InternalIntrospect(Type type, bool recursive, ClassInfoList classInfoList)
        {
            var fullClassName = OdbClassUtil.GetFullName(type);

            if (classInfoList != null)
            {
                var existingClassInfo = classInfoList.GetClassInfoWithName(fullClassName);
                if (existingClassInfo != null)
                    return classInfoList;
            }

            var classInfo = new ClassInfo(type) {ClassCategory = GetClassCategory(type)};

            if (classInfoList == null)
                classInfoList = new ClassInfoList(classInfo);
            else
                classInfoList.AddClassInfo(classInfo);

            var fields = GetAllFields(fullClassName);
            IOdbList<ClassAttributeInfo> attributes = new OdbList<ClassAttributeInfo>(fields.Count);

            for (var i = 0; i < fields.Count; i++)
            {
                var field = fields[i];

                ClassInfo classInfoWithName;

                if (!OdbType.GetFromClass(field.FieldType).IsNative())
                {
                    if (recursive)
                    {
                        classInfoList = InternalIntrospect(field.FieldType, true, classInfoList);
                        classInfoWithName = classInfoList.GetClassInfoWithName(OdbClassUtil.GetFullName(field.FieldType));
                    }
                    else
                        classInfoWithName = new ClassInfo(OdbClassUtil.GetFullName(field.FieldType));
                }
                else
                    classInfoWithName = null;
                attributes.Add(new ClassAttributeInfo((i + 1), field.Name, field.FieldType,
                                                      OdbClassUtil.GetFullName(field.FieldType), classInfoWithName));
            }
            classInfo.Attributes = attributes;
            classInfo.MaxAttributeId = fields.Count;
            return classInfoList;
        }

        private static void FillSystemClasses()
        {
            SystemClasses.Add(typeof (ClassInfoIndex).FullName, typeof (ClassInfoIndex));
            SystemClasses.Add(typeof (OID).FullName, typeof (OID));
            SystemClasses.Add(typeof (ObjectOID).FullName, typeof (ObjectOID));
            SystemClasses.Add(typeof (ClassOID).FullName, typeof (ClassOID));
            SystemClasses.Add(typeof (OdbBtreeNodeSingle).FullName, typeof (OdbBtreeNodeSingle));
            SystemClasses.Add(typeof (OdbBtreeNodeMultiple).FullName, typeof (OdbBtreeNodeMultiple));
            SystemClasses.Add(typeof (OdbBtreeSingle).FullName, typeof (OdbBtreeSingle));
            SystemClasses.Add(typeof (IBTree).FullName, typeof (IBTree));
            SystemClasses.Add(typeof (IBTreeNodeOneValuePerKey).FullName, typeof (IBTreeNodeOneValuePerKey));
            SystemClasses.Add(typeof (IKeyAndValue).FullName, typeof (IKeyAndValue));
            SystemClasses.Add(typeof (KeyAndValue).FullName, typeof (KeyAndValue));
        }
    }
}

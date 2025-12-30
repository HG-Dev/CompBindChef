using UnityEngine;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace HG.CompBindChef.Collections
{
    /// <summary>
    /// HashSet that can serialize its values as a list
    /// </summary>
    /// <example>
    /// public sealed class MyDictionary : SerializedDictionary&lt;KeyType, ValueType&gt; {}
    /// </example>
    [Serializable]
    [DebuggerDisplay("Count = {Count}")]
    public class SerializedHashSet<TValue> : SerializedHashSet<TValue, TValue>
    {
        /// <summary>
        /// Conversion to serialize a value
        /// </summary>
        /// <param name="val">The value</param>
        /// <returns>The value</returns>
        public override TValue SerializeValue(TValue val) => val;

        /// <summary>
        /// Conversion to serialize a value
        /// </summary>
        /// <param name="val">The value</param>
        /// <returns>The value</returns>
        public override TValue DeserializeValue(TValue val) => val;
    }

    /// <summary>
    /// HashSet that can serialize its values in a different form
    /// </summary>
    /// <typeparam name="TValue">The value type</typeparam>
    /// <typeparam name="TSerializedValue">The type which the key will be serialized to and from</typeparam>
    [Serializable]
    public abstract class SerializedHashSet<TValue, TSerializedValue> : HashSet<TValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<TSerializedValue> m_Values = new List<TSerializedValue>();

        /// <summary>
        /// From <see cref="TValue"/> to <see cref="TSerializedValue"/>
        /// </summary>
        /// <param name="value">The value in <see cref="TValue"/></param>
        /// <returns>The value in <see cref="TSerializedValue"/></returns>
        public abstract TSerializedValue SerializeValue(TValue value);

        /// <summary>
        /// From <see cref="TSerializedValue"/> to <see cref="TValue"/>
        /// </summary>
        /// <param name="serializedValue">The value in <see cref="TSerializedValue"/></param>
        /// <returns>The value in <see cref="TValue"/></returns>
        public abstract TValue DeserializeValue(TSerializedValue serializedValue);

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_Values.Clear();

            foreach (var value in this)
            {
                m_Values.Add(SerializeValue(value));
            }
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            Clear();

            for (int i = 0; i < m_Values.Count; i++)
                Add(DeserializeValue(m_Values[i]));
        }
    }
}
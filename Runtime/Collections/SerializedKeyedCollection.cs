using System;
using System.Collections.Generic;
using UnityEngine;

namespace HG.CompBindChef.Collections
{
    /// <summary>
    /// Dictionary of serializable, keyed values
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TKeyedValue">The value type (which contains a key)</typeparam>
    [Serializable]
    public abstract class SerializedKeyedCollection<TKey, TKeyedValue> : SerializedKeyedCollection<TKey, TKeyedValue, TKeyedValue>
    {
        protected static TKeyedValue KeyedValueIsSerializable(TKeyedValue value) => value;
        protected override Converter<TKeyedValue, TKeyedValue> Serializer => KeyedValueIsSerializable;
        protected override Converter<TKeyedValue, TKeyedValue> Deserializer => KeyedValueIsSerializable;
    }

    /// <summary>
    /// Dictionary that can serialize a keyed value for storage
    /// </summary>
    /// <typeparam name="TKey">The key type</typeparam>
    /// <typeparam name="TKeyedValue">The value type (which contains a key)</typeparam>
    /// <typeparam name="TKeyedValueSerialized">The type which the value will be serialized to and from</typeparam>
    [Serializable]
    public abstract class SerializedKeyedCollection<TKey, TKeyedValue, TKeyedValueSerialized> : Dictionary<TKey, TKeyedValue>, ISerializationCallbackReceiver
    {
        [SerializeField]
        List<TKeyedValueSerialized> values = new();
        
        protected abstract Converter<TKeyedValue, TKeyedValueSerialized> Serializer { get; }
        protected abstract Converter<TKeyedValueSerialized, TKeyedValue> Deserializer { get; }
        public abstract TKey ExtractKey(TKeyedValue valueWithKey);

        /// <summary>
        /// OnBeforeSerialize implementation.
        /// </summary>
        public void OnBeforeSerialize()
        {
            values.Clear();

            foreach (var keyedValue in Values)
            {
                values.Add(Serializer(keyedValue));
            }
        }

        /// <summary>
        /// OnAfterDeserialize implementation.
        /// </summary>
        public void OnAfterDeserialize()
        {
            Clear();

            for (int i = 0; i < values.Count; i++)
            {
                var value = Deserializer(values[i]);
                Add(ExtractKey(value), value);
            }
        }
    }
}
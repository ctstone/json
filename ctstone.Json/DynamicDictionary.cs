using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ctstone.Json
{
    public class DynamicDictionary : DynamicObject, IDictionary<string, object>, IEnumerable<KeyValuePair<string, object>>
    {
        private Dictionary<string, object> _dict;

        public DynamicDictionary()
        {
            _dict = new Dictionary<string, object>();
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            if (!_dict.TryGetValue(binder.Name, out result))
                result = null;
            return true;
        }
        public override bool TrySetMember(SetMemberBinder binder, object value)
        {
            _dict[binder.Name] = value;
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length != 1)
                throw new InvalidOperationException();

            string key = indexes[0] as String;
            _dict[key] = value;
            return true;
        }
        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length != 1)
                throw new InvalidOperationException();

            string key = indexes[0] as String;
            if (!_dict.TryGetValue(key, out result))
                result = null;
            return true;
        }

        #region IDictionary members (required for serialization)
        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(string key, object value)
        {
            _dict.Add(key, value);
        }

        public bool ContainsKey(string key)
        {
            return _dict.ContainsKey(key);
        }

        public ICollection<string> Keys
        {
            get { return _dict.Keys; }
        }

        public bool Remove(string key)
        {
            return _dict.Remove(key);
        }

        public bool TryGetValue(string key, out object value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public ICollection<object> Values
        {
            get { return _dict.Values; }
        }

        public object this[string key]
        {
            get { return _dict[key]; }
            set { _dict[key] = value; }
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _dict.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return _dict.Contains(item);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _dict.Count; }
        }

        public bool IsReadOnly
        {
            get { throw new NotImplementedException(); }
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return _dict.Remove(item.Key);
        }
        #endregion
    }
}

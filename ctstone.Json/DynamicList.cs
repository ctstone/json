using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ctstone.Json
{
    class DynamicList : DynamicObject, IEnumerable<object>
    {
        private List<object> _array;

        public DynamicList()
        {
            _array = new List<object>();
        }

        public override bool TryDeleteIndex(DeleteIndexBinder binder, object[] indexes)
        {
            if (indexes.Length != 1)
                throw new InvalidOperationException();

            int index = (int)indexes[0];
            _array.RemoveAt(index);
            return true;
        }

        public override bool TryGetIndex(GetIndexBinder binder, object[] indexes, out object result)
        {
            if (indexes.Length != 1)
                throw new InvalidOperationException();

            int index = (int)indexes[0];
            result = _array[index];
            return true;
        }

        public override bool TrySetIndex(SetIndexBinder binder, object[] indexes, object value)
        {
            if (indexes.Length != 1)
                throw new InvalidOperationException();

            int index = (int)indexes[0];
            _array[index] = value;
            return true;
        }

        public override bool TryInvokeMember(InvokeMemberBinder binder, object[] args, out object result)
        {
            result = null;
            switch (binder.Name)
            {
                case "Add":
                    if (args.Length != 1)
                        throw new InvalidOperationException();
                    _array.Add(args[0]);
                    break;
                case "Count":
                    if (args.Length != 0)
                        throw new InvalidOperationException();
                    result = _array.Count;
                    break;
            }
            return true;
        }

        public IEnumerator<object> GetEnumerator()
        {
            return _array.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}

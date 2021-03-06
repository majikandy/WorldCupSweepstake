using System;
using System.Collections.Generic;
using System.Linq;
using Stratis.SmartContracts;

namespace WorldCupSweepstake.Tests.TestTools
{
    public class TestSmartContractList<T> : ISmartContractList<T>
    {
        private readonly List<T> internalList;

        public TestSmartContractList()
        {
            this.internalList = new List<T>();
        }
        public void Add(T item)
        {
            this.internalList.Add(item);
        }

        public T GetValue(uint index)
        {
            return this.internalList[(int)index];
        }

        public void SetValue(uint index, T value)
        {
            throw new NotImplementedException();
        }

        public T Get(uint index)
        {
            return this.internalList[(int)index];
        }

        public IEnumerator<T> GetEnumerator()
        {
            return this.internalList.GetEnumerator();
        }

        public uint Count => (uint)this.internalList.Count;

        public T this[uint key]
        {
            get => this.internalList[(int)key];
            set => throw new NotImplementedException();
        }
    }
}
using System.Collections.Generic;
using Stratis.SmartContracts;

namespace WorldCupSweepstake.Tests.TestTools
{
    public class TestSmartContractMapping<T> : ISmartContractMapping<T>
    {
        private readonly Dictionary<string, T> dictionary = new Dictionary<string, T>();

        public void Put(string key, T value)
        {
            this.dictionary[key] = value;
        }

        public T Get(string key)
        {
            return this.dictionary[key];
        }

        public T this[string key]
        {
            get => this.Get(key);
            set => this.Put(key, value);
        }
    }
}
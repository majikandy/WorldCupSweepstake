using System;
using System.Collections.Generic;
using Stratis.SmartContracts;

namespace WorldCupSweepstake.Tests.TestTools
{
    public class InMemoryState : IPersistentState
    {
        private Dictionary<string, Address> addresses = new Dictionary<string, Address>();
        private Dictionary<string, ulong> uint64s = new Dictionary<string, ulong>();
        private Dictionary<string, bool> bools = new Dictionary<string, bool>();
        private Dictionary<string, TestSmartContractMapping<ulong>> uint64mappings = new Dictionary<string, TestSmartContractMapping<ulong>>();
        private Dictionary<string, string> strings = new Dictionary<string, string>();
        private Dictionary<string, uint> uint32s = new Dictionary<string, uint>();
        private Dictionary<string, TestSmartContractMapping<Address>> addressMappings = new Dictionary<string, TestSmartContractMapping<Address>>();
        private Dictionary<string, TestSmartContractList<Address>> addressLists = new Dictionary<string, TestSmartContractList<Address>>();
        private Dictionary<string, TestSmartContractList<string>> stringLists = new Dictionary<string, TestSmartContractList<string>>();
        private Dictionary<string, TestSmartContractList<ulong>> uint64Lists = new Dictionary<string, TestSmartContractList<ulong>>();

        public byte GetByte(string key)
        {
            throw new NotImplementedException();
        }

        public byte[] GetByteArray(string key)
        {
            throw new NotImplementedException();
        }

        public char GetChar(string key)
        {
            throw new NotImplementedException();
        }

        public Address GetAddress(string key)
        {
            return this.addresses.ContainsKey(key) ? this.addresses[key] : default(Address);
        }

        public bool GetBool(string key)
        {
            return this.bools[key];
        }

        public int GetInt32(string key)
        {
            throw new NotImplementedException();
        }

        public uint GetUInt32(string key)
        {
            return this.uint32s.ContainsKey(key) ? this.uint32s[key] : default(uint);
        }

        public long GetInt64(string key)
        {
            throw new NotImplementedException();
        }

        public ulong GetUInt64(string key)
        {
            return this.uint64s.ContainsKey(key) ? this.uint64s[key] : 0ul;
        }

        public string GetString(string key)
        {
            return this.strings.ContainsKey(key) ? this.strings[key] : string.Empty;
        }

        public sbyte GetSbyte(string key)
        {
            throw new NotImplementedException();
        }

        T IPersistentState.GetStruct<T>(string key)
        {
            throw new NotImplementedException();
        }

        public void SetByte(string key, byte value)
        {
            throw new NotImplementedException();
        }

        public void SetByteArray(string key, byte[] value)
        {
            throw new NotImplementedException();
        }

        public void SetChar(string key, char value)
        {
            throw new NotImplementedException();
        }

        public void SetAddress(string key, Address value)
        {
            this.addresses[key] = value;
        }

        public void SetBool(string key, bool value)
        {
            this.bools[key] = value;
        }

        public void SetInt32(string key, int value)
        {
            throw new NotImplementedException();
        }

        public void SetUInt32(string key, uint value)
        {
            this.uint32s[key] = value;
        }

        public void SetInt64(string key, long value)
        {
            throw new NotImplementedException();
        }

        public void SetUInt64(string key, ulong value)
        {
            this.uint64s[key] = value;
        }

        public void SetString(string key, string value)
        {
            this.strings[key] = value;
        }

        public void SetSByte(string key, sbyte value)
        {
            throw new NotImplementedException();
        }

        void IPersistentState.SetStruct<T>(string key, T value)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<byte> GetByteMapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<byte[]> GetByteArrayMapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<char> GetCharMapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<Address> GetAddressMapping(string name)
        {
            if (!this.addressMappings.ContainsKey(name))
                this.addressMappings.Add(name, new TestSmartContractMapping<Address>());

            return this.addressMappings[name];
        }

        public ISmartContractMapping<bool> GetBoolMapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<int> GetInt32Mapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<uint> GetUInt32Mapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<long> GetInt64Mapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<ulong> GetUInt64Mapping(string name)
        {
            if (!this.uint64mappings.ContainsKey(name))
            {
                this.uint64mappings[name] = new TestSmartContractMapping<ulong>();
            }

            return this.uint64mappings[name];
        }

        public ISmartContractMapping<string> GetStringMapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<sbyte> GetSByteMapping(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractMapping<T> GetStructMapping<T>(string name) where T : struct
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<byte> GetByteList(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<byte[]> GetByteArrayList(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<char> GetCharList(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<Address> GetAddressList(string name)
        {
            if (!this.addressLists.ContainsKey(name))
            {
                this.addressLists.Add(name, new TestSmartContractList<Address>());
            }
            return this.addressLists[name];
        }

        public ISmartContractList<bool> GetBoolList(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<int> GetInt32List(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<uint> GetUInt32List(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<long> GetInt64List(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<ulong> GetUInt64List(string name)
        {
            if (!this.uint64Lists.ContainsKey(name))
            {
                this.uint64Lists.Add(name, new TestSmartContractList<ulong>());
            }
            return this.uint64Lists[name];
        }

        public ISmartContractList<string> GetStringList(string name)
        {
            if (!this.stringLists.ContainsKey(name))
            {
                this.stringLists.Add(name, new TestSmartContractList<string>());
            }
            return this.stringLists[name];
        }

        public ISmartContractList<sbyte> GetSByteList(string name)
        {
            throw new NotImplementedException();
        }

        public ISmartContractList<T> GetStructList<T>(string name) where T : struct
        {
            throw new NotImplementedException();
        }
    }
}
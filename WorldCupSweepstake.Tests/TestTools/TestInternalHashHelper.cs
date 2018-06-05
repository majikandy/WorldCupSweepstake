using System.Text;
using Stratis.SmartContracts;

namespace WorldCupSweepstake.Tests.TestTools
{
    public class TestInternalHashHelper : IInternalHashHelper
    {
        public byte[] Keccak256(byte[] toHash)
        {
            return Encoding.ASCII.GetBytes("707d7a2f11266609dac44fcded84b1d835d3439bd66c66e92b814a8e89bb7e3b");
        }
    }
}
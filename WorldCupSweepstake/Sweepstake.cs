using System.Linq;
using Stratis.SmartContracts;

public class Sweepstake : SmartContract
{
    public virtual Address Owner
    {
        get
        {
            return PersistentState.GetAddress("Owner");
        }
        set
        {
            PersistentState.SetAddress("Owner", value);
        }
    }

    public ulong EndBlock
    {
        get
        {
            return PersistentState.GetUInt64("EndBlock");
        }
        set
        {
            PersistentState.SetUInt64("EndBlock", value);
        }
    }

    public Address HighestBidder
    {
        get
        {
            return PersistentState.GetAddress("HighestBidder");
        }
        set
        {
            PersistentState.SetAddress("HighestBidder", value);
        }
    }

    public ulong HighestBid
    {
        get
        {
            return PersistentState.GetUInt64("HighestBid");
        }
        set
        {
            PersistentState.SetUInt64("HighestBid", value);
        }
    }

    public bool HasEnded
    {
        get
        {
            return PersistentState.GetBool("HasEnded");
        }
        set
        {
            PersistentState.SetBool("HasEnded", value);
        }
    }

    public string Teams
    {
        get
        {
            return PersistentState.GetString("Teams");
        }
        set
        {
            PersistentState.SetString("Teams", value);
        }
    }

    public uint EntryFee
    {
        get
        {
            return PersistentState.GetUInt32("EntryFee");
        }
        set
        {
            PersistentState.SetUInt32("EntryFee", value);
        }
    }

    public uint FirstPrize
    {
        get
        {
            return PersistentState.GetUInt32("FirstPrize");
        }
        set
        {
            PersistentState.SetUInt32("FirstPrize", value);
        }
    }

    public uint SecondPrize
    {
        get
        {
            return PersistentState.GetUInt32("SecondPrize");
        }
        set
        {
            PersistentState.SetUInt32("SecondPrize", value);
        }
    }

    public uint ThirdPrize
    {
        get
        {
            return PersistentState.GetUInt32("ThirdPrize");
        }
        set
        {
            PersistentState.SetUInt32("ThirdPrize", value);
        }
    }


    public ISmartContractMapping<Address> AssignedTeams
    {
        get { return PersistentState.GetAddressMapping("AssignedTeams"); }
    }

    public Sweepstake(ISmartContractState smartContractState, string teams, uint entryFee, uint firstPrize, uint secondPrize, uint thirdPrize)
        : base(smartContractState)
    {
        Owner = Message.Sender;
        Teams = teams;
        EntryFee = entryFee;
        FirstPrize = firstPrize;
        SecondPrize = secondPrize;
        ThirdPrize = thirdPrize;
    }

    public string PickTeam(string senderAddress)
    {
        var teams = this.Teams.Split(",");
        var potentialIndex = (senderAddress[1] - 1) % teams.Length;

        while (AssignedTeams[potentialIndex.ToString()] != default(Address))
        {
            potentialIndex = (potentialIndex + 1) % teams.Length;
        }

        AssignedTeams[potentialIndex.ToString()] = Message.Sender;
        return teams[potentialIndex].Trim();

    }
}

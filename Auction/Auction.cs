﻿using Stratis.SmartContracts;

public class Auction : SmartContract
{
    public virtual Address Owner {
        get
        {
            return PersistentState.GetAddress("Owner");
        }
        set
        {
            PersistentState.SetAddress("Owner", value);
        }
    }

    public ulong EndBlock {
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

    public ISmartContractMapping<ulong> ReturnBalances
    {
        get
        {
            return PersistentState.GetUInt64Mapping("ReturnBalances");
        }
    }

    public Auction(ISmartContractState smartContractState, ulong durationBlocks)
        : base(smartContractState)
    {
        Owner = Message.Sender;
        EndBlock = Block.Number + durationBlocks;
        HasEnded = false;
    }

    public void Bid()
    {
        Assert(Block.Number < EndBlock);
        Assert(Message.Value > HighestBid);

        //don't understand this - this appears to only work for storing the value for the second bidder onwards?
        if (HighestBid > 0)
        {
            ReturnBalances[HighestBidder] = HighestBid;
        }
        HighestBidder = Message.Sender;
        HighestBid = Message.Value;
    }

    public bool Withdraw()
    {
        var amount = ReturnBalances[Message.Sender];
        Assert(amount > 0);
        ReturnBalances[Message.Sender] = 0;

        var transferResult = TransferFunds(Message.Sender, amount);

        if (!transferResult.Success)
            ReturnBalances[Message.Sender] = amount;
        return transferResult.Success;
    }

    public void AuctionEnd()
    {
        Assert(Block.Number >= EndBlock);
        Assert(!HasEnded);
        HasEnded = true;
        TransferFunds(Owner, HighestBid);
    }
}
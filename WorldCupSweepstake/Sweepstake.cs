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

    public ISmartContractList<Address> Players
    {
        get { return PersistentState.GetAddressList("AssignedTeams"); }
    }

    public ISmartContractList<string> AssignedTeams
    {
        get { return PersistentState.GetStringList("AssignedTeams"); }
    }

    public string FirstPlacePlayer
    {
        get
        {
            return PersistentState.GetString("FirstPlacePlayer");
        }
        set
        {
            PersistentState.SetString("FirstPlacePlayer", value);
        }
    }
    public string SecondPlacePlayer
    {
        get
        {
            return PersistentState.GetString("SecondPlacePlayer");
        }
        set
        {
            PersistentState.SetString("SecondPlacePlayer", value);
        }
    }
    public string ThirdPlacePlayer
    {
        get
        {
            return PersistentState.GetString("ThirdPlacePlayer");
        }
        set
        {
            PersistentState.SetString("ThirdPlacePlayer", value);
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

    public string TeamsCsv
    {
        get { return PersistentState.GetString("TeamsCsv"); }
        set { PersistentState.SetString("TeamsCsv", value); }
    }

    public Sweepstake(ISmartContractState smartContractState, string teams, uint entryFee, uint firstPrize, uint secondPrize, uint thirdPrize)
        : base(smartContractState)
    {
        Owner = Message.Sender;
        TeamsCsv = teams;
        EntryFee = entryFee;
        FirstPrize = firstPrize;
        SecondPrize = secondPrize;
        ThirdPrize = thirdPrize;
    }


    public void JoinGame()
    {
        var sender = Message.Sender;
        Assert(this.Players.Count < this.TeamsCsv.Split(",").Length);

        this.Players.Add(sender);

        if (this.Players.Count == this.TeamsCsv.Split(",").Length)
        {
            AssignTeams();
        }
    }

    private void AssignTeams()
    {
        uint numberOfTeams = (uint)this.TeamsCsv.Split(",").Length;

        int hashSum = 0;

        for (uint playerIndex = 0; playerIndex < numberOfTeams; playerIndex++)
        {
            var addressHash = this.Players[playerIndex] + this.Players[(playerIndex + 1) % numberOfTeams] + Block.Number;

            foreach (var letter in addressHash)
            {
                hashSum += letter;
            }
        }

        var shifterNumber = hashSum % (TeamsCsv.LastIndexOf(",") - 1); // find random point in the string

        var randomCommaIndex = TeamsCsv.IndexOf(',', shifterNumber);
        var stringBeforeComma = TeamsCsv.Substring(0, randomCommaIndex);
        var stringAfterComma = TeamsCsv.Substring(randomCommaIndex + 1);
        var newResult = stringAfterComma + "," + stringBeforeComma;

        var teamsRandomised = newResult.Split(",");
        foreach (var team in teamsRandomised)
        {
            AssignedTeams.Add(team);
        }
    }

    public void DeclareResult(string winningTeam, string secondPlace, string thirdPlace)
    {
        Assert(Message.Sender == Owner);

        var winner = default(Address);
        var second = default(Address);
        var third = default(Address);

        for (uint i = 0; i < AssignedTeams.Count; i++)
        {
            var team = AssignedTeams[i];

            if (team == winningTeam)
            {
                FirstPlacePlayer = Players[i] + " : " + AssignedTeams[i] + " : " + FirstPrize;
                winner = Players[i];
            }

            if (team == secondPlace)
            {
                SecondPlacePlayer = Players[i] + " : " + AssignedTeams[i] + " : " + SecondPrize;
                second = Players[i];
            }

            if (team == thirdPlace)
            {
                ThirdPlacePlayer = Players[i] + " : " + AssignedTeams[i] + " : " + ThirdPrize;
                third = Players[i];
            }
        }

        Assert(!string.IsNullOrWhiteSpace(FirstPlacePlayer));
        Assert(!string.IsNullOrWhiteSpace(SecondPlacePlayer));
        Assert(!string.IsNullOrWhiteSpace(ThirdPlacePlayer));

        TransferFunds(winner, FirstPrize);
        TransferFunds(second, SecondPrize);
        TransferFunds(third, ThirdPrize);
    }


}

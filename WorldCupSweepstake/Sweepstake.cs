using Stratis.SmartContracts;

public class Sweepstake : SmartContract
{
    private const ulong SatoshiMuliplier = 100000000; //satoshis

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

    public string Result
    {
        get
        {
            return PersistentState.GetString("Result");
        }
        set
        {
            PersistentState.SetString("Result", value);
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

    public ulong EntryFeeSatoshis
    {
        get
        {
            return PersistentState.GetUInt64("EntryFeeSatoshis");
        }
        set
        {
            PersistentState.SetUInt64("EntryFeeSatoshis", value);
        }
    }

    public ulong FirstPrizeSatoshis
    {
        get
        {
            return PersistentState.GetUInt64("FirstPrizeSatoshis");
        }
        set
        {
            PersistentState.SetUInt64("FirstPrizeSatoshis", value);
        }
    }

    public ulong SecondPrizeSatoshis
    {
        get
        {
            return PersistentState.GetUInt64("SecondPrizeSatoshis");
        }
        set
        {
            PersistentState.SetUInt64("SecondPrizeSatoshis", value);
        }
    }
     
    public ulong ThirdPrizeSatoshis
    {
        get
        {
            return PersistentState.GetUInt64("ThirdPrizeSatoshis");
        }
        set
        {
            PersistentState.SetUInt64("ThirdPrizeSatoshis", value);
        }
    }

    public string TeamsCsv
    {
        get { return PersistentState.GetString("TeamsCsv"); }
        set { PersistentState.SetString("TeamsCsv", value); }
    }

    public Sweepstake(ISmartContractState smartContractState, string teams, uint entryFeeStrats, uint firstPrizeStrats, uint secondPrizeStrats, uint thirdPrizeStrats)
        : base(smartContractState)
    {
        Assert((ulong)teams.Split(",").Length * entryFeeStrats == (firstPrizeStrats + secondPrizeStrats + thirdPrizeStrats));

        Owner = Message.Sender;
        TeamsCsv = teams.Trim().Trim(',');
        EntryFeeSatoshis = entryFeeStrats * SatoshiMuliplier;
        FirstPrizeSatoshis = firstPrizeStrats * SatoshiMuliplier;
        SecondPrizeSatoshis = secondPrizeStrats * SatoshiMuliplier;
        ThirdPrizeSatoshis = thirdPrizeStrats * SatoshiMuliplier;
    }

    public void JoinGame()
    {
        var sender = Message.Sender;
        Assert(this.Players.Count < this.TeamsCsv.Split(",").Length);

        Assert(Message.Value * SatoshiMuliplier == this.EntryFeeSatoshis);

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
            AssignedTeams.Add(team.Trim());
        }
    }

    public void DeclareResult(string winningTeam, string secondPlace, string thirdPlace)
    {
        Assert(Message.Sender == Owner);

        uint winner = uint.MaxValue;
        var second = uint.MaxValue;
        var third = uint.MaxValue;

        for (uint i = 0; i < AssignedTeams.Count; i++)
        {
            var team = AssignedTeams[i];

            if (team == winningTeam)
            {
                winner = i;
            }

            if (team == secondPlace)
            {
                second = i;
            }

            if (team == thirdPlace)
            {
                third = i;
            }
        }

        Result = 
            $"{Players[winner]} : {AssignedTeams[winner]} : {(FirstPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}\r\n" +
            $"{Players[second]} : {AssignedTeams[second]} : {(SecondPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}\r\n" +
            $"{Players[third]} : {AssignedTeams[third]} : {(ThirdPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}";

        Assert(winner != uint.MaxValue);
        Assert(second != uint.MaxValue);
        Assert(third != uint.MaxValue);

        TransferFunds(Players[winner], FirstPrizeSatoshis);
        TransferFunds(Players[second], SecondPrizeSatoshis);
        TransferFunds(Players[third], ThirdPrizeSatoshis);
    }

    public string Currency(Address address)
    {
        return address.ToString().StartsWith("S") ? "STRAT" : "SC-TSTRAT";
    }
}
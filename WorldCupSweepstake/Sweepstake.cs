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
        get { return PersistentState.GetAddressList("Players"); }
    }

    public string PlayersCsv
    {
        get
        {
            return PersistentState.GetString("PlayersCsv");
        }
        set
        {
            PersistentState.SetString("PlayersCsv", value);
        }
    }

    public string PlayersNickNames
    {
        get
        {
            return PersistentState.GetString("PlayersNickNames");
        }
        set
        {
            PersistentState.SetString("PlayersNickNames", value);
        }
    }

    public ISmartContractList<string> AssignedTeams
    {
        get { return PersistentState.GetStringList("AssignedTeams"); }
    }

    public string AssignedTeamsCsv
    {
        get
        {
            return PersistentState.GetString("AssignedTeamsCsv");
        }
        set
        {
            PersistentState.SetString("AssignedTeamsCsv", value);
        }
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
        get
        {
            return PersistentState.GetString("TeamsCsv");
        }
        set
        {
            PersistentState.SetString("TeamsCsv", value);
        }
    }

    public Sweepstake(ISmartContractState smartContractState, string teams, uint entryFeeStrats, uint firstPrizeStrats, uint secondPrizeStrats, uint thirdPrizeStrats)
        : base(smartContractState)
    {
        CleanInput(ref teams);
        Assert((ulong)teams.Split(",").Length * entryFeeStrats == (firstPrizeStrats + secondPrizeStrats + thirdPrizeStrats));

        Owner = Message.Sender;
        
        TeamsCsv = teams;
        EntryFeeSatoshis = entryFeeStrats * SatoshiMuliplier;
        FirstPrizeSatoshis = firstPrizeStrats * SatoshiMuliplier;
        SecondPrizeSatoshis = secondPrizeStrats * SatoshiMuliplier;
        ThirdPrizeSatoshis = thirdPrizeStrats * SatoshiMuliplier;
    }

    public void JoinGame(string nickname)
    {
        Assert(!nickname.Contains(","));

        var sender = Message.Sender;
        Assert(this.Players.Count < this.TeamsCsv.Split(",").Length);

        Assert(Message.Value == this.EntryFeeSatoshis);

        this.Players.Add(sender);
        this.PlayersCsv = (PlayersCsv + "," + sender).Trim(',').Trim();
        this.PlayersNickNames = (PlayersNickNames + "," + nickname).Trim(',').Trim();

        if (GameIsFull())
        {
            AssignTeams();
        }
    }

    private void AssignTeams()
    {
        var numberOfTeams = (uint)this.TeamsCsv.Split(",").Length;

        var hashSum = 0;

        var players = PlayersCsv.Split(",");

        for (uint playerIndex = 0; playerIndex < numberOfTeams; playerIndex++)
        {
            var addressHash = players[playerIndex] + players[(playerIndex + 1) % numberOfTeams];

            foreach (var letter in addressHash)
            {
                hashSum += letter;
            }
        }

        var shifterNumber = hashSum % (TeamsCsv.LastIndexOf(",") - 1); // find random point in the string

        var randomCommaIndex = TeamsCsv.IndexOf(',', shifterNumber);
        var stringBeforeComma = TeamsCsv.Substring(0, randomCommaIndex);
        var stringAfterComma = TeamsCsv.Substring(randomCommaIndex + 1);

        var randomisedTeamsCsv = stringAfterComma + "," + stringBeforeComma;

        this.AssignedTeamsCsv = randomisedTeamsCsv;

        foreach (var team in randomisedTeamsCsv.Split(","))
        {
            AssignedTeams.Add(team.Trim());
        }
    }

    public void DeclareResult(string winningTeam, string secondPlace, string thirdPlace)
    {
        Assert(Message.Sender == Owner);

        CleanInput(ref winningTeam);
        CleanInput(ref secondPlace);
        CleanInput(ref thirdPlace);

        var assignedTeams = AssignedTeamsCsv.Split(",");
        var players = PlayersCsv.Split(",");
        var nickNames = PlayersNickNames.Split(",");

        CheckTeamsAreDifferentAndExist(winningTeam, secondPlace, thirdPlace);

        var winner = uint.MaxValue;
        var second = uint.MaxValue;
        var third = uint.MaxValue;

        for (uint i = 0; i < assignedTeams.Length; i++)
        {
            var team = assignedTeams[i];

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
            $"{nickNames[winner]}({players[winner]}) : {assignedTeams[winner]} : {(FirstPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}\r\n" +
            $"{nickNames[second]}({players[second]}) : {assignedTeams[second]} : {(SecondPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}\r\n" +
            $"{nickNames[third]}({players[third]}) : {assignedTeams[third]} : {(ThirdPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}";

        TransferFunds(new Address(players[winner]), FirstPrizeSatoshis);
        TransferFunds(new Address(players[second]), SecondPrizeSatoshis);
        TransferFunds(new Address(players[third]), ThirdPrizeSatoshis);
    }

    private static void CleanInput(ref string toClean)
    {
        toClean = toClean.Trim().Trim(',').ToLower();
    }

    private void CheckTeamsAreDifferentAndExist(string winningTeam, string secondPlace, string thirdPlace)
    {
        Assert(winningTeam != secondPlace);
        Assert(secondPlace != thirdPlace);
        Assert(thirdPlace != winningTeam);

        Assert(TeamsCsv.Contains(winningTeam));
        Assert(TeamsCsv.Contains(secondPlace));
        Assert(TeamsCsv.Contains(thirdPlace));
    }

    public void CancelAndRefund()
    {
        Assert(Message.Sender == Owner);

        foreach (var player in Players)
        {
            TransferFunds(player, EntryFeeSatoshis);
        }

        Result = "Cancelled by owner at block: " + Block.Number + ". Refunds issued.";
    }

    private bool GameIsFull()
    {
        return this.Players.Count == this.TeamsCsv.Split(",").Length;
    }

    private string Currency(Address address)
    {
        return address.ToString().StartsWith("S") ? "STRAT" : "SC-TSTRAT";
    }
}
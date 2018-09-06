using Stratis.SmartContracts;

// ReSharper disable once CheckNamespace
public class Sweepstake : SmartContract
{
    private const ulong SatoshiMuliplier = 100000000;

    private Address Owner
    {
        get => PersistentState.GetAddress("Owner");
        set => PersistentState.SetAddress("Owner", value);
    }

    private ISmartContractList<Address> PlayersAddresses => PersistentState.GetAddressList("PlayersAddresses");
    private ISmartContractList<string> PlayersNickNames => PersistentState.GetStringList("PlayersNickNames");
    private ISmartContractList<string> AssignedTeams => PersistentState.GetStringList("AssignedTeams");
    private ISmartContractList<ulong> PrizesSatoshis => PersistentState.GetUInt64List("PrizesSatoshis");

    private ulong EntryFeeSatoshis
    {
        get => PersistentState.GetUInt64("EntryFeeSatoshis");
        set => PersistentState.SetUInt64("EntryFeeSatoshis", value);
    }

    private string TeamsCsv
    {
        get => PersistentState.GetString("TeamsCsv");
        set => PersistentState.SetString("TeamsCsv", value);
    }

    private string Result
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

    public Sweepstake(ISmartContractState smartContractState, string teams, uint entryFeeStrats, uint firstPrizeStrats, uint secondPrizeStrats, uint thirdPrizeStrats)
        : base(smartContractState)
    {
        teams = teams.Trim().Trim(',').ToLower();
        Assert((ulong)teams.Split(",").Length * entryFeeStrats == (firstPrizeStrats + secondPrizeStrats + thirdPrizeStrats),
            "Sum of the prizes must equal the teams * entryFee.");

        Owner = Message.Sender;
        
        TeamsCsv = teams;
        EntryFeeSatoshis = entryFeeStrats * SatoshiMuliplier;
        PrizesSatoshis.Add(firstPrizeStrats * SatoshiMuliplier);
        PrizesSatoshis.Add(secondPrizeStrats * SatoshiMuliplier);
        PrizesSatoshis.Add(thirdPrizeStrats * SatoshiMuliplier);
    }

    public void JoinGame(string nickname)
    {
        Assert(!nickname.Contains(","), "Cannot have comma in nickname.");
        

        Assert(!GameIsFull(), "The game is already full. No more players can join.");
        Assert(Message.Value == this.EntryFeeSatoshis, "Amount must equal entryFee: " + this.EntryFeeSatoshis/SatoshiMuliplier +", but was: " + Message.Value + ".");
        AssertNoWhitespace(nickname);
        AddPlayer(nickname, Message.Sender);

        if (GameIsFull())
        {
            AssignTeams();
        }
    }

    private void AssertNoWhitespace(string nickname)
    {
        for(var i = 0; i < nickname.Length; i++)
        {
            Assert(!char.IsWhiteSpace(nickname[i]), "Cannot have whitespace in nickname.");
        }
    }

    private void AddPlayer(string nickname, Address sender)
    {
        this.PlayersAddresses.Add(sender);
        this.PlayersNickNames.Add(nickname.Trim());
    }

    private void AssignTeams()
    {
        var shiftNumberLowerThanLastCommaIndex = GetDeterministicRandomishNumber() % (TeamsCsv.LastIndexOf(",") - 1);

        var randomCommaIndex = TeamsCsv.IndexOf(',', shiftNumberLowerThanLastCommaIndex);
        var stringBeforeComma = TeamsCsv.Substring(0, randomCommaIndex);
        var stringAfterComma = TeamsCsv.Substring(randomCommaIndex + 1);

        var reorderedTeams = stringAfterComma + "," + stringBeforeComma;

        foreach (var team in reorderedTeams.Split(","))
        {
            AssignedTeams.Add(team.Trim());
        }
    }

    /// NOTE: Miner and last player could collaborate to manipulate team assignment
    private int GetDeterministicRandomishNumber()
    {
        var numberOfTeams = (uint)this.TeamsCsv.Split(",").Length;

        int randomishNumber = 0;

        randomishNumber = AddMinersAddress(randomishNumber);
        randomishNumber = AddLastPlayersAddress(numberOfTeams, randomishNumber);
        randomishNumber = AddBlockNumber(randomishNumber);

        return randomishNumber;
    }

    private int AddBlockNumber(int randomishNumber)
    {
        randomishNumber = randomishNumber + (int) Block.Number;
        return randomishNumber;
    }

    private int AddLastPlayersAddress(uint numberOfTeams, int randomishNumber)
    {
        var lastPlayer = PlayersAddresses[numberOfTeams - 1];

        for(int i = 0; i < lastPlayer.Value.Length; i++)
        {
            randomishNumber += lastPlayer.Value[i];
        }

        return randomishNumber;
    }

    private int AddMinersAddress(int sum)
    {
        var coinbaseAddress = Block.Coinbase.Value;
        for(int i = 0; i < coinbaseAddress.Length; i++)
        {
            sum += coinbaseAddress[i];
        }

        return sum;
    }

    public void DeclareResult(string winningTeam, string secondPlace, string thirdPlace)
    {
        EnsureOnlyOwnerCalled();

        winningTeam = winningTeam.Trim().Trim(',').ToLower();
        secondPlace = secondPlace.Trim().Trim(',').ToLower();
        thirdPlace = thirdPlace.Trim().Trim(',').ToLower();

        var assignedTeams = AssignedTeams;
        var playersAddresses = PlayersAddresses;

        var nickNames = PlayersNickNames;

        CheckTeamsAreDifferentAndExist(winningTeam, secondPlace, thirdPlace);

        var winner = uint.MaxValue;
        var second = uint.MaxValue;
        var third = uint.MaxValue;

        for (uint i = 0; i < assignedTeams.Count; i++)
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

        ulong FirstPrizeSatoshis = PrizesSatoshis[0];
        ulong SecondPrizeSatoshis = PrizesSatoshis[1];
        ulong ThirdPrizeSatoshis = PrizesSatoshis[2];

        Result = 
            $"{nickNames[winner]}({playersAddresses[winner]}) : {assignedTeams[winner]} : {(FirstPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}\r\n" +
            $"{nickNames[second]}({playersAddresses[second]}) : {assignedTeams[second]} : {(SecondPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}\r\n" +
            $"{nickNames[third]}({playersAddresses[third]}) : {assignedTeams[third]} : {(ThirdPrizeSatoshis / SatoshiMuliplier)} {Currency(Message.ContractAddress)}";

        TransferFunds(new Address(playersAddresses[winner]), FirstPrizeSatoshis);
        TransferFunds(new Address(playersAddresses[second]), SecondPrizeSatoshis);
        TransferFunds(new Address(playersAddresses[third]), ThirdPrizeSatoshis);
    }

    public void CancelAndRefund()
    {
        EnsureOnlyOwnerCalled();

        for (uint i = 0; i < PlayersAddresses.Count; i++)
        {
            TransferFunds(PlayersAddresses[i], EntryFeeSatoshis);
        }

        Result = "Cancelled by owner at block: " + Block.Number + ". Refunds issued.";
    }

    private void EnsureOnlyOwnerCalled()
    {
        Assert(Message.Sender == Owner, $"Someone other than owner attempted to declare result: {Message.Sender}.");
    }

    private void CheckTeamsAreDifferentAndExist(string winningTeam, string secondPlace, string thirdPlace)
    {
        Assert(winningTeam != secondPlace, $"First place and second place were same team: {winningTeam}.");
        Assert(winningTeam != thirdPlace, $"First place and third place were same team: {winningTeam}.");
        Assert(secondPlace != thirdPlace, $"Third place and second place were same team: {secondPlace}.");

        var teams = TeamsCsv.Split(",");

        var winningTeamFound = false;
        var secondTeamFound = false;
        var thirdTeamFound = false;
        for (int i = 0; i < teams.Length; i++)
        {
            if (teams[i] == winningTeam)
                winningTeamFound = true;
            if (teams[i] == secondPlace)
                secondTeamFound = true;
            if (teams[i] == thirdPlace)
                thirdTeamFound = true;
        }

        Assert(winningTeamFound, $"Winning team not present in team list: {winningTeam}.");
        Assert(secondTeamFound, $"Second place team not present in team list: {secondPlace}.");
        Assert(thirdTeamFound, $"Third place team not present in team list: {thirdPlace}.");
    }

    private bool GameIsFull()
    {
        return this.PlayersAddresses.Count == this.TeamsCsv.Split(",").Length;
    }

    private string Currency(Address address)
    {
        return address.ToString().StartsWith("S") ? "STRAT" : "SC-TSTRAT";
    }

    public void StartGameNow()
    {
        EnsureOnlyOwnerCalled();
        Assert(this.PlayersAddresses.Count > 0, "There are no players yet.");

        int luckyPlayerIndex = 0;

        while (PlayersAddresses.Count < TeamsCsv.Split(",").Length)
        {
            AddPlayer(this.PlayersNickNames[(uint)luckyPlayerIndex], this.PlayersAddresses[(uint)luckyPlayerIndex]);
            luckyPlayerIndex = (luckyPlayerIndex+1) % (TeamsCsv.Length-1);
        } 

        AssignTeams();
    }
}
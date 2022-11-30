namespace Lab1.Network
{
    public enum PacketType : uint
    {
        AcceptJoin = 1,
        IsHereAck, //server acknowledges client`s state
        GameStart,
        GameState,
        GameEnd = 5,
        //!server
        RequestJoin = 6,
        IsHere,
        JoinAck,
        GameStartAck,
        PaddlePosition //position of paddle
    }
}

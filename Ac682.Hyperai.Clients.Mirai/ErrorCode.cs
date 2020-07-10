namespace Ac682.Hyperai.Clients.Mirai
{
    public enum ErrorCode
    {
        Normal = 0,
        InvalidAuthKey = 1,
        BotNotFount = 2,
        InvalidSessionKey = 3,
        UnverifiedSession = 4,
        TargetNotFount = 5,
        FileNotFound = 6,
        AccessDenied = 10,
        BotMuted = 20,
        MessageTooLong = 30,
        BadRequest = 400
    }
}
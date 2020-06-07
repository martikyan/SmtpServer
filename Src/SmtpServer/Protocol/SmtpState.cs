namespace SmtpServer.Protocol
{
    internal enum SmtpState
    {
        None = 0,
        Initialized = 1,
        WaitingForMail = 2,
        WithinTransaction = 3,
        CanAcceptData = 4,
    }
}
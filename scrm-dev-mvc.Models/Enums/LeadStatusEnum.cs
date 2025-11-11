namespace scrm_dev_mvc.Models.Enums
{
    public enum LeadStatusEnum
    {
        // Initial state
        New = 1,

        // Actively working
        Open = 2,
        Connected = 3,
        Qualified = 1002,

        // "Lost" or "Disqualified" states
        Disqualified = 2002,
        BadTiming = 1003,
        HighPrice = 1004
    }
}

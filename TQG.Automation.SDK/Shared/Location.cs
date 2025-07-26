namespace TQG.Automation.SDK.Shared;


/// Đại diện cho một vị trí trong kệ.
/// </summary>
/// <param name="Floor">Tầng</param>
/// <param name="Rail">Đường Ray (Dãy)</param>
/// <param name="Block">Kệ</param>
public record Location(short Floor, short Rail, short Block);
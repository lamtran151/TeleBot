namespace Webhook.Controllers.Entity
{
    #region ResultAPI
    public class Result
    {
        public bool Success { get; set; }
        public ErrorInfo Error { get; set; }
    }
    public class ErrorInfo
    {
        public string Message { get; set; }
        public int Code { get; set; }
    }
    public class ResultString : Result
    {
        public string Result { get; set; }
    }
    #endregion

    #region Login
    public class ResultLogin : Result
    {
        public LoginDto Result { get; set; }
    }
    public class LoginDto
    {
        public string UserName { get; set; }
        public string Token { get; set; }
    }
    #endregion

    #region Profile
    public class ProfileDto
    {
        public string SurName { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public decimal Balance { get; set; }
        public string ReferralCode { get; set; }
    }
    public class ResultProfile : Result
    {
        public ProfileDto Result { get; set; }
    }
    #endregion

    #region Promotion
    public class ResultPromotion : Result
    {
        public List<Promotion> result { get; set; }
    }
    public class Promotion
    {
        public string name { get; set; }
        public string content { get; set; }
        public bool status { get; set; }
        public string promotionType { get; set; }
        public string type { get; set; }
        public int displayNumber { get; set; }
        public string urlImage { get; set; }
        public bool isActive { get; set; }
        public decimal amountBuy { get; set; }
        public string typeName { get; set; }
        public bool fixedAmount { get; set; }
        public string parameter { get; set; }
        public List<object> detailPromotion { get; set; }
        public string condition { get; set; }

    }
    #endregion

    #region Bank
    public class Bank
    {
        public int Id { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string BankName { get; set; }
        public string BankShortName { get; set; }
        public string DisplayName { get; set; }
        public decimal? MaximumDeposit { get; set; }
        public decimal? MinimumDeposit { get; set; }
    }

    public class ListBank
    {
        public List<Bank> AgentBank { get; set; }
        public List<Bank> PlayerBank { get; set; }
    }

    public class ResultBank : Result
    {
        public ListBank Result { get; set; }
    }
    #endregion

    #region Game
    public class GameTypeDto
    {
        public string game_type { get; set; }
        public string game_type_name { get; set; }
        public List<PlatformDto> platforms { get; set; }
    }

    public class GameDetailDto
    {
        public string game_code { get; set; }
        public string game_name_en { get; set; }
        public string platform { get; set; }
        public string game_type { get; set; }
        public decimal rtp { get; set; }
        public string imageURL { get; set; }
    }

    public class PlatformDto
    {
        public string platform { get; set; }
        public string platform_name { get; set; }
    }
    public class ResultListGame : Result
    {
        public ListGame result { get; set; }
    }
    public class ListGame
    {
        public List<GameTypeDto> gameType { get; set; }
        public List<GameDetailDto> gameList { get; set; }
    }
    #endregion



    public class PromotionJson
    {
        public string Approval { get; set; }
        public string BonusType { get; set; }
        public double Bonus { get; set; }
        public double MaxBonus { get; set; }
        public double MaxWithdraw { get; set; }
        public double MinDeposit { get; set; }
        public int TurnOver { get; set; }
        public double TerminalAmount { get; set; }
        public string Frequency { get; set; }
        public object TranslatePromotion { get; set; }
    }

    public class ResultLanguage : Result
    {
        public LanguageDto Result { get; set; }
    }

    public class LanguageDto
    {
        public CurrentCulture CurrentCulture { get; set; }
        public List<CurrentCulture> Languages { get; set; }
        public object Values { get; set; }
    }

    public class CurrentCulture
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
    }


}

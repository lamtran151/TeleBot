using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dto
{
    public class UserInfo
    {
        public string SurName { get; set; }
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BirthDay { get; set; }
        public decimal Balance { get; set; }
        public string ReferralCode { get; set; }
    }

    public class ResultGame
    {
        public bool Success { get; set; }
        public Game result { get; set; }
    }

    public class ResultInfo
    {
        public bool Success { get; set; }
        public UserInfo result { get; set; }
    }
    public class ResultPromotion
    {
        public bool Success { get; set; }
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
    public class Game
    {
        public List<GameType> gameType { get; set; }
        public List<GameList> gameList { get; set; }
    }

    public class GameType
    {
        public string game_type { get; set; }
        public string game_type_name { get; set; }
        public List<Platform> platforms { get; set; }
    }

    public class GameList
    {
        public string game_code { get; set; }
        public string game_name_en { get; set; }
        public string game_type { get; set; }
        public string platform { get; set; }
        public string imageURL { get; set; }
        public decimal rtp { get; set; }
    }

    public class Platform
    {
        public string platform { get; set; }
        public string platform_name { get; set; }
    }

    public class Login
    {
        public string UserName { get; set; }
        public string Token { get; set; }
    }

    public class ResultCommon
    {
        public bool Success { get; set; }
        public ErrorInfo Error { get; set; }
    }

    public class ErrorInfo
    {
        public string Message { get; set; }
        public int Code { get; set; }
    }

    public class ResultLogin : ResultCommon
    {
        public Login Result { get; set; }
    }

    public class ResultProfile : ResultCommon
    {
        public UserInfo Result { get; set; }
    }

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

    public class ResultBank : ResultCommon
    {
        public ListBank Result { get; set; }
    }

    public class ResultGameDetail : ResultCommon
    {
        public string Result { get; set; }
    }

    public class ResultLanguage : ResultCommon
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dto
{
    public class UserInfo
    {
        public string UserName { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime BirthDay { get; set; }
        public decimal Balance { get; set; }
    }

    public class Result
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
        public string platform { get; set; }
    }

    public class Platform
    {
        public string platform { get; set; }
        public string platform_name { get; set; }
    }

}

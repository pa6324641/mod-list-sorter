using JiangXiaoMod.Code.Cards.CardModels;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Cards;
using MegaCrit.Sts2.Core.CardSelection; // 引用 CardSelectorPrefs
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using BaseLib.Utils;
using MegaCrit.Sts2.Core.Models.CardPools;
using MegaCrit.Sts2.Core.Entities.Players;

namespace JiangXiaoMod.Code.Cards.Token;

[Pool(typeof(TokenCardPool))]
public class StrengthenToken : JiangXiaoCardModel
{
    public const string CardId = "JIANGXIAOMOD-STRENGTHEN_TOKEN";

    public StrengthenToken() : base(2, CardType.Skill, CardRarity.Token, TargetType.None)
    {
    }

    public override HashSet<CardKeyword> CanonicalKeywords => [CardKeyword.Exhaust];

    protected override async Task OnPlay(PlayerChoiceContext choiceContext, CardPlay cardPlay)
    {
        if (Owner == null) return;

        // 1. 參考官方 SmithRestSiteOption 的設定方式
        // 使用官方標題，數量設為 1
        CardSelectorPrefs prefs = new CardSelectorPrefs(CardSelectorPrefs.UpgradeSelectionPrompt, 1)
        {
            Cancelable = false,            // 作為卡牌效果，通常不允許中途取消
            RequireManualConfirmation = true // 需要玩家點擊確認
        };

        // 2. 從牌組獲取選擇
        var selection = await CardSelectCmd.FromDeckForUpgrade(Owner, prefs);

        // 3. 遍歷結果進行升級
        if (selection != null && selection.Any())
        {
            foreach (CardModel item in selection)
            {
                // [官方規範] 傳入 CardPreviewStyle.None 以執行正式升級
                CardCmd.Upgrade(item, CardPreviewStyle.None);
            }
        }
    }
    protected override void ApplyRankLogic(Player? player, int skillRank)
    {
        
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NuciXNA.Gui;
using NuciXNA.Gui.Screens;
using NuciXNA.Input;
using NuciXNA.Primitives;

using Narivia.GameLogic.Enumerations;
using Narivia.GameLogic.Exceptions;
using Narivia.GameLogic.Events;
using Narivia.GameLogic.GameManagers;
using Narivia.Gui.GuiElements;
using Narivia.Gui.GuiElements.Enumerations;
using Narivia.Models;

namespace Narivia.Gui.Screens
{
    /// <summary>
    /// Gameplay screen.
    /// </summary>
    public class GameplayScreen : Screen
    {
        IGameManager game;

        GuiWorldmap gameMap;
        GuiAdministrationBar administrationBar;
        GuiInfoBar infoBar;
        GuiFactionBar factionBar;
        GuiProvincePanel provincePanel;
        GuiNotificationBar notificationBar;
        GuiRecruitmentPanel recruitmentPanel;
        GuiBuildingPanel buildingPanel;

        Dictionary<string, int> troopsOld;
        Dictionary<string, int> relationsOld;
        int provincesOld, holdingsOld, wealthOld, incomeOld, outcomeOld, recruitmentOld;

        /// <summary>
        /// Loads the content.
        /// </summary>
        public override void LoadContent()
        {
            string initialWorldId = "narivia";
            string initialFactionId = "f_caravenna";

            game = new GameManager(initialWorldId, initialFactionId);
            game.LoadContent();

            administrationBar = new GuiAdministrationBar
            {
                Location = Point2D.Empty,
            };
            infoBar = new GuiInfoBar(game)
            {
                Location = new Point2D(ScreenManager.Instance.Size.Width - 166, 0),
                Size = new Size2D(166, 82)
            };
            factionBar = new GuiFactionBar(game)
            {
                Location = new Point2D((ScreenManager.Instance.Size.Width - 242) / 2, 0),
                Size = new Size2D(242, 94)
            };
            gameMap = new GuiWorldmap(game)
            {
                Size = ScreenManager.Instance.Size
            };
            notificationBar = new GuiNotificationBar(game)
            {
                Location = new Point2D(
                    ScreenManager.Instance.Size.Width - 48,
                    infoBar.Size.Height),
                Size = new Size2D(48, ScreenManager.Instance.Size.Height - infoBar.Size.Height)
            };
            provincePanel = new GuiProvincePanel(game)
            {
                Location = new Point2D(0, 296)
            };

            if (ScreenArgs != null && ScreenArgs.Length >= 2)
            {
                initialWorldId = ScreenArgs[0];
                initialFactionId = ScreenArgs[1];
            }

            recruitmentPanel = new GuiRecruitmentPanel(game);
            buildingPanel = new GuiBuildingPanel(game);

            recruitmentPanel.Hide();
            buildingPanel.Hide();

            troopsOld = new Dictionary<string, int>();
            relationsOld = new Dictionary<string, int>();

            troopsOld = game.GetUnits().ToDictionary(x => x.Name, x => game.GetArmy(game.PlayerFactionId, x.Id).Size);

            foreach (Relation relation in game.GetFactionRelations(game.PlayerFactionId))
            {
                if (relationsOld.ContainsKey(relation.TargetFactionId))
                {
                    relationsOld[relation.TargetFactionId] = relation.Value;
                }
                else
                {
                    relationsOld.Add(relation.TargetFactionId, relation.Value);
                }
            }

            GuiManager.Instance.GuiElements.Add(gameMap);
            GuiManager.Instance.GuiElements.Add(administrationBar);
            GuiManager.Instance.GuiElements.Add(infoBar);
            GuiManager.Instance.GuiElements.Add(factionBar);
            GuiManager.Instance.GuiElements.Add(provincePanel);
            GuiManager.Instance.GuiElements.Add(notificationBar);
            GuiManager.Instance.GuiElements.Add(recruitmentPanel);
            GuiManager.Instance.GuiElements.Add(buildingPanel);

            provincePanel.ProvinceId = game.GetFactionCapital(game.PlayerFactionId).Id;

            base.LoadContent();

            provincePanel.Hide();

            string factionName = game.GetFaction(game.PlayerFactionId).Name;

            NotificationManager.Instance.ShowNotification(
                $"Welcome to {game.GetWorld().Name}",
                $"The era of peace has ended! Old rivalries remerged, and a global war broke out." + Environment.NewLine +
                $"Conquer the world in the name of {factionName}, and secure its place in the golden pages of history!");
            
            gameMap.CentreCameraOnLocation(
                game.GetFactionCentreX(game.PlayerFactionId),
                game.GetFactionCentreY(game.PlayerFactionId));
        }

        /// <summary>
        /// Update the content.
        /// </summary>
        /// <returns>The update.</returns>
        /// <param name="gameTime">Game time.</param>
        public override void Update(GameTime gameTime)
        {
            recruitmentPanel.Location = new Point2D(
                gameMap.Location.X + (gameMap.Size.Width - recruitmentPanel.Size.Width) / 2,
                gameMap.Location.Y + (gameMap.Size.Height - recruitmentPanel.Size.Height) / 2);
            buildingPanel.Location = new Point2D(
                gameMap.Location.X + (gameMap.Size.Width - buildingPanel.Size.Width) / 2,
                gameMap.Location.Y + (gameMap.Size.Height - buildingPanel.Size.Height) / 2);

            base.Update(gameTime);
        }

        /// <summary>
        /// Draw the content on the specified spriteBatch.
        /// </summary>
        /// <returns>The draw.</returns>
        /// <param name="spriteBatch">Sprite batch.</param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);
        }

        protected override void RegisterEvents()
        {
            base.RegisterEvents();

            game.PlayerProvinceAttacked += game_OnPlayerProvinceAttacked;
            game.FactionDestroyed += game_OnFactionDestroyed;
            game.FactionRevived += game_OnFactionRevived;
            game.FactionWon += game_OnFactionWon;

            gameMap.Clicked += GameMap_Clicked;

            infoBar.TurnButtonClicked += SideBar_TurnButtonClicked;
            administrationBar.RecruitButton.Clicked += AdministrationBar_RecruitButtonClicked;
            administrationBar.StatsButton.Clicked += AdministrationBar_StatsButtonClicked;
            provincePanel.AttackButtonClicked += ProvincePanel_AttackButtonClicked;
            provincePanel.BuildButtonClicked += ProvincePanel_BuildButtonClicked;
        }

        void NextTurn()
        {
            notificationBar.Clear();

            game.NextTurn();

            Dictionary<string, int> troopsNew = new Dictionary<string, int>();
            Dictionary<string, int> relationsNew = new Dictionary<string, int>();

            game.GetUnits().ToList().ForEach(u => troopsNew.Add(u.Name, game.GetArmy(game.PlayerFactionId, u.Id).Size));

            foreach (Relation relation in game.GetFactionRelations(game.PlayerFactionId))
            {
                if (relationsNew.ContainsKey(relation.TargetFactionId))
                {
                    relationsNew[relation.TargetFactionId] = relation.Value;
                }
                else
                {
                    relationsNew.Add(relation.TargetFactionId, relation.Value);
                }
            }

            int provincesNew = game.GetFactionProvinces(game.PlayerFactionId).Count();
            int holdingsNew = game.GetFactionHoldings(game.PlayerFactionId).Count();
            int wealthNew = game.GetFaction(game.PlayerFactionId).Wealth;
            int incomeNew = game.GetFactionIncome(game.PlayerFactionId);
            int outcomeNew = game.GetFactionOutcome(game.PlayerFactionId);
            int recruitmentNew = game.GetFactionRecruitment(game.PlayerFactionId);

            string recruitmentBody = string.Empty;
            string relationsBody = string.Empty;
            string turnBody = $"Provinces: {provincesNew} ({(provincesNew - provincesOld).ToString("+0;-#")})" + Environment.NewLine +
                              $"Holdings: {holdingsNew} ({(holdingsNew - holdingsOld).ToString("+0;-#")})" + Environment.NewLine +
                              $"Wealth: {wealthNew} ({(wealthNew - wealthOld).ToString("+0;-#")})" + Environment.NewLine +
                              $"Income: {incomeNew} ({(incomeNew - incomeOld).ToString("+0;-#")})" + Environment.NewLine +
                              $"Outcome: {outcomeNew} ({(outcomeNew - outcomeOld).ToString("+0;-#")})" + Environment.NewLine +
                              $"Militia Recruitment: {recruitmentNew} ({(recruitmentNew - recruitmentOld).ToString("+0;-#")})";

            foreach (string key in troopsNew.Keys)
            {
                recruitmentBody += $"{key}: {troopsNew[key]} ({(troopsNew[key] - troopsOld[key]).ToString("+0;-#")})" + Environment.NewLine;
            }

            foreach (string targetfactionId in relationsNew.Keys.Where(key => relationsNew[key] != relationsOld[key]))
            {
                int delta = relationsNew[targetfactionId] - relationsOld[targetfactionId];

                relationsBody += $"{game.GetFaction(targetfactionId).Name}: {relationsNew[targetfactionId].ToString("+0;-#")} " +
                                 $"({delta.ToString("+0;-#")})" + Environment.NewLine;
            }

            notificationBar.AddNotification(NotificationIcon.TurnReport).Clicked += delegate
            {
                NotificationManager.Instance.ShowNotification($"Turn {game.Turn} Report", turnBody);
            };

            notificationBar.AddNotification(NotificationIcon.RecruitmentReport).Clicked += delegate
            {
                NotificationManager.Instance.ShowNotification($"Recruitment Report", recruitmentBody);
            };

            if (!string.IsNullOrWhiteSpace(relationsBody))
            {
                notificationBar.AddNotification(NotificationIcon.RelationsReport).Clicked += delegate
                {
                    NotificationManager.Instance.ShowNotification($"Relations Report", relationsBody);
                };
            }

            troopsOld = troopsNew;
            relationsOld = relationsNew;

            provincesOld = provincesNew;
            holdingsOld = holdingsNew;
            wealthOld = wealthNew;
            incomeOld = incomeNew;
            outcomeOld = outcomeNew;
            recruitmentOld = recruitmentNew;
        }

        void AttackProvince(string provinceId)
        {
            if (game.GetFactionTroopsAmount(game.PlayerFactionId) < game.GetWorld().MinTroopsPerAttack)
            {
                NotificationManager.Instance.ShowNotification(
                    $"Not enough troops!",
                    $"Sorry!" + Environment.NewLine + Environment.NewLine +
                    $"You do need at least {game.GetWorld().MinTroopsPerAttack} troops to attack any province.");

                return;
            }

            try
            {
                Province province = game.GetProvince(provinceId);

                string provinceName = province.Name;
                string defenderFactionName = game.GetFaction(province.FactionId).Name;

                BattleResult result = game.PlayerAttackProvince(provinceId);

                NextTurn();

                if (result == BattleResult.Victory)
                {
                    notificationBar.AddNotification(NotificationIcon.BattleVictory).Clicked += delegate
                    {
                        NotificationManager.Instance.ShowNotification(
                            $"Victory in {provinceName}!",
                            $"Good news!" + Environment.NewLine + Environment.NewLine +
                            $"Our troops attacking {defenderFactionName} in {provinceName} " +
                            $"have managed to break the defence and occupy the province!");
                    };
                }
                else
                {
                    notificationBar.AddNotification(NotificationIcon.BattleDefeat).Clicked += delegate
                    {
                        NotificationManager.Instance.ShowNotification(
                            $"Defeat in {provinceName}!",
                            $"Bad news!" + Environment.NewLine + Environment.NewLine +
                            $"Our troops attacking {defenderFactionName} in {provinceName} " +
                            $"were defeated by the defending forces!");
                    };
                }
            }
            catch (InvalidTargetProvinceException)
            {
                NotificationManager.Instance.ShowNotification(
                    $"Invalid target!",
                    $"Sorry!" + Environment.NewLine + Environment.NewLine +
                    $"You have chosen an invalid target that cannot be attacked.");
            }
        }

        void SideBar_TurnButtonClicked(object sender, MouseButtonEventArgs e)
        {
            NextTurn();
        }

        void AdministrationBar_StatsButtonClicked(object sender, MouseButtonEventArgs e)
        {
            NotificationManager.Instance.ShowNotification(
                "Statistics",
                $"Income: {game.GetFactionIncome(game.PlayerFactionId)}" + Environment.NewLine +
                $"Outcome: {game.GetFactionOutcome(game.PlayerFactionId)}" + Environment.NewLine +
                $"Militia Recruitment: {game.GetFactionRecruitment(game.PlayerFactionId)}");
        }

        void AdministrationBar_RecruitButtonClicked(object sender, MouseButtonEventArgs e)
        {
            recruitmentPanel.Show();
        }

        void ProvincePanel_AttackButtonClicked(object sender, MouseButtonEventArgs e)
        {
            AttackProvince(provincePanel.ProvinceId);
        }

        void ProvincePanel_BuildButtonClicked(object sender, MouseButtonEventArgs e)
        {
            buildingPanel.Show();
        }

        void GameMap_Clicked(object sender, MouseButtonEventArgs e)
        {
            string provinceId = gameMap.SelectedProvinceId;

            if (string.IsNullOrEmpty(provinceId))
            {
                return;
            }

            provincePanel.ProvinceId = provinceId;
            provincePanel.Show();
        }

        void game_OnPlayerProvinceAttacked(object sender, BattleEventArgs e)
        {
            string provinceName = game.GetProvince(e.ProvinceId).Name;
            string attackerFactionName = game.GetFaction(e.AttackerFactionId).Name;

            if (e.BattleResult == BattleResult.Victory)
            {
                notificationBar.AddNotification(NotificationIcon.ProvinceLost).Clicked += delegate
                {
                    NotificationManager.Instance.ShowNotification(
                        $"{provinceName} province lost!",
                        $"Bad news!" + Environment.NewLine + Environment.NewLine +
                        $"One of our provinces, {provinceName}, was attacked by {attackerFactionName}, " +
                        $"who managed to break the defence and occupy it!");
                };
            }
            else
            {
                notificationBar.AddNotification(NotificationIcon.ProvinceDefended).Clicked += delegate
                {
                    NotificationManager.Instance.ShowNotification(
                        $"{provinceName} province defended!",
                        $"Important news!" + Environment.NewLine + Environment.NewLine +
                        $"One of our provinces, {provinceName}, was attacked by {attackerFactionName}, " +
                        $"but our brave troops managed to sucesfully defend it!");
                };
            }
        }

        void game_OnFactionDestroyed(object sender, FactionEventArgs e)
        {
            string factionName = game.GetFaction(e.FactionId).Name;

            notificationBar.AddNotification(NotificationIcon.FactionDestroyed).Clicked += delegate
            {
                NotificationManager.Instance.ShowNotification(
                    $"{factionName} destroyed!",
                    $"A significant development in the war just took place, " +
                    $"as {factionName} was destroyed!");
            };
        }

        void game_OnFactionRevived(object sender, FactionEventArgs e)
        {
            string factionName = game.GetFaction(e.FactionId).Name;

            notificationBar.AddNotification(NotificationIcon.FactionDestroyed).Clicked += delegate
            {
                NotificationManager.Instance.ShowNotification(
                    $"{factionName} revived!",
                    $"A significant development in the war just took place, " +
                    $"as {factionName} was revived!");
            };
        }

        void game_OnFactionWon(object sender, FactionEventArgs e)
        {
            string factionName = game.GetFaction(e.FactionId).Name;

            notificationBar.AddNotification(NotificationIcon.GameFinished).Clicked += delegate
            {
                NotificationManager.Instance.ShowNotification(
                    $"{factionName} has won!",
                    $"The war is over!" + Environment.NewLine +
                    $"{factionName} has conquered the world and established a new world order!");
            };
        }
    }
}


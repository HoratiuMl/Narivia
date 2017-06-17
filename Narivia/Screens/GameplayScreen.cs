﻿using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Narivia.GameLogic.Enumerations;
using Narivia.GameLogic.Events;
using Narivia.GameLogic.GameManagers.Interfaces;
using Narivia.GameLogic.GameManagers;
using Narivia.Infrastructure.Exceptions;
using Narivia.Input.Events;
using Narivia.Interface.Widgets;
using Narivia.Interface.Widgets.Enumerations;

namespace Narivia.Screens
{
    /// <summary>
    /// Gameplay screen.
    /// </summary>
    public class GameplayScreen : Screen
    {
        /// <summary>
        /// Gets or sets the game map.
        /// </summary>
        /// <value>The game map.</value>
        public GameMap GameMap { get; set; }

        /// <summary>
        /// Gets or sets the info bar.
        /// </summary>
        /// <value>The info bar.</value>
        public InfoBar InfoBar { get; set; }

        /// <summary>
        /// Gets or sets the region bar.
        /// </summary>
        /// <value>The region bar.</value>
        public RegionBar RegionBar { get; set; }

        /// <summary>
        /// Gets or sets the side bar.
        /// </summary>
        /// <value>The side bar.</value>
        public SideBar SideBar { get; set; }

        /// <summary>
        /// Gets or sets the notification bar.
        /// </summary>
        /// <value>The notification bar.</value>
        public NotificationBar NotificationBar { get; set; }

        IGameManager game;

        /// <summary>
        /// Loads the content.
        /// </summary>
        public override void LoadContent()
        {
            string initialWorldId = "narivia";
            string initialFactionId = "alpalet";

            if (ScreenArgs != null && ScreenArgs.Length >= 2)
            {
                initialWorldId = ScreenArgs[0];
                initialFactionId = ScreenArgs[1];
            }

            game = new GameManager();
            game.NewGame(initialWorldId, initialFactionId);

            GameMap.AssociateGameManager(ref game);
            RegionBar.AssociateGameManager(ref game);

            SideBar.WorldId = game.WorldId;
            SideBar.FactionId = game.PlayerFactionId;
            SideBar.FactionName = game.GetFactionName(game.PlayerFactionId);

            GameMap.LoadContent();
            InfoBar.LoadContent();
            RegionBar.LoadContent();
            SideBar.LoadContent();
            NotificationBar.LoadContent();

            base.LoadContent();

            string factionName = game.GetFactionName(game.PlayerFactionId);

            ShowNotification($"Welcome to {game.WorldName}",
                             $"The era of peace has ended! Old rivalries remerged, and a global war broke out." + Environment.NewLine +
                             $"Conquer the world in the name of {factionName}, and secure its place in the golden pages of history!",
                             NotificationType.Informational,
                             NotificationStyle.Big,
                             new Vector2(256, 256));

            RegionBar.SetRegion(game.GetFactionCapital(game.PlayerFactionId));

            LinkEvents();

            GameMap.CentreCameraOnPosition(game.GetFactionCentreX(game.PlayerFactionId),
                                           game.GetFactionCentreY(game.PlayerFactionId));
        }

        /// <summary>
        /// Unloads the content.
        /// </summary>
        public override void UnloadContent()
        {
            GameMap.UnloadContent();
            InfoBar.UnloadContent();
            RegionBar.UnloadContent();
            SideBar.UnloadContent();
            NotificationBar.UnloadContent();

            base.UnloadContent();
        }

        /// <summary>
        /// Update the content.
        /// </summary>
        /// <returns>The update.</returns>
        /// <param name="gameTime">Game time.</param>
        public override void Update(GameTime gameTime)
        {
            Dictionary<string, int> troops = new Dictionary<string, int>();

            game.GetUnits().ToList().ForEach(u => troops.Add(u.Name, game.GetFactionArmySize(game.PlayerFactionId, u.Id)));

            InfoBar.Regions = game.GetFactionRegionsCount(game.PlayerFactionId);
            InfoBar.Holdings = game.GetFactionHoldingsCount(game.PlayerFactionId);
            InfoBar.Wealth = game.GetFactionWealth(game.PlayerFactionId);
            InfoBar.Troops = troops;

            if (!string.IsNullOrEmpty(GameMap.SelectedRegionId))
            {
                RegionBar.SetRegion(GameMap.SelectedRegionId);
            }

            SideBar.Turn = game.Turn;
            SideBar.FactionId = game.PlayerFactionId;
            SideBar.FactionName = game.GetFactionName(game.PlayerFactionId);

            GameMap.Update(gameTime);
            InfoBar.Update(gameTime);
            RegionBar.Update(gameTime);
            SideBar.Update(gameTime);
            NotificationBar.Update(gameTime);

            base.Update(gameTime);
        }

        /// <summary>
        /// Draw the content on the specified spriteBatch.
        /// </summary>
        /// <returns>The draw.</returns>
        /// <param name="spriteBatch">Sprite batch.</param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            GameMap.Draw(spriteBatch);
            InfoBar.Draw(spriteBatch);
            RegionBar.Draw(spriteBatch);
            SideBar.Draw(spriteBatch);
            NotificationBar.Draw(spriteBatch);

            base.Draw(spriteBatch);
        }

        void LinkEvents()
        {
            game.PlayerRegionAttacked += game_OnPlayerRegionAttacked;
            game.FactionDestroyed += game_OnFactionDestroyed;
            game.FactionRevived += game_OnFactionRevived;
            game.FactionWon += game_OnFactionWon;

            GameMap.Clicked += GameMap_Clicked;

            SideBar.TurnButton.Clicked += SideBar_TurnButtonClicked;
            SideBar.StatsButton.Clicked += SideBar_StatsButtonClicked;
            SideBar.RelationsButton.Clicked += SideBar_RelationsButtonClicked;
        }

        void NextTurn()
        {
            NotificationBar.Clear();

            Dictionary<string, int> troopsOld = new Dictionary<string, int>();
            game.GetUnits().ToList().ForEach(u => troopsOld.Add(u.Name, game.GetFactionArmySize(game.PlayerFactionId, u.Id)));

            int regionsOld = game.GetFactionRegionsCount(game.PlayerFactionId);
            int holdingsOld = game.GetFactionHoldingsCount(game.PlayerFactionId);
            int wealthOld = game.GetFactionWealth(game.PlayerFactionId);
            int incomeOld = game.GetFactionIncome(game.PlayerFactionId);
            int outcomeOld = game.GetFactionOutcome(game.PlayerFactionId);
            int recruitmentOld = game.GetFactionRecruitment(game.PlayerFactionId);

            game.NextTurn();

            Dictionary<string, int> troopsNew = new Dictionary<string, int>();
            game.GetUnits().ToList().ForEach(u => troopsNew.Add(u.Name, game.GetFactionArmySize(game.PlayerFactionId, u.Id)));

            int regionsNew = game.GetFactionRegionsCount(game.PlayerFactionId);
            int holdingsNew = game.GetFactionHoldingsCount(game.PlayerFactionId);
            int wealthNew = game.GetFactionWealth(game.PlayerFactionId);
            int incomeNew = game.GetFactionIncome(game.PlayerFactionId);
            int outcomeNew = game.GetFactionOutcome(game.PlayerFactionId);
            int recruitmentNew = game.GetFactionRecruitment(game.PlayerFactionId);

            NotificationBar.AddNotification(NotificationIcon.TurnReport).Clicked += delegate
            {
                ShowNotification($"Turn {game.Turn} Report",
                                 $"Regions: {regionsNew} ({(regionsNew - regionsOld).ToString("+0;-#")})" + Environment.NewLine +
                                 $"Holdings: {holdingsNew} ({(holdingsNew - holdingsOld).ToString("+0;-#")})" + Environment.NewLine +
                                 $"Wealth: {wealthNew} ({(wealthNew - wealthOld).ToString("+0;-#")})" + Environment.NewLine +
                                 $"Income: {incomeNew} ({(incomeNew - incomeOld).ToString("+0;-#")})" + Environment.NewLine +
                                 $"Outcome: {outcomeNew} ({(outcomeNew - outcomeOld).ToString("+0;-#")})" + Environment.NewLine +
                                 $"Militia Recruitment: {recruitmentNew} ({(recruitmentNew - recruitmentOld).ToString("+0;-#")})",
                                 NotificationType.Informational,
                                 NotificationStyle.Big,
                                 new Vector2(256, 224));
            };

            NotificationBar.AddNotification(NotificationIcon.RecruitmentReport).Clicked += delegate
            {
                string body = string.Empty;

                foreach (string key in troopsNew.Keys)
                {
                    body += $"{key}: {troopsNew[key]} ({(troopsNew[key] - troopsOld[key]).ToString("+0;-#")})" + Environment.NewLine;
                }

                ShowNotification($"Recruitment Report", body, NotificationType.Informational, NotificationStyle.Big, new Vector2(256, 224));
            };
        }

        void SideBar_TurnButtonClicked(object sender, MouseEventArgs e)
        {
            NextTurn();
        }

        void SideBar_StatsButtonClicked(object sender, MouseEventArgs e)
        {
            ShowNotification("Statistics",
                             $"Income: {game.GetFactionIncome(game.PlayerFactionId)}" + Environment.NewLine +
                             $"Outcome: {game.GetFactionOutcome(game.PlayerFactionId)}" + Environment.NewLine +
                             $"Militia Recruitment: {game.GetFactionRecruitment(game.PlayerFactionId)}",
                             NotificationType.Informational,
                             NotificationStyle.Big,
                             new Vector2(256, 160));
        }

        void SideBar_RelationsButtonClicked(object sender, MouseEventArgs e)
        {
            ShowNotification("Diplomatic Relations",
                             "Comming soon :)",
                             NotificationType.Informational,
                             NotificationStyle.Big,
                             new Vector2(256, 128));
        }

        void GameMap_Clicked(object sender, MouseEventArgs e)
        {
            string regionId = GameMap.SelectedRegionId;

            if (string.IsNullOrEmpty(regionId))
            {
                return;
            }

            if (game.GetFactionTroopsCount(game.PlayerFactionId) < game.MinTroopsPerAttack)
            {
                ShowNotification($"Not enough troops!",
                                 $"Sorry!" + Environment.NewLine + Environment.NewLine +
                                 $"You do need at least {game.MinTroopsPerAttack} troops to attack any region.",
                                 NotificationType.Informational,
                                 NotificationStyle.Big,
                                 new Vector2(256, 192));

                return;
            }

            try
            {
                BattleResult result = game.PlayerAttackRegion(regionId);

                NextTurn();

                string regionName = game.GetRegionName(regionId);
                string defenderFactionId = game.GetRegionFaction(regionId);
                string defenderFactionName = game.GetFactionName(defenderFactionId);

                if (result == BattleResult.Victory)
                {
                    NotificationBar.AddNotification(NotificationIcon.BattleVictory).Clicked += delegate
                        {
                            ShowNotification($"Victory in {regionName}!",
                                             $"Good news!" + Environment.NewLine + Environment.NewLine +
                                             $"Our troops attacking {defenderFactionName} in {regionName} " +
                                             $"have managed to break the defence and occupy the region!",
                                             NotificationType.Informational,
                                             NotificationStyle.Big,
                                             new Vector2(256, 224));
                        };
                }
                else
                {
                    NotificationBar.AddNotification(NotificationIcon.BattleDefeat).Clicked += delegate
                        {
                            ShowNotification($"Defeat in {regionName}!",
                                             $"Bad news!" + Environment.NewLine + Environment.NewLine +
                                             $"Our troops attacking {defenderFactionName} in {regionName} " +
                                             $"were defeated by the defending forces!",
                                             NotificationType.Informational,
                                             NotificationStyle.Big,
                                             new Vector2(256, 224));
                        };
                }
            }
            catch (InvalidTargetRegionException)
            {
                ShowNotification($"Invalid target!",
                                 $"Sorry!" + Environment.NewLine + Environment.NewLine +
                                 $"You have chosen an invalid target that cannot be attacked.",
                                 NotificationType.Informational,
                                 NotificationStyle.Big,
                                 new Vector2(256, 192));
            }
        }

        void game_OnPlayerRegionAttacked(object sender, BattleEventArgs e)
        {
            string regionName = game.GetRegionName(e.RegionId);
            string attackerFactionName = game.GetFactionName(e.AttackerFactionId);

            if (e.BattleResult == BattleResult.Victory)
            {
                NotificationBar.AddNotification(NotificationIcon.RegionLost).Clicked += delegate
                    {
                        ShowNotification($"{regionName} region lost!",
                                         $"Bad news!" + Environment.NewLine + Environment.NewLine +
                                         $"One of our regions, {regionName}, was attacked by {attackerFactionName}, " +
                                         $"who managed to break the defence and occupy it!",
                                         NotificationType.Informational,
                                         NotificationStyle.Big,
                                         new Vector2(256, 224));
                    };
            }
            else
            {
                NotificationBar.AddNotification(NotificationIcon.RegionDefended).Clicked += delegate
                    {
                        ShowNotification($"{regionName} region defended!",
                                         $"Important news!" + Environment.NewLine + Environment.NewLine +
                                         $"One of our regions, {regionName}, was attacked by {attackerFactionName}, " +
                                         $"but our brave troops managed to sucesfully defend it!",
                                         NotificationType.Informational,
                                         NotificationStyle.Big,
                                         new Vector2(256, 224));
                    };
            }
        }

        void game_OnFactionDestroyed(object sender, FactionEventArgs e)
        {
            string factionName = game.GetFactionName(e.FactionId);

            NotificationBar.AddNotification(NotificationIcon.FactionDestroyed).Clicked += delegate
                {
                    ShowNotification($"{factionName} destroyed!",
                                     $"A significant development in the war just took place, " +
                                     $"as {factionName} was destroyed!",
                                     NotificationType.Informational,
                                     NotificationStyle.Big,
                                     new Vector2(256, 192));
                };
        }

        void game_OnFactionRevived(object sender, FactionEventArgs e)
        {
            string factionName = game.GetFactionName(e.FactionId);

            NotificationBar.AddNotification(NotificationIcon.FactionDestroyed).Clicked += delegate
                {
                    ShowNotification($"{factionName} revived!",
                                     $"A significant development in the war just took place, " +
                                     $"as {factionName} was revived!",
                                     NotificationType.Informational,
                                     NotificationStyle.Big,
                                     new Vector2(256, 192));
                };
        }

        void game_OnFactionWon(object sender, FactionEventArgs e)
        {
            string factionName = game.GetFactionName(e.FactionId);

            NotificationBar.AddNotification(NotificationIcon.GameFinished).Clicked += delegate
                {
                    ShowNotification($"{factionName} has won!",
                                     $"The war is over!" + Environment.NewLine +
                                     $"{factionName} has conquered the world and established a new world order!",
                                     NotificationType.Informational,
                                     NotificationStyle.Big,
                                     new Vector2(256, 192));
                };
        }
    }
}


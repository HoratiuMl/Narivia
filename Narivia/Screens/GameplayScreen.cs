﻿using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Narivia.BusinessLogic.GameManagers;
using Narivia.Graphics;
using Narivia.Models;
using Narivia.WorldMap;

namespace Narivia.Screens
{
    public class GameplayScreen : Screen
    {
        GameDomainService gameDomainService;
        Map map;

        public override void LoadContent()
        {
            base.LoadContent();

            gameDomainService = new GameDomainService();
            gameDomainService.NewGame("narivia", "alpalet");

            LoadMap();
        }

        public override void UnloadContent()
        {
            base.UnloadContent();

            map.UnloadContent();
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            map.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            base.Draw(spriteBatch);

            map.Draw(spriteBatch);
        }

        void LoadMap()
        {
            map = new Map
            {
                TileDimensions = new Vector2(32, 32),
                Layers = new List<Layer>()
            };

            List<Biome> biomes = gameDomainService.GetAllBiomes();

            foreach (Biome biome in biomes)
            {
                Layer layer = new Layer
                {
                    Image = new Image { ImagePath = "World/Terrain/" + biome.Id },
                    TileMap = new TileMap()
                };


                for (int x = 0; x < 640; x++)
                {
                    string row = string.Empty;

                    for (int y = 0; y < 640; y++)
                    {
                        string biomeId = gameDomainService.BiomeMap[x, y];
                        string tile = string.Empty;

                        if (biome.Id == biomeId)
                        {
                            tile = "[1:3]";
                        }
                        else
                        {
                            tile = "[x:x]";
                        }

                        row += tile;
                    }

                    layer.TileMap.Rows.Add(row);
                }

                map.Layers.Add(layer);
            }

            map.LoadContent();
        }
    }
}


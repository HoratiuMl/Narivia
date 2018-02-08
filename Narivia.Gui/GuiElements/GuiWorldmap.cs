using System.Collections.Generic;
using System.Xml.Serialization;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NuciXNA.Graphics;
using NuciXNA.Gui.GuiElements;
using NuciXNA.Input.Events;
using NuciXNA.Primitives;
using NuciXNA.Primitives.Mapping;

using Narivia.Common.Extensions;
using Narivia.GameLogic.GameManagers.Interfaces;
using Narivia.Gui.Helpers;
using Narivia.Models;
using Narivia.Settings;

namespace Narivia.Gui.GuiElements
{
    /// <summary>
    /// World map GUI element.
    /// </summary>
    public class GuiWorldmap : GuiElement
    {
        /// <summary>
        /// Gets the selected province identifier.
        /// </summary>
        /// <value>The selected province identifier.</value>
        [XmlIgnore]
        public string SelectedProvinceId { get; private set; }

        IGameManager game;
        Camera camera;
        World world;

        Sprite provinceHighlight;
        Sprite selectedProvinceHighlight;
        Sprite factionBorder;

        Dictionary<string, Sprite> terrainSprites;

        Point2D mouseCoords;

        /// <summary>
        /// Loads the content.
        /// </summary>
        public override void LoadContent()
        {
            camera = new Camera { Size = Size };
            world = game.GetWorld();

            terrainSprites = new Dictionary<string, Sprite>();

            foreach (Terrain terrain in game.GetTerrains())
            {
                Sprite terrainSprite = new Sprite
                {
                    ContentFile = $"World/Terrain/{terrain.Spritesheet}",
                    SourceRectangle = new Rectangle2D(
                        GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE * 3,
                        GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE)
                };

                terrainSprite.LoadContent();
                terrainSprites.AddOrUpdate(terrain.Spritesheet, terrainSprite);
            }

            provinceHighlight = new Sprite
            {
                ContentFile = "World/Effects/border",
                SourceRectangle = new Rectangle2D(
                    GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE * 3,
                    GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE),
                Tint = Colour.White,
                Opacity = 1.0f
            };

            selectedProvinceHighlight = new Sprite
            {
                ContentFile = "World/Effects/border",
                SourceRectangle = new Rectangle2D(
                    0, GameDefines.MAP_TILE_SIZE * 3,
                    GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE),
                Tint = Colour.White,
                Opacity = 1.0f
            };

            factionBorder = new Sprite
            {
                ContentFile = "World/Effects/border",
                SourceRectangle = new Rectangle2D(
                    0, GameDefines.MAP_TILE_SIZE,
                    GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE),
                Tint = Colour.Blue,
                Opacity = 1.0f
            };

            camera.LoadContent();

            provinceHighlight.LoadContent();
            selectedProvinceHighlight.LoadContent();
            factionBorder.LoadContent();

            base.LoadContent();
        }

        /// <summary>
        /// Unloads the content.
        /// </summary>
        public override void UnloadContent()
        {
            camera.UnloadContent();

            provinceHighlight.UnloadContent();
            selectedProvinceHighlight.UnloadContent();
            factionBorder.UnloadContent();

            terrainSprites.Clear();

            base.UnloadContent();
        }

        /// <summary>
        /// Update the content.
        /// </summary>
        /// <param name="gameTime">Game time.</param>
        public override void Update(GameTime gameTime)
        {
            camera.Size = Size;

            camera.Update(gameTime);

            provinceHighlight.Update(gameTime);
            selectedProvinceHighlight.Update(gameTime);
            factionBorder.Update(gameTime);

            base.Update(gameTime);

            Point2D mouseGameMapCoords = ScreenToMapCoordinates(mouseCoords);

            int x = mouseGameMapCoords.X;
            int y = mouseGameMapCoords.Y;

            if (x > 0 && x < game.GetWorld().Width &&
                y > 0 && y < game.GetWorld().Height)
            {
                // TODO: Handle the Id retrieval properly
                SelectedProvinceId = game.GetWorld().Tiles[x, y].ProvinceId;

                // TODO: Also handle this properly
                if (game.FactionIdAtLocation(x, y) == GameDefines.GAIA_FACTION)
                {
                    SelectedProvinceId = null;
                }
            }
            else
            {
                SelectedProvinceId = null;
            }
        }

        /// <summary>
        /// Draw the content on the specified spriteBatch.
        /// </summary>
        /// <param name="spriteBatch">Sprite batch.</param>
        public override void Draw(SpriteBatch spriteBatch)
        {
            Point camCoordsBegin = new Point(
                camera.Location.X / GameDefines.MAP_TILE_SIZE,
                camera.Location.Y / GameDefines.MAP_TILE_SIZE);

            Point camCoordsEnd = new Point(
                camCoordsBegin.X + camera.Size.Width / GameDefines.MAP_TILE_SIZE + 2,
                camCoordsBegin.Y + camera.Size.Height / GameDefines.MAP_TILE_SIZE + 1);

            for (int y = camCoordsBegin.Y; y < camCoordsEnd.Y; y++)
            {
                for (int x = camCoordsBegin.X; x < camCoordsEnd.X; x++)
                {
                    DrawTile(spriteBatch ,x, y);
                }
            }

            DrawProvinceHighlight(spriteBatch);
            DrawFactionBorders(spriteBatch);

            base.Draw(spriteBatch);
        }

        // TODO: Handle this better
        /// <summary>
        /// Associates the game manager.
        /// </summary>
        /// <param name="game">Game.</param>
        public void AssociateGameManager(ref IGameManager game)
        {
            this.game = game;
        }

        /// <summary>
        /// Centres the camera on the specified location.
        /// </summary>
        public void CentreCameraOnLocation(int x, int y)
        {
            camera.CentreOnLocation(new Point2D(x * GameDefines.MAP_TILE_SIZE, y * GameDefines.MAP_TILE_SIZE));
        }

        void DrawTile(SpriteBatch spriteBatch, int x, int y)
        {
            WorldTile tile = world.Tiles[x, y];

            Terrain terrain = game.GetTerrain(tile.TerrainId); // TODO: Optimise this. Don't call this every time
            DrawTerrainSprite(spriteBatch, x, y, terrain.Spritesheet, 1, 3);

            TileShape tileShape = GetTileShape(x, y);

            switch(tileShape)
            {
                case TileShape.InnerCornerSW:
                    Terrain terrainNW = game.GetTerrain(world.Tiles[x + 1, y + 1].TerrainId);
                    DrawTerrainSprite(spriteBatch, x, y, terrainNW.Spritesheet, 2, 1);
                    break;

                case TileShape.OuterCornerNW:
                    Terrain terrainSE = game.GetTerrain(world.Tiles[x - 1, y - 1].TerrainId);
                    DrawTerrainSprite(spriteBatch, x, y, terrainSE.Spritesheet, 0, 2);
                    break;

                case TileShape.EdgeN:
                    Terrain terrainS = game.GetTerrain(world.Tiles[x, y + 1].TerrainId);
                    DrawTerrainSprite(spriteBatch, x, y, terrainS.Spritesheet, 1, 2);
                    break;

                case TileShape.EdgeW:
                    Terrain terrainE = game.GetTerrain(world.Tiles[x + 1, y].TerrainId);
                    DrawTerrainSprite(spriteBatch, x, y, terrainE.Spritesheet, 0, 3);
                    break;

                case TileShape.EdgeS:
                    Terrain terrainN = game.GetTerrain(world.Tiles[x, y - 1].TerrainId);
                    DrawTerrainSprite(spriteBatch, x, y, terrainN.Spritesheet, 1, 4);
                    break;

                case TileShape.EdgeE:
                    Terrain terrainW = game.GetTerrain(world.Tiles[x - 1, y].TerrainId);
                    DrawTerrainSprite(spriteBatch, x, y, terrainW.Spritesheet, 2, 3);
                    break;
            }
        }

        void DrawTerrainSprite(SpriteBatch spriteBatch, int tileX, int tileY, string spritesheet, int spritesheetX, int spritesheetY)
        {
            Sprite terrainSprite = terrainSprites[spritesheet];

            terrainSprite.Location = new Point2D(
                tileX * GameDefines.MAP_TILE_SIZE - camera.Location.X,
                tileY * GameDefines.MAP_TILE_SIZE - camera.Location.Y);
            terrainSprite.SourceRectangle = new Rectangle2D(
                GameDefines.MAP_TILE_SIZE * spritesheetX, GameDefines.MAP_TILE_SIZE * spritesheetY,
                GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE);

            terrainSprite.Draw(spriteBatch);
        }

        void DrawProvinceHighlight(SpriteBatch spriteBatch)
        {
            if (string.IsNullOrEmpty(SelectedProvinceId))
            {
                return;
            }

            int cameraSizeX = camera.Size.Width / GameDefines.MAP_TILE_SIZE + 2;
            int cameraSizeY = camera.Size.Height / GameDefines.MAP_TILE_SIZE + 2;

            for (int j = 0; j < cameraSizeY; j++)
            {
                for (int i = 0; i < cameraSizeX; i++)
                {
                    Point2D screenCoords = new Point2D((i * GameDefines.MAP_TILE_SIZE) - camera.Location.X % GameDefines.MAP_TILE_SIZE,
                                                     (j * GameDefines.MAP_TILE_SIZE) - camera.Location.Y % GameDefines.MAP_TILE_SIZE);
                    Point2D gameCoords = ScreenToMapCoordinates(screenCoords);

                    int x = gameCoords.X;
                    int y = gameCoords.Y;

                    if (x < 0 || x > game.GetWorld().Width ||
                        y < 0 || y > game.GetWorld().Height)
                    {
                        continue;
                    }

                    string provinceId = game.GetWorld().Tiles[x, y].ProvinceId;
                    string factionId = game.FactionIdAtLocation(x, y);
                    Colour factionColour = game.GetFaction(factionId).Colour.ToColour();

                    if (factionId == GameDefines.GAIA_FACTION)
                    {
                        continue;
                    }

                    if (SelectedProvinceId == provinceId)
                    {
                        selectedProvinceHighlight.Location = screenCoords;
                        selectedProvinceHighlight.Tint = factionColour;
                        selectedProvinceHighlight.Draw(spriteBatch);
                    }
                    else
                    {
                        provinceHighlight.Location = screenCoords;
                        provinceHighlight.Tint = factionColour;
                        provinceHighlight.Draw(spriteBatch);
                    }
                }
            }
        }

        void DrawFactionBorders(SpriteBatch spriteBatch)
        {
            int cameraSizeX = camera.Size.Width / GameDefines.MAP_TILE_SIZE + 2;
            int cameraSizeY = camera.Size.Height / GameDefines.MAP_TILE_SIZE + 2;

            for (int j = 0; j < cameraSizeY; j++)
            {
                for (int i = 0; i < cameraSizeX; i++)
                {
                    Point2D screenCoords = new Point2D(
                        (i * GameDefines.MAP_TILE_SIZE) - camera.Location.X % GameDefines.MAP_TILE_SIZE,
                        (j * GameDefines.MAP_TILE_SIZE) - camera.Location.Y % GameDefines.MAP_TILE_SIZE);
                    Point2D gameCoords = ScreenToMapCoordinates(screenCoords);

                    int x = gameCoords.X;
                    int y = gameCoords.Y;

                    if (x < 0 || x > game.GetWorld().Width ||
                        y < 0 || y > game.GetWorld().Height)
                    {
                        continue;
                    }

                    string factionId = game.FactionIdAtLocation(x, y);
                    Colour factionColour = game.GetFaction(factionId).Colour.ToColour();

                    if (factionId == GameDefines.GAIA_FACTION)
                    {
                        continue;
                    }

                    string factionIdN = game.FactionIdAtLocation(x, y - 1);
                    string factionIdW = game.FactionIdAtLocation(x - 1, y);
                    string factionIdS = game.FactionIdAtLocation(x, y + 1);
                    string factionIdE = game.FactionIdAtLocation(x + 1, y);

                    factionBorder.Location = screenCoords;
                    factionBorder.Tint = factionColour;

                    // TODO: Optimise the below IFs

                    if (factionIdN != factionId &&
                        factionIdN != GameDefines.GAIA_FACTION)
                    {
                        factionBorder.SourceRectangle = new Rectangle2D(
                            GameDefines.MAP_TILE_SIZE, 0,
                            GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE);
                        factionBorder.Draw(spriteBatch);
                    }
                    if (factionIdW != factionId &&
                        factionIdW != GameDefines.GAIA_FACTION)
                    {
                        factionBorder.SourceRectangle = new Rectangle2D(
                            0, GameDefines.MAP_TILE_SIZE,
                            GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE);
                        factionBorder.Draw(spriteBatch);
                    }
                    if (factionIdS != factionId &&
                        factionIdS != GameDefines.GAIA_FACTION)
                    {
                        factionBorder.SourceRectangle = new Rectangle2D(
                            GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE * 2,
                            GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE);
                        factionBorder.Draw(spriteBatch);
                    }
                    if (factionIdE != factionId &&
                        factionIdE != GameDefines.GAIA_FACTION)
                    {
                        factionBorder.SourceRectangle = new Rectangle2D(
                            GameDefines.MAP_TILE_SIZE * 2, GameDefines.MAP_TILE_SIZE,
                            GameDefines.MAP_TILE_SIZE, GameDefines.MAP_TILE_SIZE);
                        factionBorder.Draw(spriteBatch);
                    }
                }
            }
        }

        TileShape GetTileShape(int x, int y)
        {
            WorldTile tile = world.Tiles[x, y];
            Terrain terrain = game.GetTerrain(tile.TerrainId); // TODO: Optimise this. Don't call this every time

            WorldTile tileNW = world.Tiles[x - 1, y - 1];
            WorldTile tileNE = world.Tiles[x + 1, y - 1];
            WorldTile tileSW = world.Tiles[x - 1, y + 1];
            WorldTile tileSE = world.Tiles[x + 1, y + 1];
            WorldTile tileN = world.Tiles[x, y - 1];
            WorldTile tileW = world.Tiles[x - 1, y];
            WorldTile tileS = world.Tiles[x, y + 1];
            WorldTile tileE = world.Tiles[x + 1, y];

            if (tileW.TerrainId != tile.TerrainId)
            {
                Terrain terrainW = game.GetTerrain(tileW.TerrainId);

                if (terrainW.ZIndex > terrain.ZIndex)
                {
                    return TileShape.EdgeE;
                }
            }
            else if (tileS.TerrainId != tile.TerrainId)
            {
                Terrain terrainS = game.GetTerrain(tileS.TerrainId);

                if (terrainS.ZIndex > terrain.ZIndex)
                {
                    return TileShape.EdgeN;
                }
            }
            else if (tileE.TerrainId != tile.TerrainId)
            {
                Terrain terrainE = game.GetTerrain(tileE.TerrainId);

                if (terrainE.ZIndex > terrain.ZIndex)
                {
                    return TileShape.EdgeW;
                }
            }

            return TileShape.None;
        }

        /// <summary>
        /// Gets the map cpprdomates based on the specified screen coordinates.
        /// </summary>
        /// <returns>The map coordinates.</returns>
        /// <param name="screenCoords">Screen coordinates.</param>
        Point2D ScreenToMapCoordinates(Point2D screenCoords)
        {
            return new Point2D(
                (camera.Location.X + screenCoords.X) / GameDefines.MAP_TILE_SIZE,
                (camera.Location.Y + screenCoords.Y) / GameDefines.MAP_TILE_SIZE);
        }

        protected override void OnMouseMoved(object sender, MouseEventArgs e)
        {
            base.OnMouseMoved(sender, e);

            mouseCoords = e.Location.ToPoint2D();
        }
    }
}

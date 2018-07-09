using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Xml.Serialization;

using NuciXNA.DataAccess.IO;
using NuciXNA.DataAccess.Repositories;
using NuciXNA.Primitives;

using Narivia.Common.Extensions;
using Narivia.DataAccess.DataObjects;

namespace Narivia.DataAccess.Repositories
{
    /// <summary>
    /// World repository implementation.
    /// </summary>
    public class WorldRepository : IRepository<string, WorldEntity>
    {
        readonly string worldsDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="WorldRepository"/> class.
        /// </summary>
        /// <param name="worldsDirectory">File name.</param>
        public WorldRepository(string worldsDirectory)
        {
            this.worldsDirectory = worldsDirectory;
        }

        /// <summary>
        /// Adds the specified world.
        /// </summary>
        /// <param name="worldEntity">World.</param>
        public void Add(WorldEntity worldEntity)
        {
            // TODO: Implement this
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get the world with the specified identifier.
        /// </summary>
        /// <returns>The world.</returns>
        /// <param name="id">Identifier.</param>
        public WorldEntity Get(string id)
        {
            WorldEntity worldEntity;
            string worldFile = Path.Combine(worldsDirectory, id, "world.xml");

            using (TextReader reader = new StreamReader(worldFile))
            {
                XmlSerializer xml = new XmlSerializer(typeof(WorldEntity));
                worldEntity = (WorldEntity)xml.Deserialize(reader);
            }

            worldEntity.Tiles = LoadWorldTiles(id);
            
            return worldEntity;
        }

        /// <summary>
        /// Gets all the worlds.
        /// </summary>
        /// <returns>The worlds</returns>
        public IEnumerable<WorldEntity> GetAll()
        {
            ConcurrentBag<WorldEntity> worldEntities = new ConcurrentBag<WorldEntity>();
            
            foreach(string worldId in Directory.GetDirectories(worldsDirectory))
            {
                worldEntities.Add(Get(worldId));
            }

            return worldEntities;
        }

        /// <summary>
        /// Updates the specified world.
        /// </summary>
        /// <param name="worldEntity">World.</param>
        public void Update(WorldEntity worldEntity)
        {
            string worldFile = Path.Combine(worldsDirectory, worldEntity.Id, "world.xml");

            using (TextWriter writer = new StreamWriter(worldFile))
            {
                XmlSerializer xml = new XmlSerializer(typeof(WorldEntity));
                xml.Serialize(writer, worldEntity);
            }

            // TODO: Save the ProvinceMap and TerrainMap as well
        }

        /// <summary>
        /// Removes the world with the specified identifier.
        /// </summary>
        /// <param name="id">Identifier.</param>
        public void Remove(string id)
        {
            Directory.Delete(Path.Combine(worldsDirectory, id));
        }

        /// <summary>
        /// Removes the specified world.
        /// </summary>
        /// <param name="world">World.</param>
        public void Remove(WorldEntity worldEntity)
        {
            Remove(worldEntity.Id);
        }

        WorldTileEntity[,] LoadWorldTiles(string worldId)
        {
            ConcurrentDictionary<int, string> provinceColourIds = new ConcurrentDictionary<int, string>();
            ConcurrentDictionary<int, string> terrainColourIds = new ConcurrentDictionary<int, string>();

            string provincesPath = Path.Combine(worldsDirectory, worldId, "provinces.xml");
            string terrainsPath = Path.Combine(worldsDirectory, worldId, "terrains.xml");

            IRepository<string, ProvinceEntity> provinceRepository = new ProvinceRepository(provincesPath);
            IRepository<string, TerrainEntity> terrainRepository = new TerrainRepository(terrainsPath);

            Parallel.ForEach(provinceRepository.GetAll(), r => provinceColourIds.AddOrUpdate(Colour.FromHexadecimal(r.ColourHexadecimal).ToArgb(), r.Id));
            Parallel.ForEach(terrainRepository.GetAll(), b => terrainColourIds.AddOrUpdate(Colour.FromHexadecimal(b.ColourHexadecimal).ToArgb(), b.Id));

            BitmapFile heightsBitmap = new BitmapFile(Path.Combine(worldsDirectory, worldId, "world_heights.png"));
            BitmapFile provinceBitmap = new BitmapFile(Path.Combine(worldsDirectory, worldId, "world_provinces.png"));
            BitmapFile riversBitmap = new BitmapFile(Path.Combine(worldsDirectory, worldId, "world_rivers.png"));
            BitmapFile terrainBitmap = new BitmapFile(Path.Combine(worldsDirectory, worldId, "world_terrains.png"));

            Point2D worldSize = new Point2D(
                Math.Max(terrainBitmap.Size.Width, provinceBitmap.Size.Width),
                Math.Max(terrainBitmap.Size.Height, provinceBitmap.Size.Height));

            WorldTileEntity[,] tiles = new WorldTileEntity[provinceBitmap.Size.Width, provinceBitmap.Size.Height];

            Parallel.For(0, worldSize.Y, y => Parallel.For(0, worldSize.X, x => tiles[x, y] = new WorldTileEntity()));

            terrainBitmap.LockBits();
            provinceBitmap.LockBits();
            riversBitmap.LockBits();

            Parallel.For(0, worldSize.Y, y => Parallel.For(0, worldSize.X, x =>
            {
                Colour heightColour = heightsBitmap.GetPixel(x, y);
                Colour riverColour = riversBitmap.GetPixel(x, y);
                int provinceArgb = provinceBitmap.GetPixel(x, y).ToArgb();
                int terrainArgb = terrainBitmap.GetPixel(x, y).ToArgb();

                tiles[x, y].ProvinceId = provinceColourIds[provinceArgb];
                tiles[x, y].TerrainId = terrainColourIds[terrainArgb];
                tiles[x, y].HasRiver = riverColour == Colour.Blue;

                if (heightColour == Colour.Blue ||
                    heightColour == Colour.Black)
                {
                    tiles[x, y].HasWater = true;
                    tiles[x, y].Altitude = 0;
                }
                else
                {
                    tiles[x, y].Altitude = (byte)((heightColour.R + heightColour.G + heightColour.B) / 3);
                }
            }));

            heightsBitmap.Dispose();
            provinceBitmap.Dispose();
            riversBitmap.Dispose();
            terrainBitmap.Dispose();

            return tiles;
        }
    }
}

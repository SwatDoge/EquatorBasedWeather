using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog.Targets;
using Sandbox.Game.Entities;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System.Reflection;
using Torch;
using Torch.Managers.PatchManager;
using Torch.Utils;
using VRage;
using VRage.Game;
using VRage.Library.Collections;
using VRage.Utils;
using VRageMath;

namespace BetterRandomWeather.patches
{
    [PatchShim]
    public static class HMySectorWeatherComponent
    {
        class CustomWeather
        {
            public string name;
            public float optimalRatio;
            public float biasMultiplierRatio;
            public int minDuration;
            public int maxDuration;

            public CustomWeather(string name, float optimalRatio, float biasMultiplierRatio, int minDuration, int maxDuration)
            {
                this.name = name;
                this.optimalRatio = optimalRatio;
                this.biasMultiplierRatio = biasMultiplierRatio;
                this.minDuration = minDuration;
                this.maxDuration = maxDuration;
            }

            public double getLikelyhoodAtEquatorRatio(float equatorRatio, float biasMultiplierRatio)
            {
                return Math.Pow(1.0 - Math.Abs(optimalRatio - equatorRatio) / Math.Max(equatorRatio, 1.0 - equatorRatio), biasMultiplierRatio + 2);
            }

            public static CustomWeather getRandomWeightedWeather(List<CustomWeather> weathers, float poleRatio)
            {
                double totalWeight = weathers.Aggregate(0d, (acc, weather) => acc + weather.getLikelyhoodAtEquatorRatio(poleRatio, weather.biasMultiplierRatio));
                double accumulatedWeight = 0;
                float randomWeight = (float)(random.NextDouble() * totalWeight);
                List<CustomWeather> sortedWeather = weathers.OrderByDescending(weather => weather.getLikelyhoodAtEquatorRatio(poleRatio, weather.biasMultiplierRatio)).ToList();

                foreach (CustomWeather weather in sortedWeather)
                {
                    if (Plugin.Config.Debug)
                    {
                        Plugin.Log.Info(weather.name + " " + weather.getLikelyhoodAtEquatorRatio(poleRatio, weather.biasMultiplierRatio));
                    }
                }

                foreach (CustomWeather weather in sortedWeather)
                {
                    double LikelyhoodAtEquatorRatio = weather.getLikelyhoodAtEquatorRatio(poleRatio, weather.biasMultiplierRatio);

                    if (Plugin.Config.Debug)
                    {
                        Plugin.Log.Info(weather.name + " " + LikelyhoodAtEquatorRatio);
                    }

                    accumulatedWeight += LikelyhoodAtEquatorRatio;

                    if (accumulatedWeight > randomWeight)
                    {
                        if (Plugin.Config.Debug)
                        {
                            Plugin.Log.Info("Spawned weather \"" + weather.name + "\" - pole ratio " + poleRatio + " with weight " + LikelyhoodAtEquatorRatio + " of " + randomWeight + "/" + totalWeight + " with bias multiplier of x" + weather.biasMultiplierRatio);
                        }
                        return weather;
                    }
                }

                return null;
            }
        }

        static Random random = new Random();

        private static List<CustomWeather> customWeatherList = new List<CustomWeather>
        {
            //Poles
            new CustomWeather("SnowHeavy",              1f, 3, 300, 600),
            new CustomWeather("Hailstorm",              1f, 2, 300, 600),
            new CustomWeather("SnowLight",              1f, 3, 600, 900),
            new CustomWeather("FogHeavy",               1f, 3, 300, 600),

            //Subpolar
            new CustomWeather("SnowLight",              0.85f, 3, 600, 900),
            new CustomWeather("SnowHeavy",              0.85f, 1, 300, 600),
            new CustomWeather("FogLight",               0.85f, 2, 600, 900),

            //Temperate
            new CustomWeather("HighWinds",              0.75f, 1, 300, 600),
            new CustomWeather("LowWinds",               0.75f, 2, 600, 900),
            new CustomWeather("RainLight",              0.75f, 4, 600, 900),
            new CustomWeather("ThunderstormLight",      0.75f, 2, 600, 900),
            new CustomWeather("ThunderstormHeavy",      0.75f, 1, 300, 600),

            //Subtropical
            new CustomWeather("HighWinds",              0.65f, 3, 300, 600),
            new CustomWeather("RainLight",              0.65f, 1, 600, 900),
            new CustomWeather("ThunderstormLight",      0.65f, 1, 600, 900),
            new CustomWeather("ThunderstormHeavy",      0.65f, 1, 300, 600),
            new CustomWeather("Dust",                   0.65f, 3, 600, 900),

            //Tropical
            new CustomWeather("HeatWave",               0.4f, 3, 600, 900),
            new CustomWeather("Dust",                   0.4f, 1, 600, 900),
            new CustomWeather("ExtremeHeat",            0.4f, 1, 300, 600),
            new CustomWeather("SandStormLight",         0.4f, 1, 600, 900),
            new CustomWeather("ElectricStorm",          0.4f, 0.1f, 300, 600),

            //(Sub)equatorial
            new CustomWeather("ThunderstormHeavy",      0.01f, 5, 300, 600),
            new CustomWeather("ExtremeHeat",            0.01f, 3, 300, 600),
            new CustomWeather("RainHeavy",              0.01f, 3, 300, 600),
        };


        #region CreateRandomWeather
        [ReflectedMethodInfo(typeof(MySectorWeatherComponent), "CreateRandomWeather", Parameters = [typeof(MyPlanet), typeof(Action<string>), typeof(int?)])]
        private static readonly MethodInfo CreateRandomWeather;

        [ReflectedMethodInfo(typeof(HMySectorWeatherComponent), "PrefixCreateRandomWeather")]
        private static readonly MethodInfo HCreateRandomWeather;

        public static bool PrefixCreateRandomWeather(MySectorWeatherComponent __instance, MyPlanet planet, Action<string> feedback, int? maxLength)
        {
            #region Default behavour
            bool isPlanet = planet != null;
            bool hasWeatherGenerators = planet.Generator.WeatherGenerators != null && planet.Generator.WeatherGenerators.Count != 0;

            bool hasPersistantWeather = string.IsNullOrEmpty(planet.Generator.PersistentWeather);

            bool globalWeatherEnabled = planet.Generator.GlobalWeather;

            bool hasGlobalWeather = planet.Generator.WeatherGenerators != null && planet.Generator.WeatherGenerators.Count > 0 && planet.Generator.WeatherGenerators?[0] != null;
            if (!isPlanet)
            {
                feedback.InvokeIfNotNull(MyTexts.Get(MyCommonTexts.ChatCommand_Texts_NoPlanet).ToString());
                return false;
            }
            if (!hasWeatherGenerators)
            {
                feedback.InvokeIfNotNull(MyTexts.Get(MyCommonTexts.ChatCommand_Texts_NoWeatherSystem).ToString());
                return false;
            }
            if (!hasPersistantWeather)
            {
                feedback.InvokeIfNotNull(MyTexts.Get(MyCommonTexts.ChatCommand_Texts_PersistentWeather).ToString());
                return false;
            }
            if (globalWeatherEnabled && !hasGlobalWeather)
            {
                return false;
            }

            if (globalWeatherEnabled)
            {
                __instance.SetWeather("Clear", planet.AtmosphereRadius, planet.PositionComp.WorldMatrix.Translation, null, Vector3.Zero);
                List<int> list = new List<int>();
                for (int i = 0; i < planet.Generator.WeatherGenerators[0].Weathers.Count; i++)
                {
                    for (int j = 0; j < planet.Generator.WeatherGenerators[0].Weathers[i].Weight; j++)
                    {
                        list.Add(i);
                    }
                }

                if (list.Count > 0)
                {
                    int randomInt = MyUtils.GetRandomInt(list.Count);
                    int randomWeatherIndex = MyUtils.GetRandomInt(planet.Generator.WeatherGenerators[0].Weathers[list[randomInt]].MinLength, planet.Generator.WeatherGenerators[0].Weathers[list[randomInt]].MaxLength);
                    
                    if (maxLength.HasValue)
                    {
                        randomWeatherIndex = Math.Min(randomWeatherIndex, maxLength.Value);
                    }

                    __instance.SetWeather(planet.Generator.WeatherGenerators[0].Weathers[list[randomInt]].Name, planet.AtmosphereRadius, planet.PositionComp.GetPosition(), null, Vector3.Zero, randomWeatherIndex);
                    feedback.InvokeIfNotNull(MyTexts.Get(MyCommonTexts.ChatCommand_Texts_RandomWeather).ToString());
                }

                return false;
            }

            #endregion
            int playerIndex = 0;
            foreach (MyPlayer onlinePlayer in Sync.Players.GetOnlinePlayers())
            {
                bool hasOnlinePlayer = onlinePlayer != null;
                bool isCurrentPlanet = MyGamePruningStructure.GetClosestPlanet(onlinePlayer.GetPosition())?.EntityId == planet.EntityId;
                
                if (!hasOnlinePlayer || !isCurrentPlanet)
                {
                    continue;
                }

                playerIndex++;
                Vector3D worldPosition = planet.GetClosestSurfacePointGlobal(onlinePlayer.GetPosition());
                bool weatherExists = __instance.GetWeather(worldPosition, out var _);

                if (!weatherExists)
                {
                    Vector3D axis = Vector3D.Normalize(planet.PositionComp.GetPosition() - worldPosition);
                    Vector3D randomPerpendicularVector = MyUtils.GetRandomPerpendicularVector(ref axis);
                    MyVoxelMaterialDefinition materialAt = planet.GetMaterialAt(ref worldPosition);
                    bool hasMaterial = materialAt != null;
                    bool hasMaterialTypeName = materialAt?.MaterialTypeName != null;
                    float atmosOffset = (float)(75.0 / 668.0 * (double)planet.AtmosphereRadius);

                    if (!hasMaterial || !hasMaterialTypeName)
                    {
                        continue;
                    }

                    if (Plugin.Config.Enabled && planet.Name.StartsWith("TerraRemake"))
                    {
                        Vector3D playerVector = Vector3D.Normalize(onlinePlayer.GetPosition() - planet.PositionComp.GetPosition());
                        Vector3D planetUp = planet.WorldMatrix.Up;

                        double poleRatio = Math.Abs(Vector3D.Dot(playerVector, planetUp));

                        CustomWeather weather = CustomWeather.getRandomWeightedWeather(customWeatherList, (float)poleRatio);

                        if (weather == null)
                        {
                            continue;
                        }

                        int spawnOffset = 1000;
                        int intensity = 1; //value between 1-2
                        int duration = random.Next(weather.minDuration, weather.maxDuration);


                        bool hasExistingWeatherNearbyFront = __instance.GetWeather(worldPosition - randomPerpendicularVector * ((float)spawnOffset + atmosOffset), out var _);
                        bool hasExistingWeatherNearbyBack = __instance.GetWeather(worldPosition + randomPerpendicularVector * ((float)spawnOffset + atmosOffset), out var _);
                        worldPosition -= randomPerpendicularVector * ((float)spawnOffset + atmosOffset);

                        __instance.SetWeather(
                            weather.name,
                            atmosOffset,
                            worldPosition,
                            null,
                            randomPerpendicularVector * (2f * ((float)spawnOffset + atmosOffset) / duration),
                            duration
                        );
                    }
                    #region old logic
                    else
                    {
                        foreach (MyWeatherGeneratorSettings weatherGenerator in planet.Generator.WeatherGenerators)
                        {

                            bool isMatchingMaterial = weatherGenerator.Voxel.Equals(materialAt.MaterialTypeName);

                            if (isMatchingMaterial)
                            {
                                continue;
                            }

                            List<int> weatherIndexes = new List<int>();
                            for (int weatherIndex = 0; weatherIndex < weatherGenerator.Weathers.Count; weatherIndex++)
                            {
                                for (int weatherWeightIndex = 0; weatherWeightIndex < weatherGenerator.Weathers[weatherIndex].Weight; weatherWeightIndex++)
                                {
                                    weatherIndexes.Add(weatherIndex);
                                }
                            }

                            if (weatherIndexes.Count > 0)
                            {
                                int randomWeatherIndex = MyUtils.GetRandomInt(weatherIndexes.Count);
                                int randomWeatherWeight = MyUtils.GetRandomInt(weatherGenerator.Weathers[weatherIndexes[randomWeatherIndex]].MinLength, weatherGenerator.Weathers[weatherIndexes[randomWeatherIndex]].MaxLength);
                                int spawnOffset = weatherGenerator.Weathers[weatherIndexes[randomWeatherIndex]].SpawnOffset;

                                bool hasExistingWeatherNearbyFront = __instance.GetWeather(worldPosition - randomPerpendicularVector * ((float)spawnOffset + atmosOffset), out var _);
                                bool hasExistingWeatherNearbyBack = __instance.GetWeather(worldPosition + randomPerpendicularVector * ((float)spawnOffset + atmosOffset), out var _);

                                if (!hasExistingWeatherNearbyFront && !hasExistingWeatherNearbyBack)
                                {
                                    worldPosition -= randomPerpendicularVector * ((float)spawnOffset + atmosOffset);
                                    __instance.SetWeather(
                                        weatherGenerator.Weathers[weatherIndexes[randomWeatherIndex]].Name,
                                        atmosOffset,
                                        worldPosition,
                                        null,
                                        randomPerpendicularVector * (2f * ((float)spawnOffset + atmosOffset) / (float)randomWeatherWeight),
                                        randomWeatherWeight
                                    );
                                    feedback.InvokeIfNotNull(MyTexts.Get(MyCommonTexts.ChatCommand_Texts_RandomWeather).ToString());
                                }
                            }
                        }
                    }
                    #endregion
                }
                else
                {
                    feedback.InvokeIfNotNull(MyTexts.Get(MyCommonTexts.ChatCommand_Texts_WeatherIncoming).ToString());
                }
            }

            if (playerIndex == 0)
            {
                feedback.InvokeIfNotNull(MyTexts.Get(MyCommonTexts.ChatCommand_Texts_NoPlayersAround).ToString());
            }

            return false;
        }
        #endregion

        #region UpdatePlanetDataServer
        //[ReflectedMethodInfo(typeof(MySectorWeatherComponent), "UpdatePlanetDataServer")]
        //private static readonly MethodInfo UpdatePlanetDataServer;

        //[ReflectedMethodInfo(typeof(HMySectorWeatherComponent), "PrefixUpdatePlanetDataServer")]
        //private static readonly MethodInfo HUpdatePlanetDataServer;

        //public static bool PrefixUpdatePlanetDataServer(MySectorWeatherComponent __instance)
        //{
        //    foreach (MyObjectBuilder_WeatherPlanetData weatherPlanetDatum in __instance.GetWeatherPlanetData())
        //    {
        //        if (weatherPlanetDatum.NextWeather > 600)
        //        {
        //            weatherPlanetDatum.NextWeather = 600;
        //        }
        //    }

        //    return true;
        //}

        #endregion

        public static void Patch(PatchContext context)
        {
            context
                .GetPattern(CreateRandomWeather)
                .Prefixes
                .Add(HCreateRandomWeather);

            //context
            //    .GetPattern(UpdatePlanetDataServer)
            //    .Prefixes
            //    .Add(HUpdatePlanetDataServer);

            Plugin.Log.Info("Patched MySectorWeatherComponent");
        }
    }
}

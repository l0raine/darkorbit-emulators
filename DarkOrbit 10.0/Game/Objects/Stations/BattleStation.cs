﻿using Ow.Game;
using Ow.Game.Movements;
using Ow.Game.Ticks;
using Ow.Managers;
using Ow.Net.netty.commands;
using Ow.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ow.Game.Objects.Stations
{
    public class EquippedModuleBase
    {
        public int ClanId { get; set; }
        public List<SatelliteBase> Modules { get; set; }

        public EquippedModuleBase(int clanId, List<SatelliteBase> satellites)
        {
            ClanId = clanId;
            Modules = satellites;
        }
    }

    class BattleStation : Activatable
    {
        public Dictionary<int, List<Satellite>> EquippedStationModule = new Dictionary<int, List<Satellite>>();

        public bool InBuildingState = false;
        public int BuildTimeInMinutes = 0;
        public string AsteroidName { get; set; }

        public BattleStation(string name, int factionId, Spacemap spacemap, Position position, Clan clan, 
            List<EquippedModuleBase> modules, bool inBuildingState, int buildTimeInMinutes, DateTime buildTime, List<int> visualModifiers) : base(spacemap, factionId, position, clan, (clan.Id == 0 || inBuildingState ? AssetTypeModule.ASTEROID : AssetTypeModule.BATTLESTATION))
        {
            ShieldAbsorption = 0.8;

            MaxHitPoints = 5850000;
            CurrentHitPoints = MaxHitPoints;
            //CurrentShieldPoints = 250000;
            //MaxShieldPoints = 500000;

            InBuildingState = inBuildingState;
            BuildTimeInMinutes = buildTimeInMinutes;

            AsteroidName = name;

            Name = Clan.Id != 0 ? Clan.Name : name;

            foreach (var modifier in visualModifiers)
                AddVisualModifier(new VisualModifierCommand(Id, (short)modifier, 0, "", 0, true));

            foreach (var module in modules)
            {
                var satellite = new List<Satellite>();

                foreach (var m in module.Modules)
                    satellite.Add(new Satellite(this, m.OwnerId, Satellite.GetName(m.Type), m.DesignId, m.ItemId, m.SlotId, m.Type, Satellite.GetPosition(Position, m.SlotId)));

                EquippedStationModule.Add(module.ClanId, satellite);
            }

            if (Clan.Id != 0 && InBuildingState)
            {
                BuildTimeInMinutes = BuildTimeInMinutes - (int)DateTime.Now.Subtract(buildTime).TotalMinutes;
                this.buildTime = DateTime.Now;
                Program.TickManager.AddTick(this, out var TickId);
            }
            else if (Clan.Id != 0 && !InBuildingState)
                Build();
        }

        public DateTime buildTime = new DateTime();
        public new void Tick()
        {
            if (InBuildingState && buildTime.AddMinutes(BuildTimeInMinutes) < DateTime.Now)
            {
                Build();
                QueryManager.BattleStations.BattleStation(this);
            }
        }

        public void Build()
        {
            AssetTypeId = AssetTypeModule.BATTLESTATION;

            RemoveVisualModifier(VisualModifierCommand.BATTLESTATION_CONSTRUCTING);
            //Visuals.Add(new VisualModifierCommand(Id, VisualModifierCommand.BATTLESTATION_DOWNTIME_TIMER, 1800, "", 0, true));
            PrepareSatellites();

            GameManager.SendCommandToMap(Spacemap.Id, AssetRemoveCommand.write(GetAssetType(), Id));

            foreach (var character in Spacemap.Characters.Values)
            {
                if (character is Player player)
                {
                    short relationType = character.Clan.Id != 0 && Clan.Id != 0 ? Clan.GetRelation(character.Clan) : (short)0;
                    player.SendCommand(GetAssetCreateCommand(relationType));
                }
            }

            BuildTimeInMinutes = 0;
            InBuildingState = false;
        }

        public void UpdatePlayerModuleInventory(Player player)
        {
            player.Storage.BattleStationModules.Clear();
            
            for (var i = 1; i <= 8; i++)
                player.Storage.BattleStationModules.Add(new StationModuleModule(Id, i, i, StationModuleModule.LASER_LOW_RANGE, 1, 1, 1, 1, 16, player.Name, 0, 0 , 0, 0, 500));
            for (var i = 9; i <= 16; i++)
                player.Storage.BattleStationModules.Add(new StationModuleModule(Id, i, i, StationModuleModule.LASER_MID_RANGE, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));
            for (var i = 15; i <= 22; i++)
                player.Storage.BattleStationModules.Add(new StationModuleModule(Id, i, i, StationModuleModule.LASER_HIGH_RANGE, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));
            for (var i = 23; i <= 30; i++)
                player.Storage.BattleStationModules.Add(new StationModuleModule(Id, i, i, StationModuleModule.ROCKET_LOW_ACCURACY, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));
            for (var i = 31; i <= 38; i++)
                player.Storage.BattleStationModules.Add(new StationModuleModule(Id, i, i, StationModuleModule.ROCKET_MID_ACCURACY, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));

            player.Storage.BattleStationModules.Add(new StationModuleModule(Id, 39, 39, StationModuleModule.DAMAGE_BOOSTER, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));
            player.Storage.BattleStationModules.Add(new StationModuleModule(Id, 40, 40, StationModuleModule.EXPERIENCE_BOOSTER, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));
            player.Storage.BattleStationModules.Add(new StationModuleModule(Id, 41, 41, StationModuleModule.HONOR_BOOSTER, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));
            player.Storage.BattleStationModules.Add(new StationModuleModule(Id, 42, 42, StationModuleModule.REPAIR, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));
            player.Storage.BattleStationModules.Add(new StationModuleModule(Id, 43, 43, StationModuleModule.DEFLECTOR, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));
            player.Storage.BattleStationModules.Add(new StationModuleModule(Id, 44, 44, StationModuleModule.HULL, 1, 1, 1, 1, 16, player.Name, 0, 0, 0, 0, 500));

            foreach (var battleStation in GameManager.BattleStations.Values)
            {
                if (battleStation.EquippedStationModule.ContainsKey(player.Clan.Id))
                {
                    foreach (var module in battleStation.EquippedStationModule[player.Clan.Id])
                    {
                        if (module.OwnerId == player.Id)
                        {
                            var playerModule = player.Storage.BattleStationModules.Where(x => x.itemId == module.ItemId).FirstOrDefault();
                            player.Storage.BattleStationModules.Remove(playerModule);
                        }
                    }
                }
            }         
        }

        public void PrepareSatellites()
        {
            foreach (var satellite in EquippedStationModule[Clan.Id])
            {
                if (satellite.Type != StationModuleModule.DEFLECTOR && satellite.Type != StationModuleModule.HULL)
                {
                    Spacemap.Activatables.TryAdd(satellite.Id, satellite);

                    foreach (var character in satellite.Spacemap.Characters.Values)
                    {
                        if (character is Player player)
                        {                   
                            short relationType = character.Clan.Id != 0 && satellite.Clan.Id != 0 ? satellite.Clan.GetRelation(character.Clan) : (short)0;
                            player.SendCommand(satellite.GetAssetCreateCommand(relationType));
                        }
                    }
                }
            }
        }

        public override void Click(GameSession gameSession)
        {
            var player = gameSession.Player;
            UpdatePlayerModuleInventory(player);

            int secondsLeft = (int)(TimeSpan.FromMinutes(BuildTimeInMinutes).TotalSeconds - (DateTime.Now - buildTime).TotalSeconds);

            if (InBuildingState)
                player.SendCommand(BattleStationBuildingStateCommand.write(Id, Id, Name, secondsLeft, 0, Clan.Name, new FactionModule((short)FactionId)));
            else
            {
                if (Clan.Id == 0 || buildTime.AddMinutes(BuildTimeInMinutes) > DateTime.Now)
                {
                    var stationModuleModule = new List<StationModuleModule>();

                    if (EquippedStationModule.ContainsKey(player.Clan.Id))
                    {
                        foreach (var mm in EquippedStationModule[player.Clan.Id])
                        {
                            stationModuleModule.Add(new StationModuleModule(Id, mm.ItemId, mm.SlotId, mm.Type, mm.CurrentHitPoints,
                                mm.MaxHitPoints, mm.CurrentShieldPoints, mm.MaxShieldPoints, 16, QueryManager.GetUserShipName(mm.OwnerId), 0, mm.InstallationSecondsLeft, 0, 0, 500));
                        }
                    }

                    var bestClan = EquippedStationModule.Values.Where(x => x.Where(y => y.Installed).ToList().Count() > 0).ToList().Count > 0 ? EquippedStationModule.OrderByDescending(x => x.Value.Count) : null;
                    player.SendCommand(BattleStationBuildingUiInitializationCommand.write(Id, Id, Name,
                                      new AsteroidProgressCommand(
                                              Id,
                                              (float)(EquippedStationModule.ContainsKey(player.Clan.Id) ? EquippedStationModule[player.Clan.Id].Where(x => x.Installed).ToList().Count : 0) / 10,
                                              (float)(bestClan != null ? bestClan.FirstOrDefault().Value.Where(x => x.Installed).ToList().Count : 0) / 10,
                                              player.Clan.Name,
                                              bestClan != null ? GameManager.GetClan(bestClan.FirstOrDefault().Key).Name :  "Leading clan's progress",                                             
                                              new EquippedModulesModule(EquippedStationModule.ContainsKey(player.Clan.Id) ? stationModuleModule : new List<StationModuleModule>()),
                                              (EquippedStationModule.ContainsKey(player.Clan.Id) ? EquippedStationModule[player.Clan.Id].Where(x => x.Installed).ToList().Count : 0) == 10),
                                      new AvailableModulesCommand(player.Storage.BattleStationModules),
                                      1,
                                      60,
                                      0));
                }
                else
                {
                    if (player.Clan.Id == Clan.Id)
                    {
                        var stationModuleModule = new List<StationModuleModule>();

                        if (EquippedStationModule.ContainsKey(player.Clan.Id))
                        {
                            foreach (var mm in EquippedStationModule[player.Clan.Id])
                            {
                                if (mm.Type == StationModuleModule.HULL)
                                {
                                    mm.CurrentHitPoints = CurrentHitPoints;
                                    mm.MaxHitPoints = MaxHitPoints;
                                    mm.CurrentShieldPoints = CurrentShieldPoints;
                                    mm.MaxShieldPoints = MaxShieldPoints;
                                }

                                stationModuleModule.Add(new StationModuleModule(Id, mm.ItemId, mm.SlotId, mm.Type, mm.CurrentHitPoints,
                                    mm.MaxHitPoints, mm.CurrentShieldPoints, mm.MaxShieldPoints, 16, "", 0, mm.InstallationSecondsLeft, 0, 0, 500));
                            }
                        }

                        player.SendCommand(BattleStationStatusCommand.write(Id, Id, Name, false, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, new EquippedModulesModule(stationModuleModule), false));
                    }
                }
            }
        }

        public override byte[] GetAssetCreateCommand(short clanRelationModule = ClanRelationModule.NONE)
        {
            return AssetCreateCommand.write(GetAssetType(), Name,
                                          FactionId, Clan.Tag, Id, 0, 0,
                                          Position.X, Position.Y, Clan.Id, true, true, true, true,
                                          new ClanRelationModule(clanRelationModule),
                                          VisualModifiers.Values.ToList());
        }
    }
}

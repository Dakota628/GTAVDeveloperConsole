using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using GTA;
using GTA.Math;
using GTA.Native;

namespace DeveloperConsole {
    public class DefaultCommands : Script {
        private CommandDispatcher _commandDispatcher;
        private DeveloperConsole _developerConsole;
        private bool _godEnabled;
        private bool _forceFieldEnabled;
        private bool _noClipEnabled;
        private Blip _lastWaypoint;
        private Player _player;

        public DefaultCommands() {
            this.RegisterConsoleScript(OnConsoleAttached);
        }

        private void OnConsoleAttached(DeveloperConsole dc) {
            Tick += OnTick;

            _player = Game.Player;

            _developerConsole = dc;
            _commandDispatcher = dc.CommandDispatcher;

            RegisterDefaultCommands();

            dc.PrintDebug("DefaultCommands loaded successfully.");
        }

        private void OnTick(object sender, EventArgs e) {
            if (Game.Player.Character.IsInVehicle() || Game.Player.Character.IsSittingInVehicle()) {
                var v = Game.Player.Character.CurrentVehicle;
                if (_godEnabled) {
                    v = new Vehicle(GTAFuncs.RequestEntityControl(v).Handle);
                    if(v != null) {
                        v.BodyHealth = v.MaxHealth;
                        v.EngineHealth = v.MaxHealth;
                        v.PetrolTankHealth = v.MaxHealth;
                        v.Health = v.MaxHealth;
                        v.CanBeVisiblyDamaged = false;
                    }
                    GTAFuncs.SetEntityInvinc(v, true);
                }
                else GTAFuncs.SetEntityInvinc(v, false);
            }

            if (_godEnabled) Game.Player.Character.Health = Game.Player.Character.MaxHealth;

            GTAFuncs.SetInvincTime(_godEnabled ? 30000 : 0);
            GTAFuncs.SetSPInvinc(_player, _godEnabled);

            if (GTAFuncs.IsWaypointActive())
                _lastWaypoint = new Blip(GTAFuncs.GetFirstBlipInfoID((int) BlipSprite.Waypoint));

            if (_noClipEnabled) {
                Game.Player.Character.Rotation = GameplayCamera.Rotation;

                GTAFuncs.SetEntityGravity(Game.Player.Character, false);
                GTAFuncs.SetEntityLoadColissionFlag(Game.Player.Character, false);
                GTAFuncs.SetEntityRecordsCollisions(Game.Player.Character, false);
                GTAFuncs.SetEntityCollision(Game.Player.Character, false, false);
                GTAFuncs.DisableControlAction(Control.MoveUp, true);
                GTAFuncs.DisableControlAction(Control.MoveDown, true);
                GTAFuncs.DisableControlAction(Control.MoveLeft, true);
                GTAFuncs.DisableControlAction(Control.MoveRight, true);
                GTAFuncs.DisableControlAction(Control.Attack, true);
                GTAFuncs.DisableControlAction(Control.Aim, true);

                var v = new Vector3(0, 0, 0);

                if (Game.Player.Character.IsInVehicle() || Game.Player.Character.IsSittingInVehicle())
                    Game.Player.Character.Position = Game.Player.Character.Position;

                if (GTAFuncs.GetControlNormal(Control.MoveUp) != 0) {
                    v += Vector3.Multiply(Game.Player.Character.ForwardVector,
                        -25*GTAFuncs.GetControlNormal(Control.MoveUp));
                }
                if (GTAFuncs.GetControlNormal(Control.MoveRight) != 0) {
                    v += Vector3.Multiply(Game.Player.Character.RightVector,
                        25*GTAFuncs.GetControlNormal(Control.MoveRight));
                }
                if (GTAFuncs.IsControlPressed(Control.Attack)) {
                    v += Vector3.Multiply(Game.Player.Character.UpVector, 15);
                }
                if (GTAFuncs.IsControlPressed(Control.Aim)) {
                    v += Vector3.Multiply(Game.Player.Character.UpVector, -15);
                }

                Game.Player.Character.Velocity = v;
            }
            else {
                GTAFuncs.EnableControlAction(Control.MoveUp, true);
                GTAFuncs.EnableControlAction(Control.MoveDown, true);
                GTAFuncs.EnableControlAction(Control.MoveLeft, true);
                GTAFuncs.EnableControlAction(Control.MoveRight, true);
                GTAFuncs.EnableControlAction(Control.Attack, true);
                GTAFuncs.EnableControlAction(Control.Aim, true);
                GTAFuncs.SetEntityGravity(Game.Player.Character, true);
                GTAFuncs.SetEntityLoadColissionFlag(Game.Player.Character, true);
                GTAFuncs.SetEntityRecordsCollisions(Game.Player.Character, true);
                GTAFuncs.SetEntityCollision(Game.Player.Character, true, true);
            }

            if (_forceFieldEnabled) {
                GTAFuncs.ClearAreaOfObjects(Game.Player.Character.Position, 100);
                GTAFuncs.ClearAreaOfProjectiles(Game.Player.Character.Position, 100);

                foreach (var _ent in World.GetAllEntities()) {
                    var ent = _ent;
                    if (ent.Handle == Game.Player.Character.Handle || GTAFuncs.GetPlayerEntity(Game.Player).Handle == ent.Handle) continue;

                    if (ent.Position.DistanceTo(Game.Player.Character.Position) <= 100) {
                        if (GTAFuncs.GetEntityType(ent) == GTAFuncs.EntityType.Ped && new Ped(ent.Handle).IsPlayer) {
                            Player player = GTAFuncs.GetPedPlayer(new Ped(ent.Handle));
                            ent = GTAFuncs.RequestEntityControl(player, 1);
                            GTAFuncs.ActivateDamageTrackerOnNetworkId(GTAFuncs.GetNetworkID(player), true);
                        }
                        else GTAFuncs.RequestEntityControl(ent, 1);

                        if (ent.IsAttached()) {
                            ent.Detach();
                            ent.Delete();
                        } else {
                            GTAFuncs.SetEntityInvinc(ent, false);
                            Vector3 vel = (Game.Player.Character.Position - ent.Position);
                            vel.Normalize();
                            vel *= -1000;
                            ent.Velocity = vel + new Vector3(0, 0, 100);
                        }
                    }

                }
            }

            if (_developerConsole.Debug && ConsoleSettings.IsDevBuild) GTAFuncs.AntiBan();
        }

        private void RegisterDefaultCommands() {
            #region Register help

            _commandDispatcher.RegisterCommand(
                new CommandDispatcher.Command("help", "Displays a list of all commands.", DefaultCommandEventHandler),
                true);

            #endregion

            #region Register clear

            _commandDispatcher.RegisterCommand(
                new CommandDispatcher.Command("clear", "Clears the console window.", DefaultCommandEventHandler), true);

            #endregion

            #region Register man

            var man = new CommandDispatcher.Command("man", "Prints out info for the provided command.",
                DefaultCommandEventHandler);

            man.AddArgumentSet(
                new CommandDispatcher.CommandArgument("cmd", "The command to get info for", typeof (string))
                );

            _commandDispatcher.RegisterCommand(man, true);

            #endregion

            #region Register cs

            var cs = new CommandDispatcher.Command("cs",
                "Executes a block of code and returns the .ToString() of the object.", DefaultCommandEventHandler);

            cs.AddArgumentSet(
                new CommandDispatcher.CommandArgument("code", "The code to execute", typeof (object))
                );

            _commandDispatcher.RegisterCommand(cs, true);

            #endregion

            #region Register god

            var god = new CommandDispatcher.Command("god", "Toggles godmode (invincibility).",
                DefaultCommandEventHandler);

            god.AddArgumentSet(
                new CommandDispatcher.CommandArgument("active", "Whether or not godmode should be active", typeof (bool))
                );

            _commandDispatcher.RegisterCommand(god, true);

            #endregion

            #region Register forcefield

            var forcefield = new CommandDispatcher.Command("forcefield", "Toggles forcefield.",
                DefaultCommandEventHandler);

            forcefield.AddArgumentSet(
                new CommandDispatcher.CommandArgument("active", "Whether or not forcefield should be active", typeof(bool))
                );

            if(ConsoleSettings.IsDevBuild) _commandDispatcher.RegisterCommand(forcefield, true);

            #endregion

            #region Register noclip

            var noclip = new CommandDispatcher.Command("noclip", "Toggles noclip.",
                DefaultCommandEventHandler);

            noclip.AddArgumentSet(
                new CommandDispatcher.CommandArgument("active", "Whether or not noclip should be active", typeof (bool))
                );

            _commandDispatcher.RegisterCommand(noclip, true);

            #endregion

            #region Register tp

            var tp = new CommandDispatcher.Command("tp", "Teleports you to a given location, player or blip.",
                DefaultCommandEventHandler);

            tp.AddArgumentSet();

            tp.AddArgumentSet(
                new CommandDispatcher.CommandArgument("player", "The name of the player to teleport to", typeof (string))
                );

            tp.AddArgumentSet(
                new CommandDispatcher.CommandArgument("x", "The x coordinate to teleport to", typeof (double)),
                new CommandDispatcher.CommandArgument("y", "The y coordinate to teleport to", typeof (double)),
                new CommandDispatcher.CommandArgument("z", "The z coordinate to teleport to", typeof (double))
                );

            tp.AddArgumentSet(
                new CommandDispatcher.CommandArgument("x", "The x coordinate to teleport to", typeof(int)),
                new CommandDispatcher.CommandArgument("y", "The y coordinate to teleport to", typeof(int)),
                new CommandDispatcher.CommandArgument("z", "The z coordinate to teleport to", typeof(int))
            );

            tp.AddArgumentSet(
                new CommandDispatcher.CommandArgument("playerId", "The player ID", typeof (int))
                );

            _commandDispatcher.RegisterCommand(tp, true);

            #endregion

            #region Register players

            _commandDispatcher.RegisterCommand(
                new CommandDispatcher.Command("players", "Displays a list of all players.", DefaultCommandEventHandler),
                true);

            #endregion

            #region Register drop

            var drop = new CommandDispatcher.Command("drop", "Drops an item with the specified hash key and value.",
                DefaultCommandEventHandler);

            drop.AddArgumentSet(
                new CommandDispatcher.CommandArgument("hashKey", "The hash key to drop", typeof (string)),
                new CommandDispatcher.CommandArgument("value", "The dropped items value", typeof (string))
                );

            _commandDispatcher.RegisterCommand(drop, true);

            #endregion

            #region Register vehicle

            var vehicle = new CommandDispatcher.Command("vehicle", "Spawns a vehicle of the speicifed hash key.",
                DefaultCommandEventHandler);

            vehicle.AddArgumentSet(
                new CommandDispatcher.CommandArgument("hashKey", "The hash key of the vehicle", typeof (string))
                );

            _commandDispatcher.RegisterCommand(vehicle, true);

            #endregion

            #region Register dump

            var dump = new CommandDispatcher.Command("dump", "Dumps the properties and methods of a specified object.",
                DefaultCommandEventHandler);

            dump.AddArgumentSet(
                new CommandDispatcher.CommandArgument("object", "The object to dump", typeof (object))
                );

            _commandDispatcher.RegisterCommand(dump, true);

            #endregion

            #region Register money

            _commandDispatcher.RegisterCommand(
                new CommandDispatcher.Command("money", "Drops money.", DefaultCommandEventHandler), true);

            #endregion

            #region Register weapons

            var weapons = new CommandDispatcher.Command("weapons", "Gives player a weapon of the speicifed hash key.",
                DefaultCommandEventHandler);

            _commandDispatcher.RegisterCommand(weapons, true);

            #endregion

            #region Register gtfo

            var gtfo = new CommandDispatcher.Command("gtfo", "Gets you out of a sticky situation.",
                DefaultCommandEventHandler);

            _commandDispatcher.RegisterCommand(gtfo, true);

            #endregion

            #region Register up

            var up = new CommandDispatcher.Command("up", "Teleport entity up.",
                DefaultCommandEventHandler);

            up.AddArgumentSet(
                new CommandDispatcher.CommandArgument("distance", "The distance to be teleported up", typeof(int))
                );

            up.AddArgumentSet(
                new CommandDispatcher.CommandArgument("entity", "The entity to teleport", typeof(Entity)),
                new CommandDispatcher.CommandArgument("distance", "The distance to be teleported up", typeof(int))
                );

            up.AddArgumentSet(
                new CommandDispatcher.CommandArgument("player", "The player to teleport", typeof(Player)),
                new CommandDispatcher.CommandArgument("distance", "The distance to be teleported up", typeof(int))
                );

            _commandDispatcher.RegisterCommand(up, true);

            #endregion

            #region Register launch

            var launch = new CommandDispatcher.Command("launch", "Launch entity up.",
                DefaultCommandEventHandler);

            launch.AddArgumentSet(
                new CommandDispatcher.CommandArgument("velocity", "The distance to be launched up", typeof(int))
                );

            launch.AddArgumentSet(
                new CommandDispatcher.CommandArgument("entity", "The entity to teleport", typeof(Entity)),
                new CommandDispatcher.CommandArgument("velocity", "The distance to be launched up", typeof(int))
                );

            launch.AddArgumentSet(
                new CommandDispatcher.CommandArgument("player", "The player to teleport", typeof(Player)),
                new CommandDispatcher.CommandArgument("velocity", "The distance to be launched up", typeof(int))
                );

            _commandDispatcher.RegisterCommand(launch, true);

            #endregion

            #region Register upright

            var upright = new CommandDispatcher.Command("upright", "Set an entity upright.",
                DefaultCommandEventHandler);

            upright.AddArgumentSet();

            upright.AddArgumentSet(new CommandDispatcher.CommandArgument("entity", "The entity to set upright", typeof(Entity)));

            upright.AddArgumentSet(new CommandDispatcher.CommandArgument("player", "The player to set upright", typeof(Player)));

            _commandDispatcher.RegisterCommand(upright, true);

            #endregion

            #region Register heal

            var heal = new CommandDispatcher.Command("heal", "Heal an entity",
                DefaultCommandEventHandler);

            heal.AddArgumentSet();

            heal.AddArgumentSet(new CommandDispatcher.CommandArgument("entity", "The entity to heal", typeof(Entity)));

            heal.AddArgumentSet(new CommandDispatcher.CommandArgument("player", "The player to heal", typeof(Player)));

            _commandDispatcher.RegisterCommand(heal, true);

            #endregion

            #region Register devshirt

            var devshirt = new CommandDispatcher.Command("devshirt", "Get a dev shirt",
                DefaultCommandEventHandler);

            devshirt.AddArgumentSet(new CommandDispatcher.CommandArgument("color", "The desired shirt color (white, gray/grey or black)", typeof(String)));

            if (ConsoleSettings.IsDevBuild) _commandDispatcher.RegisterCommand(devshirt, true);

            #endregion

            #region Register spectator
            var spectator = new CommandDispatcher.Command("spectator", "Set yourself or another player as a spectator.",
                DefaultCommandEventHandler);

            spectator.AddArgumentSet(
                new CommandDispatcher.CommandArgument("active", "Whether or not the player should be spectating", typeof(bool))
                );

            spectator.AddArgumentSet(
                new CommandDispatcher.CommandArgument("player", "The name of the player that should be spectating", typeof(Player)),
                new CommandDispatcher.CommandArgument("active", "Whether or not the player should be spectating", typeof(bool))
                );

            _commandDispatcher.RegisterCommand(spectator, true);
            #endregion
        }

        private void DefaultCommandEventHandler(CommandDispatcher.CommandEventArgs e) {
            switch (e.CommandName) {
                case "help":
                    HelpCommand();
                    break;
                case "clear":
                    _developerConsole.ClearLines();
                    break;
                case "man":
                    _developerConsole.PrintCommandInfo(e.Tokens[0].String);
                    break;
                case "cs":
                    _developerConsole.PrintLine(e.Tokens[0].Eval.ToString());
                    break;
                case "god":
                    GodCommand((bool) e.Tokens[0].Eval);
                    break;
                case "forcefield":
                    ForceFieldCommand((bool)e.Tokens[0].Eval);
                    break;
                case "noclip":
                    NoclipCommand((bool) e.Tokens[0].Eval);
                    break;
                case "tp":
                    switch (e.ArgIndex) {
                        case 0:
                            TpCommand();
                            break;
                        case 1:
                            TpCommand(e.Tokens[0].String);
                            break;
                        case 2:
                        case 3:
                            TpCommand(new Vector3((float) (Convert.ToDouble(e.Tokens[0].Eval)),
                                (float) (Convert.ToDouble(e.Tokens[1].Eval)), (float) (Convert.ToDouble(e.Tokens[2].Eval))));
                            break;
                        case 4:
                            TpCommand(Convert.ToInt32(e.Tokens[0].Eval));
                            break;
                    }
                    break;
                case "players":
                    PlayersCommand();
                    break;
                case "drop":
                    DropCommand((string) e.Tokens[0].Eval, Convert.ToInt32(e.Tokens[1].Eval));
                    break;
                case "vehicle":
                    VehicleCommand((string) e.Tokens[0].Eval);
                    break;
                case "dump":
                    DumpCommand(e.Tokens[0].Eval);
                    break;
                case "money":
                    MoneyCommand();
                    break;
                case "weapons":
                    WeaponsCommand();
                    break;
                case "gtfo":
                    GtfoCommand();
                    break;
                case "up":
                    switch (e.ArgIndex) {
                        case 0:
                            UpCommand((int) e.Tokens[0].Eval);
                            break;
                        case 1:
                            UpCommand((Entity) e.Tokens[0].Eval, (int) e.Tokens[1].Eval);
                            break;
                        case 2:
                            UpCommand((Player) e.Tokens[0].Eval, (int) e.Tokens[1].Eval);
                            break;
                    }
                    break;
                case "launch":
                    switch (e.ArgIndex) {
                        case 0:
                            LaunchCommand((int)e.Tokens[0].Eval);
                            break;
                        case 1:
                            LaunchCommand((Entity)e.Tokens[0].Eval, (int)e.Tokens[1].Eval);
                            break;
                        case 2:
                            LaunchCommand((Player)e.Tokens[0].Eval, (int)e.Tokens[1].Eval);
                            break;
                    }
                    break;
                case "upright":
                    switch (e.ArgIndex) {
                        case 0:
                            UprightCommand();
                            break;
                        case 1:
                            UprightCommand((Entity)e.Tokens[0].Eval);
                            break;
                        case 2:
                            UprightCommand((Player)e.Tokens[0].Eval);
                            break;
                    }
                    break;
                case "heal":
                    switch (e.ArgIndex) {
                        case 0:
                            HealCommand();
                            break;
                        case 1:
                            HealCommand((Entity)e.Tokens[0].Eval);
                            break;
                        case 2:
                            HealCommand((Player)e.Tokens[0].Eval);
                            break;
                    }
                    break;
                case "devshirt":
                    DevShirtCommand(e.Tokens[0].String.ToLower());
                    break;
                case "spectator":
                    switch (e.ArgIndex) {
                        case 0:
                            SpectatorCommand((bool) e.Tokens[0].Eval);
                            break;
                        case 1:
                            SpectatorCommand((Player) e.Tokens[0].Eval, (bool) e.Tokens[0].Eval);
                            break;
                    }
                    break;
            }
        }

        #region Command Methods

        #region Tp

        private void TpCommand() {
            GTAFuncs.RequestEntityControl(_player.Character, 5);
            if (_lastWaypoint == null) {
                _developerConsole.PrintError("Cannot teleport to waypoint. No waypoint exists.");
                return;
            }
            GTAFuncs.GetPlayerEntity(_player).Position =
                GTAFuncs.GetGroundPos(new Vector2(_lastWaypoint.Position.X, _lastWaypoint.Position.Y));
        }

        private void TpCommand(Vector3 v) {
            GTAFuncs.RequestEntityControl(_player.Character, 5);
            GTAFuncs.GetPlayerEntity(_player).Position = v;
        }

        private void TpCommand(string name) {
            var p = GTAFuncs.GetPlayerByName(name);
            TpCommand(p.Character.Position);
        }

        private void TpCommand(int playerId) {
            var p = new Player(playerId);
            TpCommand(p.Character.Position);
        }

        #endregion

        #region Dump

        private void DumpCommand(object o) {
            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(o)) {
                var name = descriptor.Name;
                var value = descriptor.GetValue(o);
                _developerConsole.PrintLine("." + name + " = " + value);
            }
            foreach (var m in o.GetType().GetMethods()) {
                var args = "";
                foreach (var t in m.GetGenericArguments()) args += t.FullName + ", ";
                Console.WriteLine("." + m.Name + "(" + args.TrimEnd(',', ' ') + ")");
            }
        }

        #endregion

        #region Vehicle

        private void VehicleCommand(string hash) {
            GTAFuncs.SpawnVehicleProper(new Model(hash), GTAFuncs.GetCoordsFromCam(15));
        }

        #endregion

        #region Drop

        private void DropCommand(string hash, int value) {
            GTAFuncs.CreateAmbientPickup(hash, _player.Character.Position, value);
        }

        #endregion

        #region Players

        private void PlayersCommand() {
            for (var i = 0; i < 32; i++) {
                var p = new Player(i);
                if (p.Name != "**Invalid**") {
                    _developerConsole
                        .PrintLine(
                            p.Name + " -- Player #" + p.Handle + ", Ped #" + p.Character.Handle + ", Position: " +
                            p.Character.Position.X + " " + p.Character.Position.Y + " " + p.Character.Position.Z,
                            GTAFuncs.GetPlayerInvincible(p) ? Color.CadetBlue : ConsoleSettings.DefaultTextColor);
                }
            }
        }

        #endregion

        #region God

        private void GodCommand(bool active) {
            _godEnabled = active;
        }

        #endregion

        #region Forcefield

        private void ForceFieldCommand(bool active) {
            _forceFieldEnabled = active;
        }

        #endregion

        #region Noclip

        private void NoclipCommand(bool active) {
            _noClipEnabled = active;
        }

        #endregion

        #region Help

        private void HelpCommand() {
            _developerConsole.PrintLine(
                "Press Ctrl+Tab to open the object selector. Press tab to cycle through objects and Ctrl+Tab again to select.");
            foreach (var c in _commandDispatcher.Commands.Values) {
                _developerConsole.PrintLine(c.Name + " -- " + c.Description);
            }
        }

        #endregion

        #region Money

        private void MoneyCommand() {
            GTAFuncs.CreateAmbientPickup("PICKUP_MONEY_CASE", _player.Character.Position, 40000);
            GTAFuncs.CreateAmbientPickup("PICKUP_MONEY_CASE", _player.Character.Position, 40000);
            GTAFuncs.CreateAmbientPickup("PICKUP_MONEY_CASE", _player.Character.Position, 40000);
            GTAFuncs.CreateAmbientPickup("PICKUP_MONEY_CASE", _player.Character.Position, 40000);
            GTAFuncs.CreateAmbientPickup("PICKUP_MONEY_CASE", _player.Character.Position, 40000);
        }

        #endregion

        #region Weapons

        private void WeaponsCommand() {
            var p = Game.Player.Character;
            var initialWeapon = p.Weapons.Current;
            foreach (var w in Enum.GetValues(typeof (WeaponHash)).Cast<WeaponHash>().ToList()) {
                p.Weapons.Give(w, 0, true, true);
                var currentWeapon = p.Weapons.Current;
                currentWeapon.Ammo = currentWeapon.MaxAmmo;
            }
            p.Weapons.Select(initialWeapon);
        }

        #endregion

        #region Gtfo

        private void GtfoCommand() {
            var p = Game.Player.Character;
            var v = GTAFuncs.SpawnVehicleProper(new Model(VehicleHash.Lazer), p.Position + new Vector3(0, 0, 500));
            v.EngineRunning = true;
            v.Heading = p.Heading;
            v.Velocity = Vector3.Multiply(v.ForwardVector, 100);
            v.Speed = 200;
            p.SetIntoVehicle(v, VehicleSeat.Driver);
        }

        #endregion

        #region Up
        private void UpCommand(Player p, int dist) {
            UpCommand(GTAFuncs.GetPlayerEntity(p), dist);
        }

        private void UpCommand(Entity e, int dist) {
            GTAFuncs.RequestEntityControl(e);
            e.Position += new Vector3(0, 0, dist);
        }

        private void UpCommand(int dist) {
            UpCommand(Game.Player, dist);
        }
        #endregion

        #region Launch
        private void LaunchCommand(Player p, int vel) {
            LaunchCommand(GTAFuncs.GetPlayerEntity(p), vel);
        }

        private void LaunchCommand(Entity e, int vel) {
            GTAFuncs.RequestEntityControl(e);
            e.Velocity += new Vector3(0, 0, vel);
        }

        private void LaunchCommand(int vel) {
            LaunchCommand(Game.Player, vel);
        }
        #endregion

        #region Upright
        private void UprightCommand(Player p) {
            UprightCommand(GTAFuncs.GetPlayerEntity(p));
        }

        private void UprightCommand(Entity e) {
            GTAFuncs.RequestEntityControl(e);
            e.Rotation = new Vector3(0, 0, 0);
        }

        private void UprightCommand() {
            UprightCommand(Game.Player);
        }
        #endregion

        #region Heal
        private void HealCommand(Player p) {
            HealCommand(GTAFuncs.GetPlayerEntity(p));
        }

        private void HealCommand(Entity e) {
            GTAFuncs.RequestEntityControl(e);
            e.Health = e.MaxHealth;
            while (e.Health != e.MaxHealth) {
                GTAFuncs.CreateAmbientPickup("PICKUP_HEALTH_STANDARD", e.Position, 50000);
            }
        }

        private void HealCommand() {
            HealCommand(Game.Player);
        }
        #endregion

        #region Devshirt
        private void DevShirtCommand(String color) {
            switch (color) {
                case "grey":
                case "gray":
                    Function.Call(Hash.CLEAR_PED_DECORATIONS, Game.Player.Character.Handle);
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, 11, 44, 3, 0);
                    Function.Call(Hash._0x5F5D1665E352A839, Game.Player.Character.Handle, GTAFuncs.GetHashKey("mphipster_overlays"), GTAFuncs.GetHashKey("fm_rstar_m_tshirt_002"));
                    break;
                case "black":
                    Function.Call(Hash.CLEAR_PED_DECORATIONS, Game.Player.Character.Handle);
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, 11, 22, 1, 0);
                    Function.Call(Hash._0x5F5D1665E352A839, Game.Player.Character.Handle, GTAFuncs.GetHashKey("mphipster_overlays"), GTAFuncs.GetHashKey("fm_rstar_m_tshirt_001"));
                    break;
                case "white":
                    Function.Call(Hash.CLEAR_PED_DECORATIONS, Game.Player.Character.Handle);
                    Function.Call(Hash.SET_PED_COMPONENT_VARIATION, 11, 22, 0, 0);
                    Function.Call(Hash._0x5F5D1665E352A839, Game.Player.Character.Handle, GTAFuncs.GetHashKey("mphipster_overlays"), GTAFuncs.GetHashKey("fm_rstar_m_tshirt_003"));
                    break;
                default:
                    _developerConsole.PrintError("No shirt exists with the color '" + color + "'.");
                    break;
            }
        }
        #endregion

        #region Spectator
        private void SpectatorCommand(Player p, bool b) {
            GTAFuncs.SetInSpectatorMode(p, b);
        }

        private void SpectatorCommand(bool b) {
            SpectatorCommand(Game.Player, b);
        } 
        #endregion

        #endregion
    }
}
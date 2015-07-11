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
        private Blip _lastWaypoint;
        private bool _noClipEnabled;
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

            //dc.PrintDebug("DefaultCommands loaded successfully.");
        }

        private void OnTick(object sender, EventArgs e) {
            if (Game.Player.Character.IsInVehicle() || Game.Player.Character.IsSittingInVehicle()) {
                var v = Game.Player.Character.CurrentVehicle;
                if (_godEnabled) {
                    v.BodyHealth = v.MaxHealth;
                    v.EngineHealth = v.MaxHealth;
                    v.PetrolTankHealth = v.MaxHealth;
                    v.Health = v.MaxHealth;
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

            GTAFuncs.AntiBan();
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
                new CommandDispatcher.CommandArgument("playerId", "The player ID", typeof (double))
                );

            _commandDispatcher.RegisterCommand(tp, true);

            #endregion

            #region Register kill

            var kill = new CommandDispatcher.Command("kill", "Kills the specified player", DefaultCommandEventHandler);

            kill.AddArgumentSet(
                new CommandDispatcher.CommandArgument("player", "The name of the player to teleport to", typeof (string))
                );

            kill.AddArgumentSet(
                new CommandDispatcher.CommandArgument("playerId", "The player ID", typeof (double))
                );

            _commandDispatcher.RegisterCommand(kill, true);

            #endregion

            #region Register kick

            var kick = new CommandDispatcher.Command("kick", "Kicks the specified player", DefaultCommandEventHandler);

            kick.AddArgumentSet(
                new CommandDispatcher.CommandArgument("player", "The name of the player to teleport to", typeof (string))
                );

            kick.AddArgumentSet(
                new CommandDispatcher.CommandArgument("playerId", "The player ID", typeof (double))
                );

            _commandDispatcher.RegisterCommand(kick, true);

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

            #region Register idle

            var idle = new CommandDispatcher.Command("idle", "Set the time before idle timeout.",
                DefaultCommandEventHandler);

            idle.AddArgumentSet(
                new CommandDispatcher.CommandArgument("time", "The idle timeout time", typeof (double))
                );

            _commandDispatcher.RegisterCommand(idle, true);

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
                            TpCommand(new Vector3((float) ((double) e.Tokens[0].Eval),
                                (float) ((double) e.Tokens[1].Eval), (float) ((double) e.Tokens[2].Eval)));
                            break;
                        case 3:
                            TpCommand(Convert.ToInt32(e.Tokens[0].Eval));
                            break;
                    }
                    break;
                case "kill":
                    KillCommand((string) e.Tokens[0].Eval);
                    switch (e.ArgIndex) {
                        case 0:
                            KillCommand(e.Tokens[0].String);
                            break;
                        case 1:
                            KillCommand(Convert.ToInt32(e.Tokens[0].Eval));
                            break;
                    }
                    break;
                case "kick":
                    KillCommand((string) e.Tokens[0].Eval);
                    switch (e.ArgIndex) {
                        case 0:
                            KickCommand(e.Tokens[0].String);
                            break;
                        case 1:
                            KickCommand(Convert.ToInt32(e.Tokens[0].Eval));
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
                case "idle":
                    IdleCommand((int) e.Tokens[0].Eval);
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
            }
        }

        #region Command Methods

        #region Tp

        private void TpCommand() {
            if (_lastWaypoint == null) {
                _developerConsole.PrintError("Cannot teleport to waypoint. No waypoint exists.");
                return;
            }
            GTAFuncs.GetPlayerEntity(_player).Position =
                GTAFuncs.GetGroundPos(new Vector2(_lastWaypoint.Position.X, _lastWaypoint.Position.Y));
        }

        private void TpCommand(Vector3 v) {
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

        #region Idle

        private void IdleCommand(int time) {
            _developerConsole.PrintWarning("This command does not currently work as intended.");
        }

        #endregion

        #region Vehicle

        private void VehicleCommand(string hash) {
            World.CreateVehicle(hash, GTAFuncs.GetCoordsFromCam(15));
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

        #region Noclip

        private void NoclipCommand(bool active) {
            _noClipEnabled = active;
        }

        #endregion

        #region Kill

        private void KillCommand(Player player) {
            if (player == null) {
                _developerConsole.PrintError("Player not found!");
                return;
            }
            player.Character.Kill();
            if (!player.Character.IsDead) {
                World.ShootBullet(player.Character.Position, player.Character.Position, player.Character,
                    new Model(GTAFuncs.GetHashKey("WEAPON_AIRSTRIKE_ROCKET")), 500000);
            }
        }

        private void KillCommand(string player) {
            KillCommand(GTAFuncs.GetPlayerByName(player));
        }

        private void KillCommand(int playerId) {
            KillCommand(new Player(playerId));
        }

        #endregion

        #region Kick

        private void KickCommand(Player player) {
            if (player == null) {
                _developerConsole.PrintError("Player not found!");
                return;
            }
            GTAFuncs.KickPlayer(player);
        }

        private void KickCommand(string player) {
            KickCommand(GTAFuncs.GetPlayerByName(player));
        }

        private void KickCommand(int playerId) {
            KickCommand(new Player(playerId));
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
            var v = World.CreateVehicle(new Model(VehicleHash.Lazer), p.Position + new Vector3(0, 0, 500));
            v.EngineRunning = true;
            v.Heading = p.Heading;
            ;
            v.Velocity = Vector3.Multiply(v.ForwardVector, 100);
            v.Speed = 200;
            p.SetIntoVehicle(v, VehicleSeat.Driver);
        }

        #endregion

        #endregion
    }
}
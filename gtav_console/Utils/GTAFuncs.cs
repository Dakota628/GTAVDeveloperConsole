using System;
using System.Drawing;
using GTA;
using GTA.Math;
using GTA.Native;
using Font = GTA.Font;

namespace DeveloperConsole {
    public static class GTAFuncs {
        public static void SetEntityRecordsCollisions(Entity e, bool b) {
            Function.Call(Hash.SET_ENTITY_RECORDS_COLLISIONS, e, b);
        }

        public static void SetEntityCollision(Entity e, bool b, bool b1) {
            Function.Call(Hash.SET_ENTITY_COLLISION, e, b, b1);
        }

        public static void SetEntityProofs(Entity e, bool b1, bool b2, bool b3, bool b4, bool b5, bool b6, bool b7,
            bool b8) {
                Function.Call(Hash.SET_ENTITY_PROOFS, e, b1, b2, b3, b4, b5, b6, b7, b8);
        }

        public static void SetEntityLoadColissionFlag(Entity e, bool b) {
            Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, e, b);
        }

        public static void SetEntityGravity(Entity e, bool b) {
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY,e.Handle, false);
        }

        public static float GetControlNormal(Control c) {
            return Function.Call<float>(Hash.GET_CONTROL_NORMAL, 0, (int)c);
        }

        public static int GetControlValue(Control c) {
            return Function.Call<int>(Hash.GET_CONTROL_VALUE, 0, (int) c);
        }

        public static bool IsControlPressedIgnoreDisabled(Control c) {
            return Function.Call<bool>(Hash.IS_DISABLED_CONTROL_PRESSED, 0, (int) c) || Function.Call<bool>(Hash.IS_CONTROL_PRESSED, 0, (int) c);
        }

        public static void SetTextEdge(int i, Color c) {
            Function.Call(Hash.SET_TEXT_EDGE, i, c.R, c.G, c.B, c.A);
        }

        public static void SetTextDropShadow(int i, Color c) {
            Function.Call(Hash.SET_TEXT_DROP_SHADOW, i, c.R, c.G, c.B, c.A);
        }

        public static unsafe Quaternion GetEntityQuaternion(Entity e) {
            float x = 0;
            float y = 0;
            float z = 0;
            float w = 0;
            Function.Call<bool>(Hash.GET_ENTITY_QUATERNION, e.Handle, &x, &y, &z, &w);
            return new Quaternion(x, y, z, w);
        }

        public static void DrawLine(Vector3 v1, Vector3 v2, Color c) {
            Function.Call(Hash.DRAW_LINE, v1.X, v1.Y, v1.Z, v2.X, v2.Y, v2.Z, c.R, c.G, c.B, c.A);
        }

        public static void DrawBox(Vector3 v1, Vector3 v2, Color c) {
            Function.Call(Hash.DRAW_BOX, v1.X, v1.Y, v1.Z, v2.X, v2.Y, v2.Z, c.R, c.G, c.B, c.A);
        }

        public static void DrawPoly(Vector3 v1, Vector3 v2, Vector3 v3, Color c) {
            Function.Call(Hash.DRAW_POLY, v1.X, v1.Y, v1.Z, v2.X, v2.Y, v2.Z, v3.X, v3.Y, v3.Z, c.R, c.G, c.B, c.A);
        }

        public static unsafe Vector2 WorldToScreen(Vector3 world) {
            float x = 0;
            float y = 0;
            Function.Call<bool>(Hash._WORLD3D_TO_SCREEN2D, world.X, world.Y, world.Z, &x, &y);
            return new Vector2(x*UI.WIDTH, y*UI.HEIGHT);
        }

        public static unsafe Point TestGetInput() {
            var x = 0;
            var y = 0;
            Function.Call(Hash._0x36C1451A88A09630, &x, &y);
            return new Point(x, y);
        }

        public static void DisplayRadar(bool b) {
            Function.Call(Hash.DISPLAY_RADAR, b);
        }

        public static void DisplayHud(bool b) {
            Function.Call(Hash.DISPLAY_HUD, b);
        }

        public static void ShowCursorThisFrame() {
            Function.Call(Hash._SHOW_CURSOR_THIS_FRAME);
        }

        public static int GetNumNetworkPlayers() {
            return Function.Call<int>(Hash.NETWORK_GET_NUM_CONNECTED_PLAYERS);
        }

        public static bool GetPlayerInvincible(Player p) {
            return Function.Call<bool>(Hash.GET_PLAYER_INVINCIBLE, p.Handle);
        }

        public static int GetFirstBlipInfoID(int i) {
            return Function.Call<int>(Hash.GET_FIRST_BLIP_INFO_ID, i);
        }

        public static bool IsWaypointActive() {
            return Function.Call<bool>(Hash.IS_WAYPOINT_ACTIVE);
        }

        public static Vector3 GetGroundPos(Vector2 pos) {
            var height = World.GetGroundHeight(pos);
            return new Vector3(pos.X, pos.Y, height + 1);
        }

        public static Vector3 GetCoordsFromCam(int distance) {
            var rot = GameplayCamera.Rotation;
            var coord = GameplayCamera.Position;

            var tZ = rot.Z*0.0174532924f;
            var tX = rot.X*0.0174532924f;
            var num = Math.Abs((float) Math.Cos(tX));

            coord.X = coord.X + (float) (-Math.Sin(tZ))*(num + distance);
            coord.Y = coord.Y + (float) (Math.Cos(tZ))*(num + distance);
            coord.Z = coord.Z + (float) (Math.Sin(tX))*8;

            return coord;
        }

        public static Player GetPlayerByName(string player) {
            for (var i = 0; i < 32; i++) {
                var p = new Player(i);
                if (p.Name.ToLower() == player.ToLower()) return p;
            }

            return null;
        }

        public static Entity GetPlayerEntity(Player p) {
            if (p.Character.IsInVehicle() || p.Character.IsSittingInVehicle()) return p.Character.CurrentVehicle;
            return p.Character;
        }

        public static void KickPlayer(Player p) {
            Function.Call(Hash.NETWORK_SESSION_KICK_PLAYER, p.Handle);
        }

        public static void AntiBan() {
            var playerGroup = GetPedGroupIndex(Game.Player.Character);
            Function.Call(Hash.NETWORK_SET_SCRIPT_IS_SAFE_FOR_NETWORK_GAME);
            Function.Call(Hash.NETWORK_SET_THIS_SCRIPT_IS_NETWORK_SCRIPT, 1, true, 1);
            StatSetInt("MP" + playerGroup + "_BAD_SPORT_BITSET", 0, true);
            StatSetInt("MP" + playerGroup + "_CHEAT_BITSET", 0, true);
            StatSetFloat("MPPLY_OVERALL_BADSPORT", 0, true);
            StatSetBool("MPPLY_CHAR_IS_BADSPORT", false, true);
            StatSetInt("MPPLY_BECAME_BADSPORT_NUM", 0, true);
            StatSetInt("MPPLY_REPORT_STRENGTH", 32, true);
            StatSetInt("MPPLY_COMMEND_STRENGTH", 100, true);
            StatSetInt("MPPLY_FRIENDLY", 100, true);
            StatSetInt("MPPLY_HELPFUL", 100, true);
            StatSetInt("MPPLY_GRIEFING", 0, true);
            StatSetInt("MPPLY_OFFENSIVE_LANGUAGE", 0, true);
            StatSetInt("MPPLY_OFFENSIVE_UGC", 0, true);
            StatSetInt("MPPLY_VC_HATE", 0, true);
            StatSetInt("MPPLY_GAME_EXPLOITS", 0, true);
            StatSetInt("MPPLY_ISPUNISHED", 0, true);
        }

        public static void SetEntityInvinc(Entity e, bool b) {
            Function.Call(Hash.SET_ENTITY_INVINCIBLE, e.Handle, b);
        }

        public static void SetSPInvinc(Player p, bool b) {
            Function.Call(Hash.SET_PLAYER_INVINCIBLE, p.Handle, b);
        }

        public static void SetInvincTime(int time) {
            Function.Call(Hash.NETWORK_SET_LOCAL_PLAYER_INVINCIBLE_TIME, time);
        }

        public static int GetPedGroupIndex(Ped p) {
            return Function.Call<int>(Hash.GET_PED_GROUP_INDEX, p.Handle);
        }

        public static void StatSetInt(string hashKey, int value, bool b) {
            Function.Call(Hash.STAT_SET_INT, GetHashKey(hashKey), value, b);
        }

        public static void StatSetFloat(string hashKey, float value, bool b) {
            Function.Call(Hash.STAT_SET_FLOAT, GetHashKey(hashKey), value, b);
        }

        public static void StatSetBool(string hashKey, bool value, bool b) {
            Function.Call(Hash.STAT_SET_BOOL, GetHashKey(hashKey), value, b);
        }

        public static void CreateAmbientPickup(string hashKey, Vector3 pos, int value) {
            Function.Call(Hash.CREATE_AMBIENT_PICKUP, GetHashKey(hashKey), pos.X, pos.Y, pos.Z, 0, value, 1, false, true);
        }

        public static int GetHashKey(string s) {
            return Function.Call<int>(Hash.GET_HASH_KEY, s);
        }

        public static void SetTextRightJustifty(bool b) {
            Function.Call(Hash.SET_TEXT_RIGHT_JUSTIFY, b);
        }

        public static float GetTextWidth(string s, Font f, float scale) {
            Function.Call(Hash.SET_TEXT_FONT, (int) f);
            Function.Call(Hash.SET_TEXT_SCALE, scale, scale);
            Function.Call(Hash._0x54CE8AC98E120CAB, "STRING"); //SET_TEXT_ENTRY_FOR_WIDTH
            Function.Call(Hash._ADD_TEXT_COMPONENT_STRING, s);
            return Function.Call<float>(Hash._0x85F061DA64ED2F67, 1); //_GET_TEXT_SCREEN_WIDTH
        }

        public static void DisableControlAction(Control c, bool b) {
            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int) c, b);
        }

        public static void EnableControlAction(Control c, bool b) {
            Function.Call(Hash.ENABLE_CONTROL_ACTION, 0, (int) c, b);
        }

        public static void SetControlActions(bool b) {
            if (b) Function.Call(Hash.ENABLE_ALL_CONTROL_ACTIONS, 0);
            else Function.Call(Hash.DISABLE_ALL_CONTROL_ACTIONS, 0);
        }
    }
}
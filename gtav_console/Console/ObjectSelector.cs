using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.Math;

namespace DeveloperConsole {
    /// <summary>
    ///     The object selector
    /// </summary>
    public class ObjectSelector {
        private int _lastHandle;
        private Entity _selectedEntity;

        /// <summary>
        ///     Creates an ObjectSelector
        /// </summary>
        public ObjectSelector() {
            Enabled = false;
        }

        /// <summary>
        ///     A dictionary of entities where the key is the entities handle and the value is the entity
        /// </summary>
        public SortedDictionary<int, Entity> Entities { get; private set; }

        /// <summary>
        ///     A dictionary of entity click boxes where the value is the entities click box handle and the key is the entity
        /// </summary>
        public Dictionary<Entity, Rectangle> EntityClickBoxes { get; private set; }

        /// <summary>
        ///     Where or not the object selector is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        ///     This method should be called each tick
        /// </summary>
        public void Tick() {
            if (_selectedEntity != null && !_selectedEntity.Exists()) _selectedEntity = null;

            HandleMouse();
            Draw();
        }

        /// <summary>
        ///     Handles mouse/rightjoystick input
        /// </summary>
        private void HandleMouse() {
            if (Enabled) {
                GTAFuncs.ShowMouseThisFrame();

                var pt = GTAFuncs.GetMousePos();

                if (EntityClickBoxes != null) {
                    foreach (var v in EntityClickBoxes) {
                        var r = v.Value;
                        if (r.IntersectsWith(new Rectangle(pt.X, pt.Y, 1, 1))) {
                            _selectedEntity = v.Key;
                            _lastHandle = _selectedEntity.Handle;
                        }
                    }
                }

                if (GTAFuncs.IsLeftMouseClicked()) SelectObject(true);
                if (GTAFuncs.IsRightMouseClicked()) SelectObject(true, true);
            }
        }

        /// <summary>
        ///     Calls the object selector
        /// </summary>
        public void Draw() {
            if (Enabled) {
                UI.ShowSubtitle(
                    "Use the mouse and click an entity.\n Or Press Tab to cycle between objects.\nPress Ctl+Tab to select the object highlighted in red.",
                    1);
                GTAFuncs.DisplayHud(false);
                GTAFuncs.DisplayRadar(false);
                DrawEnts();
            }
            else {
                GTAFuncs.DisplayHud(true);
                GTAFuncs.DisplayRadar(true);
            }
        }

        /// <summary>
        ///     Choose the currently select entity
        /// </summary>
        /// <param name="click">Was this issued through click?</param>
        /// <param name="alt">Whether or not to create a cs command to modify this object</param>
        private void SelectObject(bool click = false, bool alt = false) {
            if (click && _selectedEntity != null || !click) Enabled = false;
            if (_selectedEntity == null) return;
            var returnText = "return ";
            if (DeveloperConsole.Instance.Input.StartsWith("cs")) returnText = "";

            if (alt) {
                DeveloperConsole.Instance.Input = "cs";
                returnText = "";
            }

            if (_selectedEntity is Vehicle) {
                DeveloperConsole.Instance.Input += " {" + returnText + "new Vehicle(" +
                                                   _selectedEntity.Handle + ")} ";
            }
            else if (_selectedEntity is Ped) {
                if (((Ped) _selectedEntity).IsPlayer) {
                    DeveloperConsole.Instance.Input += " {" + returnText + "new Player(" + _selectedEntity.Handle +
                                                       ")} ";
                }
                else {
                    DeveloperConsole.Instance.Input += " {" + returnText + "new Ped(" + _selectedEntity.Handle +
                                                       ")} ";
                }
            } else if (_selectedEntity is Prop) {
                DeveloperConsole.Instance.Input += " {" + returnText + "new Prop(" +
                                                    _selectedEntity.Handle + ")} ";
            }
            else {
                DeveloperConsole.Instance.Input += " {" + returnText + "new Entity(" +
                                                   _selectedEntity.Handle + ")} ";
            }
            DeveloperConsole.Instance.ShowConsole(true);
        }

        /// <summary>
        ///     Handles keypresses, should be called from the scripts KeyDown event
        /// </summary>
        /// <param name="sender">The object sending the event</param>
        /// <param name="e">The event arguments</param>
        public void KeyPress(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Tab) {
                if ((e.Modifiers & Keys.Control) == Keys.Control) {
                    if (Enabled) {
                        SelectObject();
                    }
                    else {
                        Enabled = true;
                    }
                }
                else {
                    if (!Enabled) return;
                    _selectedEntity = GetNextEntity();
                    _lastHandle = _selectedEntity.Handle;
                }
            }
        }

        /// <summary>
        ///     Gets the next entity in the object selector
        /// </summary>
        /// <returns>The next entity</returns>
        private Entity GetNextEntity() {
            if (Entities.Count < 1) return null;
            var last = Entities.Last().Key;
            if (_lastHandle > last) _lastHandle = 0;
            while (_lastHandle++ < last) {
                if (Entities.ContainsKey(_lastHandle)) return Entities[_lastHandle];
            }
            _lastHandle = 0;
            return GetNextEntity();
        }

        /// <summary>
        ///     Draw all the entities we can see
        /// </summary>
        private void DrawEnts() {
            Entities = new SortedDictionary<int, Entity>();
            EntityClickBoxes = new Dictionary<Entity, Rectangle>();

            foreach (var e in World.GetAllEntities()) DrawEntity(e);
        }

        /// <summary>
        ///     Draw a specified entity
        /// </summary>
        /// <param name="e">The entity to draw</param>
        private void DrawEntity(Entity e) {
            if (!e.IsOnScreen || e.IsOccluded) return;

            var textScale = .25f;

            //Set text color
            var c = Color.FromArgb(150, Color.White);
            if (_selectedEntity != null && e.Equals(_selectedEntity)) c = Color.Red;
            else {
                switch (GTAFuncs.GetEntityType(e)) {
                    case GTAFuncs.EntityType.Ped:
                        c = new Ped(e.Handle).IsPlayer
                            ? Color.FromArgb(150, Color.CornflowerBlue)
                            : Color.FromArgb(150, Color.Yellow);
                        break;
                    case GTAFuncs.EntityType.Vehicle:
                        c = Color.FromArgb(150, Color.DeepPink);
                        break;
                    case GTAFuncs.EntityType.Prop:
                        c = Color.FromArgb(150, Color.Green);
                        break;
                }
            }

            //Create entity info lines
            var lines = new List<string>();

            switch(GTAFuncs.GetEntityType(e)) {
                case GTAFuncs.EntityType.Ped:
                    Ped ped = new Ped(e.Handle);
                    if (ped.IsPlayer) {
                        Player pl = GTAFuncs.GetPedPlayer(ped);
                        lines.Add(pl.Name);
                        lines.Add("Player #" + pl.Handle);
                        if (GTAFuncs.GetPlayerInvincible(pl)) lines.Add("INVINCIBLE");
                    }
                    lines.Add("Ped #" + ped.Handle);
                    e = ped;
                    break;
                case GTAFuncs.EntityType.Vehicle:
                    Vehicle v = new Vehicle(e.Handle);
                    lines.Add("Vehicle #" + v.Handle);
                    lines.Add(v.FriendlyName);
                    lines.Add(v.DisplayName);
                    e = v;
                    break;
                case GTAFuncs.EntityType.Prop:
                    Prop prop = new Prop(e.Handle);
                    lines.Add("Prop #" + prop.Handle);
                    lines.Add("Model: " + prop.Model.Hash);
                    e = prop;
                    break;
                default:
                    lines.Add("Entity #" + e.Handle);
                    break;
            }

            Entities.Add(e.Handle, e);

            //Draw entity info
            var screenPos = GTAFuncs.WorldToScreen(e.Position);
            var contain =
                new Rectangle(
                    new Point((int) screenPos.X,
                        (int)screenPos.Y + (GTAFuncs.GetEntityType(e) == GTAFuncs.EntityType.Ped && new Ped(e.Handle).IsInVehicle() ? lines.Count * -10 : 0)),
                    new Size(50, (lines.Count*11) - 1));

            for (var i = 0; i < lines.Count; i++) {
                GTAFuncs.SetTextDropShadow(2, Color.FromArgb(255, 0, 0, 0));
                new UIText(lines[i], new Point(0, (i*10)), textScale, Color.FromArgb(255, c), 0, true).Draw(
                    new Size(contain.Location));
                GTAFuncs.SetTextDropShadow(0, Color.Transparent);
            }

            EntityClickBoxes.Add(e, contain);
            DrawEntBox(e, c);
        }

        /// <summary>
        ///     Draws a box around the specified entity
        /// </summary>
        /// <param name="e">The entity to draw around</param>
        /// <param name="c">The box color</param>
        public void DrawEntBox(Entity e, Color c) {
            Vector3 min = new Vector3(), max = new Vector3();
            e.Model.GetDimensions(out min, out max);
            new Rectangle3D(e.Position, min, max).Rotate(GTAFuncs.GetEntityQuaternion(e)).DrawWireFrame(c, true);
        }
    }
}
